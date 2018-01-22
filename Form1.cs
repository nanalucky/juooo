using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace juooo
{
    public enum Column
    {
        Telephone = 0,
        Login,
        Address,
        IdCard,
        Buy1,
        Confirm1,
        Buy2,
        Confirm2, 
        Buy3,
        Confirm3,
        Buy4,
        Confirm4,
        Buy5,
        Confirm5,
    };
    
    
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        public void Form1_Init()
        {
            dataGridViewInfo.Rows.Clear();
        }       
        
        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            AllPlayers.Init();
            //AllPlayers.Run();
            //AllPlayers.Base64ToJPG();
            //AllPlayers.ReadSecurityImage();

            Thread thread = new Thread(new ThreadStart(AllPlayers.Run));
            thread.Start();
        }
            
        public void dataGridViewInfo_AddRow(string _phone)
        {
            dataGridViewInfo.Rows.Add(_phone);
        }

        public delegate void DelegateUpdateDataGridView(string telephone, Column colIndex, string colValue);
        public void UpdateDataGridView(string telephone, Column colIndex, string colValue)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new DelegateUpdateDataGridView(UpdateDataGridView), new object[] { telephone, colIndex, colValue });
            }

            int nCount = dataGridViewInfo.Rows.Count;
            for (int i = 0; i < nCount; ++i)
            {
                if ((string)dataGridViewInfo[(int)Column.Telephone, i].Value == telephone)
                {
                    dataGridViewInfo[(int)colIndex, i].Value = colValue;
                    return;
                }
            }
        }

        public delegate void DelegateButton1_Enabled();
        public void button1_Enabled()
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new DelegateButton1_Enabled(button1_Enabled), new object[] { });
                return;
            }

            button1.Enabled = true;
        }

        public delegate void DelegateRichTextBoxStatus_AddString(string strAdd);
        public void richTextBoxStatus_AddString(string strAdd)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new DelegateRichTextBoxStatus_AddString(richTextBoxStatus_AddString), new object[] { strAdd });
                return;
            }

            richTextBoxStatus.Focus();
            //设置光标的位置到文本尾   
            richTextBoxStatus.Select(richTextBoxStatus.TextLength, 0);
            //滚动到控件光标处   
            richTextBoxStatus.ScrollToCaret();
            richTextBoxStatus.AppendText(strAdd);
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Environment.Exit(0);
        }

    }
}
