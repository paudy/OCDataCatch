using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Xml;
using LitJson;
using System.Windows.Forms;
using System.Data.SqlClient;//.SqlConnection;

namespace OCAPICatch
{
    public partial class FormMain : Form
    {
        public FormMain()
        {
            InitializeComponent();
            LoadConfigFile();
        }

        private string strDBHost;
        private string strDBCatalog;
        private string strDBUser;
        private string strDBPwd;

        private void LoadConfigFile()
        {
            string strConfigFile="";
            //将XML文件加载进来
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load("config.xml");
            if (xmlDoc == null) return;

            //获取到XML的根元素进行操作
            XmlNode root = xmlDoc.GetElementsByTagName("config").Item(0);

            XmlNode node = root.SelectSingleNode("dbinfo");
            strDBHost = node.Attributes["host"].Value.ToString();
            strDBCatalog = node.Attributes["catalog"].Value.ToString();
            strDBUser = node.Attributes["user"].Value.ToString();
            strDBPwd = node.Attributes["pwd"].Value.ToString();

            XmlNode node2 = root.SelectSingleNode("interval");
            string strIntervalSeconds = node2.Attributes["seconds"].Value.ToString();

        }

        private string lastExpectNo = "";
        private SqlConnection sqlCnt;

        private void OpenBJPK10DB()
        {
            //连接数据库
            try
            {
                string connectString = "Data Source = "+ strDBHost + "; Initial Catalog = " + strDBCatalog + "; Persist Security Info = True; User ID = sa; PWD = zbd;";
                sqlCnt = new SqlConnection(connectString);
                sqlCnt.Open();
            }
            catch (Exception ex) { this.textBoxXmlResult.Text = "连接数据库错误：" + ex.Message; }
        }

        private void CloseBJPK10DB()
        {
            //关闭DB连接
            sqlCnt.Close();
        }

        private void GetXMLData(string strURL)
        {
            try
            {
                string html = api.HttpGet(strURL, Encoding.UTF8);
                if (!html.Substring(0, 5).Equals("<?xml", StringComparison.OrdinalIgnoreCase))
                    throw new Exception(html);
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(html);
                this.textBoxXmlResult.Text = "采集方式：UTF8编码标准下的Xml格式\r\n";
                this.textBoxXmlResult.Text += "采集时间：" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff") + "\r\n";
                this.textBoxXmlResult.Text += "采集行数：" + xml.SelectSingleNode("xml").Attributes["rows"].Value + "行\r\n";

                OpenBJPK10DB();

                //解析XML数据
                foreach (XmlNode node in xml.SelectNodes("xml/row"))
                {
                    string strExpect = node.Attributes["expect"].Value;
                    string strOpenCode = node.Attributes["opencode"].Value;
                    string strOpenTime = node.Attributes["opentime"].Value;

                    this.textBoxXmlResult.Text += "\r\n";
                    this.textBoxXmlResult.Text += "开奖期号：" + strExpect + "\r\n";
                    this.textBoxXmlResult.Text += "开奖号码：" + strOpenCode + "\r\n";
                    this.textBoxXmlResult.Text += "开奖时间：" + strOpenTime + "\r\n";

                    //if (strExpect.CompareTo(lastExpectNo) <= 0)
                    //{
                    //    //"忽略重复保存期号：" + strExpect;
                    //    continue;
                    //}
                    //else
                    //{
                    //    lastExpectNo = strExpect;
                    //}

                    //保存到数据库
                    Save2DB_PK10(strExpect, strOpenCode, strOpenTime);
                                        
                }
               
                CloseBJPK10DB();

            }
            catch (Exception ex) { this.textBoxXmlResult.Text = "采集出现错误：" + ex.Message; }
        }

        private void buttonXml_Click(object sender, EventArgs e)
        {

            if (buttonXml.Text == "停止")
            {
                buttonXml.Text = "开始";
                timer1.Enabled = false;
            }
            else
            {
                buttonXml.Text = "停止";
                timer1.Enabled = true;
                GetXMLData(this.textBoxXml.Text);
            }

        }

