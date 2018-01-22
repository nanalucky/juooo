using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Security;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using System.Threading;
using System.IO.Compression;


namespace juooo
{
    public class RequestState
    {
        // This class stores the State of the request.
        public HttpWebRequest request;
        public HttpWebResponse response;
        public string body;
        public int nShow;
        public int nBuyTimes;
        
        public RequestState()
        {
            request = null;
            response = null;
            body = @"";
            nShow = 0;
            nBuyTimes = 0;
        }
    }
    
    class Player
    {
        public string strAccount = @"";
        public string strPassword = @"";
        public string strUserName1 = @"";
        public string strCard1 = @"";
        public string strUserName2 = @"";
        public string strCard2 = @"";
        public List<bool> listShowFinish = new List<bool>();
        public List<int> listShowTicketIndex = new List<int>();

        public int nIndex = 0;
        public Thread thread;
        bool bLoginSuccess;
        bool bAddressSuccess;
        string strAddressId = @"";
        string strIdcard = @"";
        bool bIdcardSuccess;
        int nBuyIndex;
        string strBuyK;

        CookieContainer cookieContainer = new CookieContainer();
        Encoding requestEncoding = Encoding.GetEncoding("utf-8");

        ManualResetEvent allDone;

        private string GetBody(HttpWebResponse response)
        {
            string body = @"";
            System.IO.StreamReader reader = null;

            if (response.ContentEncoding.ToLower().Contains("gzip"))
            {
                reader = new System.IO.StreamReader(new GZipStream(response.GetResponseStream(), CompressionMode.Decompress), requestEncoding);
            }
            else
            {
                reader = new System.IO.StreamReader(response.GetResponseStream(), requestEncoding);
            }
            body = reader.ReadToEnd();
            return body; 
        }

        private string GetBody(RequestState _requestState)
        {
            System.IO.StreamReader reader = null;
            if (_requestState.response.ContentEncoding.ToLower().Contains("gzip"))
            {
                reader = new System.IO.StreamReader(new GZipStream(_requestState.response.GetResponseStream(), CompressionMode.Decompress), requestEncoding);
            }
            else
            {
                reader = new System.IO.StreamReader(_requestState.response.GetResponseStream(), requestEncoding);
            }
            _requestState.body = reader.ReadToEnd();
            return _requestState.body;
        }

        private void RespFirstCallback(IAsyncResult asynchronousResult)
        {
            try
            {
                // State of request is asynchronous.
                RequestState myRequestState = (RequestState)asynchronousResult.AsyncState;
                HttpWebRequest myHttpWebRequest = myRequestState.request;
                myRequestState.response = (HttpWebResponse)myHttpWebRequest.EndGetResponse(asynchronousResult);

                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(Http.CheckValidationResult);
                myRequestState.request = WebRequest.Create(@"http://passport.juooo.com/User/login") as HttpWebRequest;
                myRequestState.request.ProtocolVersion = HttpVersion.Version11;
                myRequestState.request.Method = "POST";
                myRequestState.request.Headers.Add("Origin", "http://passport.juooo.com");
                myRequestState.request.Referer = @"http://passport.juooo.com/User/login";
                myRequestState.request.Headers.Add("Accept-Language", "zh-Hans-CN,zh-Hans;q=0.5");
                myRequestState.request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36 Edge/16.16299";
                myRequestState.request.ContentType = @"application/x-www-form-urlencoded; charset=UTF-8";
                myRequestState.request.Accept = "application/json, text/javascript, */*; q=0.01";
                myRequestState.request.Headers.Add("X-Requested-With", "XMLHttpRequest");
                myRequestState.request.Headers.Add("Accept-Encoding", "gzip, deflate");
                myRequestState.request.Headers.Add("Pragma", "no-cache"); 
                myRequestState.request.CookieContainer = cookieContainer;

                StringBuilder buffer = new StringBuilder();
                buffer.AppendFormat("{0}={1}", "username", strAccount);
                buffer.AppendFormat("&{0}={1}", "password", strPassword);
                buffer.AppendFormat("&{0}={1}", "isCard", "1");
                Byte[] data = requestEncoding.GetBytes(buffer.ToString());
                using (Stream stream = myRequestState.request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                } 
                IAsyncResult result = (IAsyncResult)myRequestState.request.BeginGetResponse(new AsyncCallback(RespLoginCallback), myRequestState);
                return;
            }
            catch (WebException e)
            {
                Console.WriteLine("\nRespCallback Exception raised!");
                Console.WriteLine("\nMessage:{0}", e.Message);
                Console.WriteLine("\nStatus:{0}", e.Status);
                allDone.Set();
            }
        }


        private void RespLoginCallback(IAsyncResult asynchronousResult)
        {
            try
            {
                // State of request is asynchronous.
                RequestState myRequestState = (RequestState)asynchronousResult.AsyncState;
                HttpWebRequest myHttpWebRequest = myRequestState.request;
                myRequestState.response = (HttpWebResponse)myHttpWebRequest.EndGetResponse(asynchronousResult);

                GetBody(myRequestState);
                if (myRequestState.body.IndexOf("code") >= 0)
                {
                    JObject joBody = (JObject)JsonConvert.DeserializeObject(myRequestState.body);
                    if (string.Compare((string)joBody["code"], "1", true) == 0)
                    {
                        Program.form1.UpdateDataGridView(strAccount, Column.Login, "成功");
                        bLoginSuccess = true;
                    }
                }

                allDone.Set();
                return;
            }
            catch (WebException e)
            {
                Console.WriteLine("\nRespCallback Exception raised!");
                Console.WriteLine("\nMessage:{0}", e.Message);
                Console.WriteLine("\nStatus:{0}", e.Status);
                allDone.Set();
            }
        }

        private void RespAddressCallback(IAsyncResult asynchronousResult)
        {
            try
            {
                // State of request is asynchronous.
                RequestState myRequestState = (RequestState)asynchronousResult.AsyncState;
                HttpWebRequest myHttpWebRequest = myRequestState.request;
                myRequestState.response = (HttpWebResponse)myHttpWebRequest.EndGetResponse(asynchronousResult);

                GetBody(myRequestState);
                if (myRequestState.body.IndexOf(@"<!DOCTYPE HTML>") >= 0)
                {
                    if (myRequestState.body.IndexOf("address-id=") >= 0)
                    {
                        int nAddrStart = myRequestState.body.IndexOf("address-id=");
                        nAddrStart = myRequestState.body.IndexOf(@"""", nAddrStart) + 1;
                        int nAddrEnd = myRequestState.body.IndexOf(@"""", nAddrStart);
                        strAddressId = myRequestState.body.Substring(nAddrStart, nAddrEnd - nAddrStart);
                    }
                    if (strAddressId == "")
                        Program.form1.UpdateDataGridView(strAccount, Column.Address, "没有地址");
                    else
                        Program.form1.UpdateDataGridView(strAccount, Column.Address, string.Format("成功:{0}", strAddressId));
                    bAddressSuccess = true;
                }

                allDone.Set();
                return;
            }
            catch (WebException e)
            {
                Console.WriteLine("\nRespCallback Exception raised!");
                Console.WriteLine("\nMessage:{0}", e.Message);
                Console.WriteLine("\nStatus:{0}", e.Status);
                allDone.Set();
            }
        }

        private void RespNoneCallback(IAsyncResult asynchronousResult)
        {
            try
            {
                // State of request is asynchronous.
                RequestState myRequestState = (RequestState)asynchronousResult.AsyncState;
                HttpWebRequest myHttpWebRequest = myRequestState.request;
                myRequestState.response = (HttpWebResponse)myHttpWebRequest.EndGetResponse(asynchronousResult);
                return;
            }
            catch (WebException e)
            {
                Console.WriteLine("\nRespCallback Exception raised!");
                Console.WriteLine("\nMessage:{0}", e.Message);
                Console.WriteLine("\nStatus:{0}", e.Status);
            }
        }

        
        private void RespIdcardCallback(IAsyncResult asynchronousResult)
        {
            try
            {
                // State of request is asynchronous.
                RequestState myRequestState = (RequestState)asynchronousResult.AsyncState;
                HttpWebRequest myHttpWebRequest = myRequestState.request;
                myRequestState.response = (HttpWebResponse)myHttpWebRequest.EndGetResponse(asynchronousResult);

                GetBody(myRequestState);
                if (myRequestState.body.IndexOf(@"code") >= 0)
                {
                    JObject joBody = (JObject)JsonConvert.DeserializeObject(myRequestState.body);
                    if (string.Compare((string)joBody["code"], "200", true) == 0)
                    {
                        JArray jaData = (JArray)joBody["data"];
                        if(jaData.Count() >= 1)
                            strIdcard = (string)((JObject)jaData[0])["user_certification_id"];
                    }
                    if (strIdcard == "")
                        Program.form1.UpdateDataGridView(strAccount, Column.IdCard, "没有绑定身份证");
                    else
                        Program.form1.UpdateDataGridView(strAccount, Column.IdCard, string.Format("成功:{0}", strIdcard));
                    bIdcardSuccess = true;
                }

                allDone.Set();
                return;
            }
            catch (WebException e)
            {
                Console.WriteLine("\nRespCallback Exception raised!");
                Console.WriteLine("\nMessage:{0}", e.Message);
                Console.WriteLine("\nStatus:{0}", e.Status);
                allDone.Set();
            }
        }

        private void RespBuyCallback(IAsyncResult asynchronousResult)
        {
            try
            {
                // State of request is asynchronous.
                RequestState myRequestState = (RequestState)asynchronousResult.AsyncState;
                HttpWebRequest myHttpWebRequest = myRequestState.request;
                myRequestState.response = (HttpWebResponse)myHttpWebRequest.EndGetResponse(asynchronousResult);

                GetBody(myRequestState);
                bool bSuccess = false;
                if (myRequestState.body.IndexOf("code") >= 0)
                {
                    JObject joBody = (JObject)JsonConvert.DeserializeObject(myRequestState.body);
                    if (string.Compare((string)joBody["code"], "ok", true) == 0)
                    {
                        Program.form1.UpdateDataGridView(strAccount, Column.Buy1, string.Format("{0}:成功", myRequestState.nBuyTimes));
                        string strData = (string)joBody["data"];
                        int nStartK = strData.IndexOf("_k");
                        if (nStartK > 0)
                        {
                            bSuccess = true;

                            nStartK = strData.IndexOf("=", nStartK) + 1;
                            int nEndK = strData.IndexOf("&", nStartK);
                            strBuyK = strData.Substring(nStartK, nEndK - nStartK);

                            // submit
                            Program.form1.UpdateDataGridView(strAccount, Column.Confirm1, string.Format("{0}", myRequestState.nBuyTimes));
                            myRequestState.request = WebRequest.Create(@"http://buy.juooo.com/Index/createOrder") as HttpWebRequest;
                            myRequestState.request.ProtocolVersion = HttpVersion.Version11;
                            myRequestState.request.Method = "POST";
                            myRequestState.request.Accept = "application/json, text/javascript, */*; q=0.01";
                            myRequestState.request.Headers.Add("Origin", "http://buy.juooo.com");
                            myRequestState.request.Headers.Add("X-Requested-With", "XMLHttpRequest");
                            myRequestState.request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36 Edge/16.16299";
                            myRequestState.request.ContentType = @"application/x-www-form-urlencoded; charset=UTF-8";
                            myRequestState.request.Headers.Add("Accept-Encoding", "gzip, deflate");
                            myRequestState.request.Headers.Add("Accept-Language", "zh-CN,zh;q=0.8");
                            myRequestState.request.CookieContainer = cookieContainer;

                            StringBuilder buffer = new StringBuilder();
                            buffer.AppendFormat("_k={0}", strBuyK);
                            buffer.AppendFormat("&type=1&isClass=buyTicket&shippingId=1");
                            buffer.AppendFormat("&addressId={0}", strAddressId);
                            buffer.AppendFormat("&mobile=&payId=666&isScore=0&isCard=0&isCoupon=0&isUserMoney=0&is_sale_give=undefined_&ticketActivityId=&goods_deduction=&bankActivityId=&activityType=2&orderRemarks=");
                            buffer.AppendFormat("&userCertificationId={0}", strIdcard);
                            buffer.AppendFormat("&shippingScheduleId=&clientCityId=0");
                            Byte[] data = requestEncoding.GetBytes(buffer.ToString());
                            using (Stream stream = myRequestState.request.GetRequestStream())
                            {
                                stream.Write(data, 0, data.Length);
                            }
                            IAsyncResult result = (IAsyncResult)myRequestState.request.BeginGetResponse(new AsyncCallback(RespCreateOrderCallback), myRequestState);
                        }
                    }
                }

                if (!bSuccess)
                {
                    Program.form1.UpdateDataGridView(strAccount, Column.Buy1, string.Format("{0}:失败}", myRequestState.nBuyTimes));
                    allDone.Set();
                }
            }
            catch (WebException e)
            {
                Console.WriteLine("\nRespCallback Exception raised!");
                Console.WriteLine("\nMessage:{0}", e.Message);
                Console.WriteLine("\nStatus:{0}", e.Status);
                allDone.Set();
            }
        }


        private void RespCreateOrderCallback(IAsyncResult asynchronousResult)
        {
            try
            {
                // State of request is asynchronous.
                RequestState myRequestState = (RequestState)asynchronousResult.AsyncState;
                HttpWebRequest myHttpWebRequest = myRequestState.request;
                myRequestState.response = (HttpWebResponse)myHttpWebRequest.EndGetResponse(asynchronousResult);

                GetBody(myRequestState);
                if (myRequestState.body.IndexOf("code") >= 0)
                {
                    JObject joBody = (JObject)JsonConvert.DeserializeObject(myRequestState.body);
                    Program.form1.UpdateDataGridView(strAccount, Column.Confirm1, string.Format("{0}:{1}", myRequestState.nBuyTimes, (string)joBody["code"]));
                }

                allDone.Set();
                return;
            }
            catch (WebException e)
            {
                Console.WriteLine("\nRespCallback Exception raised!");
                Console.WriteLine("\nMessage:{0}", e.Message);
                Console.WriteLine("\nStatus:{0}", e.Status);
                allDone.Set();
            }
        }

        public void Run()
        {
            allDone = new ManualResetEvent(false);
            cookieContainer = new CookieContainer();
            requestEncoding = Encoding.GetEncoding("utf-8");

            int nLoginTimes = 1;
            bLoginSuccess = false;
            while (true)
            {
                Program.form1.UpdateDataGridView(strAccount, Column.Login, string.Format("开始登录:{0}", nLoginTimes));
                try
                {
                    allDone.Reset();

                    RequestState requestState = new RequestState();
                    ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(Http.CheckValidationResult);
                    requestState.request = WebRequest.Create(@"http://passport.juooo.com/User/login") as HttpWebRequest;
                    requestState.request.ProtocolVersion = HttpVersion.Version11;
                    requestState.request.Method = "GET";
                    requestState.request.Accept = "text/html, application/xhtml+xml, image/jxr, */*";
                    requestState.request.Headers.Add("Accept-Language", "zh-Hans-CN,zh-Hans;q=0.5");
                    requestState.request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36 Edge/16.16299";
                    requestState.request.Headers.Add("Accept-Encoding", "gzip, deflate"); 
                    requestState.request.CookieContainer = cookieContainer;
                    IAsyncResult result = (IAsyncResult)requestState.request.BeginGetResponse(new AsyncCallback(RespFirstCallback), requestState);
                    allDone.WaitOne();
                }
                catch (WebException e)
                {
                    Console.WriteLine("\nRespCallback Exception raised!");
                    Console.WriteLine("\nMessage:{0}", e.Message);
                    Console.WriteLine("\nStatus:{0}", e.Status);
                }
                
                if (bLoginSuccess)
                {
                    break;
                }
                nLoginTimes++;
                if (nLoginTimes > 3)
                {
                    Program.form1.UpdateDataGridView(strAccount, Column.Login, string.Format("放弃"));
                    return;
                }
                Thread.Sleep(500);
            }

            int nAddressTimes = 1;
            bAddressSuccess = false;
            while (true)
            {
                Program.form1.UpdateDataGridView(strAccount, Column.Address, string.Format("开始:{0}", nAddressTimes));
                try
                {
                    allDone.Reset();

                    RequestState requestState = new RequestState();
                    ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(Http.CheckValidationResult);
                    requestState.request = WebRequest.Create(@"http://myjuooo.juooo.com/User/myaddress") as HttpWebRequest;
                    requestState.request.ProtocolVersion = HttpVersion.Version11;
                    requestState.request.Method = "GET";
                    requestState.request.Accept = "text/html, application/xhtml+xml, image/jxr, */*";
                    requestState.request.Referer = "http://myjuooo.juooo.com/User/myorderlist";
                    requestState.request.Headers.Add("Accept-Language", "zh-Hans-CN,zh-Hans;q=0.5");
                    requestState.request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36 Edge/16.16299";
                    requestState.request.Headers.Add("Accept-Encoding", "gzip, deflate");
                    requestState.request.CookieContainer = cookieContainer;
                    IAsyncResult result = (IAsyncResult)requestState.request.BeginGetResponse(new AsyncCallback(RespAddressCallback), requestState);
                    allDone.WaitOne();
                }
                catch (WebException e)
                {
                    Console.WriteLine("\nRespCallback Exception raised!");
                    Console.WriteLine("\nMessage:{0}", e.Message);
                    Console.WriteLine("\nStatus:{0}", e.Status);
                }

                if (bAddressSuccess)
                {
                    break;
                }
                nAddressTimes++;
                if (nAddressTimes > 10)
                {
                    Program.form1.UpdateDataGridView(strAccount, Column.Address, string.Format("放弃"));
                    return;
                }
            }

            if (strAddressId == "")
                return;

            int nIdcardTimes = 1;
            bIdcardSuccess = false;
            while (true)
            {
                Program.form1.UpdateDataGridView(strAccount, Column.IdCard, string.Format("开始:{0}", nIdcardTimes));
                try
                {
                    allDone.Reset();

                    RequestState requestState = new RequestState();
                    ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(Http.CheckValidationResult);
                    requestState.request = WebRequest.Create(@"http://buy.juooo.com/Index/ajax?action=getUserIdNumber") as HttpWebRequest;
                    requestState.request.ProtocolVersion = HttpVersion.Version11;
                    requestState.request.Method = "POST";
                    requestState.request.Accept = "application/json, text/javascript, */*; q=0.01";
                    requestState.request.Headers.Add("X-Requested-With", "XMLHttpRequest");
                    requestState.request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36 Edge/16.16299";
                    requestState.request.Headers.Add("Accept-Encoding", "gzip, deflate");
                    requestState.request.Headers.Add("Accept-Language", "zh-CN,zh;q=0.8"); 
                    requestState.request.CookieContainer = cookieContainer;
                    IAsyncResult result = (IAsyncResult)requestState.request.BeginGetResponse(new AsyncCallback(RespIdcardCallback), requestState);
                    allDone.WaitOne();
                }
                catch (WebException e)
                {
                    Console.WriteLine("\nRespCallback Exception raised!");
                    Console.WriteLine("\nMessage:{0}", e.Message);
                    Console.WriteLine("\nStatus:{0}", e.Status);
                }

                if (bIdcardSuccess)
                {
                    break;
                }
                nIdcardTimes++;
                if (nIdcardTimes > 10)
                {
                    Program.form1.UpdateDataGridView(strAccount, Column.IdCard, string.Format("放弃"));
                    return;
                }
            }

            DateTime lastTime = DateTime.Now;
            while ((DateTime.Now < AllPlayers.dtStartTime))
            {
                // maintain login
                if ((AllPlayers.dtStartTime - DateTime.Now).TotalMilliseconds > 60000 && (DateTime.Now - lastTime).TotalMilliseconds > 60000 * 5)
                {
                    try
                    {
                        RequestState requestState = new RequestState();
                        ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(Http.CheckValidationResult);
                        requestState.request = WebRequest.Create(@"http://myjuooo.juooo.com/User/myaddress") as HttpWebRequest;
                        requestState.request.ProtocolVersion = HttpVersion.Version11;
                        requestState.request.Method = "GET";
                        requestState.request.Accept = "text/html, application/xhtml+xml, image/jxr, */*";
                        //requestState.request.Referer = "http://myjuooo.juooo.com/User/myorderlist";
                        requestState.request.Headers.Add("Accept-Language", "zh-Hans-CN,zh-Hans;q=0.5");
                        requestState.request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36 Edge/16.16299";
                        requestState.request.Headers.Add("Accept-Encoding", "gzip, deflate");
                        requestState.request.CookieContainer = cookieContainer;
                        IAsyncResult result = (IAsyncResult)requestState.request.BeginGetResponse(new AsyncCallback(RespNoneCallback), requestState);
                    }
                    catch (WebException e)
                    {
                        Console.WriteLine("\nRespCallback Exception raised!");
                        Console.WriteLine("\nMessage:{0}", e.Message);
                        Console.WriteLine("\nStatus:{0}", e.Status);
                    }

                    lastTime = DateTime.Now;
                }

                if ((AllPlayers.dtStartTime - DateTime.Now).TotalMilliseconds > 60000)
                    Thread.Sleep(60000);
                else if ((AllPlayers.dtStartTime - DateTime.Now).TotalMilliseconds > 1000)
                    Thread.Sleep(1000);
                else if ((AllPlayers.dtStartTime - DateTime.Now).TotalMilliseconds > 50)
                    Thread.Sleep(50);
                else
                    Thread.Sleep(1);
            }


            int nBuyTimes = 1;
            nBuyIndex = 0;
            strBuyK = "";
            
            while ((DateTime.Now <= AllPlayers.dtEndTime))
            {
                try
                {
                    allDone.Reset();
                    int nProductId = AllPlayers.listTicketData[0].productId[nBuyIndex];

                    Program.form1.UpdateDataGridView(strAccount, Column.Buy1, string.Format("{0}:{1}", nBuyTimes, nProductId));
                    ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(Http.CheckValidationResult);
                    RequestState requestState = new RequestState();
                    requestState.nBuyTimes = nBuyTimes;
                    requestState.request = WebRequest.Create(@"http://item.juooo.com/Index/buyTickets") as HttpWebRequest;
                    requestState.request.ProtocolVersion = HttpVersion.Version11;
                    requestState.request.Method = "POST";
                    requestState.request.Accept = "application/json, text/javascript, */*; q=0.01";
                    requestState.request.Headers.Add("Origin", "http://item.juooo.com");
                    requestState.request.Headers.Add("X-Requested-With", "XMLHttpRequest");
                    requestState.request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36 Edge/16.16299";
                    requestState.request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                    requestState.request.Headers.Add("Accept-Encoding", "gzip, deflate");
                    requestState.request.Headers.Add("Accept-Language", "zh-CN,zh;q=0.8");
                    requestState.request.CookieContainer = cookieContainer;

                    StringBuilder buffer = new StringBuilder();
                    buffer.AppendFormat(@"type=1&tickets={0}_{1}_2", nProductId, AllPlayers.listTicketData[0].quantity);
                    Byte[] data = requestEncoding.GetBytes(buffer.ToString());
                    using (Stream stream = requestState.request.GetRequestStream())
                    {
                        stream.Write(data, 0, data.Length);
                    } 
                       
                    IAsyncResult result = (IAsyncResult)requestState.request.BeginGetResponse(new AsyncCallback(RespBuyCallback), requestState);
                    allDone.WaitOne();

                    nBuyIndex = (nBuyIndex + 1) % AllPlayers.listTicketData[0].productId.Count();
                }
                catch (WebException e)
                {
                    Console.WriteLine("\nRespCallback Exception raised!");
                    Console.WriteLine("\nMessage:{0}", e.Message);
                    Console.WriteLine("\nStatus:{0}", e.Status);
                }

                nBuyTimes++;
                if (AllPlayers.nInterval > 0)
                    Thread.Sleep(AllPlayers.nInterval);
            }
        }
    };