        private void GetJsonData(string strURL)
        {
            try
            {
                string html = api.HttpGet(strURL, Encoding.UTF8);
                if (!html.Substring(0, 5).Equals("{\"row", StringComparison.OrdinalIgnoreCase))
                    throw new Exception(html);

                JsonData json = JsonMapper.ToObject(html);
                this.textBoxJsonResult.Text = "采集方式：UTF8编码标准下的Json格式\r\n";
                this.textBoxJsonResult.Text += "采集时间：" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff") + "\r\n";
                this.textBoxJsonResult.Text += "采集行数：" + json["rows"].ToString() + "行\r\n";
                foreach (JsonData row in json["data"])
                {
                    this.textBoxJsonResult.Text += "\r\n";
                    this.textBoxJsonResult.Text += "开奖期号：" + row["expect"].ToString() + "\r\n";
                    this.textBoxJsonResult.Text += "开奖号码：" + row["opencode"].ToString() + "\r\n";
                    this.textBoxJsonResult.Text += "开奖时间：" + row["opentime"].ToString() + "\r\n";
                }
            }
            catch (Exception ex) { this.textBoxJsonResult.Text = "采集出现错误：" + ex.Message; }
        }

        private void buttonJson_Click(object sender, EventArgs e)
        {
            GetJsonData(this.textBoxJson.Text);
        }

        private void label3_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.opencai.net/");
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            // Set the caption to the current time.  
            //label4.Text = DateTime.Now.ToString();
            //获取数据
            GetXMLData(this.textBoxXml.Text);
        }

        private void InitializeTimer()
        {
            // 调用本方法开始用计算器          
            timer1.Interval = 10000;//设置时钟周期为10秒（1000毫秒）
            timer1.Tick += new EventHandler(timer1_Tick);
            // Enable timer.
            timer1.Enabled = true;            
            buttonXml.Text = "停止";
            //buttonXml.Click += new EventHandler(buttonXml_Click);
            GetXMLData(this.textBoxXml.Text);
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            InitializeTimer();
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            DialogResult result = MessageBox.Show("确定退出数据采集程序吗", "BJPK10采集", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
            if (result == DialogResult.OK)
            {
                e.Cancel = false;  //点击OK   
            }
            else
            {
                e.Cancel = true;
            }
        }

        private void Save2DB_PK10(string strExpect, string strOpenCode, string strOpenTime)
        {

            try {
                //执行查询

                //实例化一个SqlCommand对象
                //SqlCommand command = new SqlCommand();
                //command.Connection = sqlCnt;            // 绑定SqlConnection对象

                //SqlCommand常用操作
                //① 执行SQL
                //SqlCommand cmd = sqlCnt.CreateCommand();              //创建SqlCommand对象
                //cmd.CommandType = CommandType.Text;
                //cmd.CommandText = "select * from [dbo].[BJPK10Data] where ID = @ID";   //sql语句
                //cmd.Parameters.Add("@ID", SqlDbType.Int);
                //cmd.Parameters["@ID"].Value = 1;                    //给参数sql语句的参数赋值
                //cmd.ExecuteReader(CommandBehavior.Default);                                

                //② 调用存储过程
                SqlCommand cmd2 = sqlCnt.CreateCommand();
                cmd2.CommandType = System.Data.CommandType.StoredProcedure;
                cmd2.CommandText = "[dbo].[ZSP_SavePK10Data]";//"存储过程名";
                cmd2.Parameters.Add("@ExpectNO", SqlDbType.NVarChar);
                cmd2.Parameters["@ExpectNO"].Value = strExpect;
                cmd2.Parameters.Add("@OpenCode", SqlDbType.NVarChar);
                cmd2.Parameters["@OpenCode"].Value = strOpenCode;
                cmd2.Parameters.Add("@OpenTime", SqlDbType.NVarChar);
                cmd2.Parameters["@OpenTime"].Value = strOpenTime;
                int nr = cmd2.ExecuteNonQuery();

            } catch(Exception ex) {
                this.textBoxXmlResult.Text = "查询数据库错误：" + ex.Message;
            }
        }

    }
}