    class TicketData
    {
        public List<int> productId = new List<int>();
        public int quantity = 0;
    }

    class AllPlayers
    {
        public static bool bSetProxy = false;
        public static int nInterval = 1000;
        public static DateTime dtStartTime;
        public static DateTime dtEndTime;
        public static List<TicketData> listTicketData = new List<TicketData>();
        public static List<Player> listPlayer = new List<Player>();

        public static void Init()
        {
            string szConfigFileName = System.Environment.CurrentDirectory + @"\" + @"config.txt";
            string szAccountFileName = System.Environment.CurrentDirectory + @"\" + @"account.csv";

            string[] arrayConfig = File.ReadAllLines(szConfigFileName);
            JObject joInfo = (JObject)JsonConvert.DeserializeObject(arrayConfig[0]);
            dtStartTime = DateTime.Parse((string)joInfo["StartTime"]);
            dtEndTime = DateTime.Parse((string)joInfo["EndTime"]);
            nInterval = (int)joInfo["interval"];
            listTicketData = new List<TicketData>();
            JArray jaData = (JArray)joInfo["data"];
            foreach (JObject ticket in jaData)
            {
                TicketData ticketData = new TicketData();
                JArray jaProductId = (JArray)ticket["productId"];
                foreach (JToken id in jaProductId)
                {
                    ticketData.productId.Add((int)id);                
                }
                ticketData.quantity = (int)ticket["quantity"];
                listTicketData.Add(ticketData);
            }


            Program.form1.Form1_Init();

            listPlayer = new List<Player>();
            string[] arrayText = File.ReadAllLines(szAccountFileName);
            int nIndex = 0;
            for (int i = 0; i < arrayText.Length; ++i)
            {
                string[] arrayParam = arrayText[i].Split(new char[] { ',' });
                if (arrayParam.Length >=4)
                {
                    Player player = new Player();
                    player.strAccount = arrayParam[0];
                    player.strPassword = arrayParam[1];
                    player.strUserName1 = arrayParam[2];
                    player.strCard1 = arrayParam[3];
                    if (arrayParam.Length >= 6)
                    {
                        player.strUserName2 = arrayParam[4];
                        player.strCard2 = arrayParam[5];
                    }
                    player.thread = new Thread(new ThreadStart(player.Run));
                    player.nIndex = nIndex++;
                    listPlayer.Add(player);
                    Program.form1.dataGridViewInfo_AddRow(player.strAccount);
                }
            }
        }


        public static void Run()
        {
            foreach (Player player in listPlayer)
            {
                player.thread.Start();
                Thread.Sleep(500);
            }

            foreach (Player player in listPlayer)
            {
                player.thread.Join();
            }

            Program.form1.richTextBoxStatus_AddString("任务完成!\n");
            Program.form1.button1_Enabled();
        }
    };
    
    
    
    class Http
    {
        public static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return true; //总是接受  
        }
                
        public static string Timestamp()
        {
            TimeSpan span = (DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime());
            return ((ulong)span.TotalMilliseconds).ToString();
        }

        public static string UserMd5(string str)
        {
            string cl = str;
            string pwd = "";
            MD5 md5 = MD5.Create();//实例化一个md5对像
            // 加密后是一个字节类型的数组，这里要注意编码UTF8/Unicode等的选择　
            byte[] s = md5.ComputeHash(Encoding.UTF8.GetBytes(cl));
            // 通过使用循环，将字节类型的数组转换为字符串，此字符串是常规字符格式化所得
            for (int i = 0; i < s.Length; i++)
            {
                // 将得到的字符串使用十六进制类型格式。格式后的字符是小写的字母，如果使用大写（X）则格式后的字符是大写字符
                pwd = pwd + s[i].ToString("x2");
            }
            return pwd;
        }

    }
}
