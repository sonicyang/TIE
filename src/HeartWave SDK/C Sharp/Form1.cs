using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.IO.Ports;
using System.Xml;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms.DataVisualization.Charting;
using Microsoft.Win32;
using System.Diagnostics;

delegate void MyDelegate(string s, string p);


namespace ECGDisplay
{
    public partial class Form1 : Form
    {
        private string userpath;
        public SerialPort com;
        string sBuffer = String.Empty;
        private int baudrate = 115200; 
        private bool conn,pflag,stopFlag;
        private string modulename;
        private int a = 0; //for ecg x-axis
        DataTable hrtable;//HR curve 
        DataTable rritable;//RRI curve
        DataTable sdnntable;//SDNN curve
        DataTable hrvtable;//HRV curve
        private byte[] pastAry;//for compare
        private string pastdata;//for compare
        private string tranXML;//for rx data
        private int lastY;
        private string dumpname, asciidata,rawfile;        
        private int cnt = 1; //time count (sec)
        private int k = 0; //self test file data length
        public Graphics gp;
        Bitmap BB;
        private int lastRRI;
        private int RRI1, RRI2;
        List<string> RRIList = new List<string>();
        private string[] Testary;
        private double[] hrvPer;
        private int[,] RGB = new int[,] {  {255,128,128},
                                                                    {255,255,128},
                                                                    {128,255,128},
                                                                    {128,255,255},
                                                                    {0,128,255},  
                                                                    {255,128,255},
                                                                    {255,0,0},    
                                                                    {128,128,192},
                                                                    {128,0,64},   
                                                                    {224,192,128},
                                                                    {255,128,0},  
                                                                    {255,80,192},  
                                                                    {128,0,0},    
                                                                    {64,192,255}, 
                                                                    {128,0,255},  
                                                                    {0,0,128},    
                                                                    {192,192,192},
                                                                    {128,128,64}, 
                                                                    {64,128,128}, 
                                                                    {128,128,128}
                                                                };
        private int thrMax =900 ;
        private int thrlim = 600;
        public Form1()
        {
            userpath = System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) +"\\bomdicECG";
            if (!Directory.Exists(userpath))
                Directory.CreateDirectory(userpath);
            InitializeComponent();
           
            conn = false;
            pflag = false;
            stopFlag = false;
            tableinit();
            BaseTime = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 0, 0, 0);
            GetSerialPort();
            hrvPer = new double[4];
        }
        #region select comport
        /*System.IO.Ports.SerialPort.GetPortNames error with BlueTooth    
            https://connect.microsoft.com/VisualStudio/feedback/details/236183/system-io-ports-serialport-getportnames-error-with-bluetooth
         * 會遇到亂碼,所以要多判斷是否為digit
         */
       
        private void GetSerialPort()   //獲取序列列表
        {
            string selectport = getPortName();
            RegistryKey keyCom = Registry.LocalMachine.OpenSubKey("Hardware\\DeviceMap\\SerialComm");
            if (keyCom != null)
            {
                interfaceToolStripMenuItem.DropDownItems.Clear();
                string[] sSubKeys = keyCom.GetValueNames();
                int a = 0;
                List<int> intList = new List<int>();
                foreach (string sName in sSubKeys)
                {
                    string sValue = "";
                    if (sName.Contains("BthModem"))
                    {
                        sValue = (string)keyCom.GetValue(sName);
                        for (int i = 3; i < sValue.Length; i++) // 3 = len of COM prefix
                            if (!char.IsDigit(sValue, i))
                            {
                                sValue = sValue.Substring(0, i);
                           //     keyCom.SetValue(sName, sValue); // correct the registry
                                break;
                            }
                        intList.Add( Convert.ToInt32(sValue.Replace("COM", "")));                                                                     
                    }
                    else
                    {
                        sValue = (string)keyCom.GetValue(sName);
                        interfaceToolStripMenuItem.DropDownItems.Add(sValue, null, new EventHandler(NoPort_Click));
                        if (sValue == selectport)
                            ((ToolStripMenuItem)(interfaceToolStripMenuItem.DropDownItems[a])).Checked = true;
                        a++;
                    }
                }
                if(intList.Count>0){
                    intList.Sort();
                    for(int i=0;i<intList.Count;i+=2){
                         interfaceToolStripMenuItem.DropDownItems.Add("COM"+intList[i].ToString()+"(BT)", null, new EventHandler(NoPort_Click));
                         if ("COM" + intList[i].ToString() + "(BT)" == selectport)
                             ((ToolStripMenuItem)(interfaceToolStripMenuItem.DropDownItems[a])).Checked = true;
                         a++;
                    }

                }
            }
        }
        private void NoPort_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem mi = (ToolStripMenuItem)sender;
            for (int i = 0; i < interfaceToolStripMenuItem.DropDownItems.Count; i++)
            {
                ((ToolStripMenuItem)(interfaceToolStripMenuItem.DropDownItems[i])).Checked = false;
            }
            mi.Checked = true;           
        }
        #endregion
       
      
        private void addbubble(int x,int y)
        {
            float rx, ry;
            rx = 0; ry = 0;
            rx = 4 * (x - 150) / 45.0F + 100;
            ry = 142 - (4 * (y - 150) / 45.0F);
       
            RectangleF rect = new RectangleF( rx, ry, 2.0F, 2.0F);
            Pen pen = new Pen(Color.FromArgb(255, 255 , 255), 1); //change color;
            try
            {
                if (cnt / 120 > 0)
                {
                    int cls = (cnt / 120) - 1;
                    if (cls >= RGB.Length/3)
                        cls /= RGB.Length;
                    pen = new Pen(Color.FromArgb(RGB[cls, 0], RGB[cls, 1], RGB[cls, 2]), 1); //change color
                }
            }
            catch {
                pen = new Pen(Color.FromArgb(RGB[1, 0], RGB[1, 1], RGB[1, 2]), 1); //change color
            }

            gp.DrawPie(pen, rect, 0.0F, 45.0F);
         
            gp.Save();
            try
            {
               
                MyDelegate picText = setLog;
                object[] Obj;
                Obj = new object[2] { "", "pic3" };

                pictureBox3.Invoke(picText, Obj);                    
            }
            catch(Exception) { 
            }            
        }
        private void main()
        {
            string portname = getPortName();
           
            if (portname.Contains("(BT)"))
                portname = portname.Replace("(BT)","");
            com = new SerialPort(portname, baudrate, Parity.None, 8, StopBits.One);
            try
            {
                com.Open();
            }
            catch (Exception ex)
            {
                listBox1.Items.Add( "開啟錯誤:" + ex.Message );
                pictureBox2.Image = ECGDisplay.Properties.Resources.heart1;
            }
            if (com.IsOpen)
            {
                listBox1.Items.Add ( "同步裝置...");
                com.DataReceived += new SerialDataReceivedEventHandler(com_DataReceived);
                listBox1.Items.Add("等待接收資料...");
                pictureBox2.Visible = true;
                cnt = 0;
                hTag = 0;
               
                rritable.Rows.Clear();
                hrtable.Rows.Clear();
                sdnntable.Rows.Clear();
                hrvtable.Rows.Clear();
                filtercheck.Visible = true;
                revcheckBox.Visible = true;
                pictureBox3.Visible = false;
                pictureBox4.Visible = false;
               
                chart2.Visible = false;
                chart3.Visible = false;
                chart4.Visible = false;
                chart5.Visible = false;
                chart6.Visible = false;
                //chart7.Visible = false;
                k = 0;
            }
            else
            {
                listBox1.Items.Add (portname + " 開啟失敗");
                connectToolStripMenuItem.Text = "Connect";

                replayECGToolStripMenuItem.Enabled = true;
                conn = false;
                pictureBox2.Image = ECGDisplay.Properties.Resources.heart1;
            }
        }
    
        private DataTable SDNNUpdate(DataTable table)
        {
            if (cnt > thrMax)
            {
                 string preTime = Convert.ToDateTime(table.Rows[0]["x"]).ToString("mm:ss");
                 string[] timeary = preTime.Split(':');
                int totaltime =0;
                if (timeary.Length > 2)
                    totaltime = Convert.ToInt32(timeary[0]) * 3600 +Convert.ToInt32(timeary[1])*60 + Convert.ToInt32(timeary[2]);
                else if(timeary.Length>1)
                    totaltime = Convert.ToInt32(timeary[0])*60 +Convert.ToInt32(timeary[1]);
                else
                     totaltime =Convert.ToInt32(timeary[1]);
                if (totaltime < cnt - thrlim)
                {
                    string timeth = BaseTime.AddSeconds(cnt - thrlim).ToString();
                    string expression = "x >'"+timeth+"'";
                    //string sortOrder = "x ASC";
                   
                    DataTable sdnntable2 = new DataTable("SDNN");
                    sdnntable2.Columns.Add(new DataColumn("x", typeof(DateTime))); //time
                    sdnntable2.Columns.Add(new DataColumn("y", typeof(double)));  // 1.5 min SDNN
                    sdnntable2.Columns.Add(new DataColumn("y2", typeof(double)));  //5 min SDNN
                    sdnntable2.Columns.Add(new DataColumn("y3", typeof(double)));  //250 RRI SDNN

                    DataRow[] foundRows;
                    foundRows = table.Select(expression);

                    foreach (DataRow row in foundRows)
                    {
                        sdnntable2.Rows.Add(row[0], row[1], row[2], row[3]);             
                    }
                    return sdnntable2;
                }
            }
            return table;
        }

        private DataTable dtUpdate(DataTable table,string type)
        {
            if (table.Rows.Count > thrMax)
            {
                DataTable table2;
                switch (type)
                {
                    case "HR":
                        table2 = new DataTable("HR");
                        break;
                    case "RRI":
                        table2 = new DataTable("RRI");
                        break;
                    default:
                        table2 = new DataTable("DEL");
                        break;
                }
                table2.Columns.Add(new DataColumn("x", typeof(string))); //time
                table2.Columns.Add(new DataColumn("y", typeof(int)));  //HR

                for (int i = table.Rows.Count - thrlim; i < table.Rows.Count; i++)
                    table2.Rows.Add(table.Rows[i][0], table.Rows[i][1]);
                return table2;
            }
            return table;
        }
       
        private void tableinit()
        {
                hrtable = new DataTable("HR");
                hrtable.Columns.Add(new DataColumn("x", typeof(string))); //time
                hrtable.Columns.Add(new DataColumn("y", typeof(int)));  //HR
                rritable = new DataTable("RRI");
                rritable.Columns.Add(new DataColumn("x", typeof(string))); //time
                rritable.Columns.Add(new DataColumn("y", typeof(int)));  //RRI
                sdnntable = new DataTable("SDNN");
                sdnntable.Columns.Add(new DataColumn("x", typeof(DateTime))); //time
                sdnntable.Columns.Add(new DataColumn("y", typeof(double)));  // 1.5 min SDNN
                sdnntable.Columns.Add(new DataColumn("y2", typeof(double)));  //5 min SDNN
                sdnntable.Columns.Add(new DataColumn("y3", typeof(double)));  //250 RRI SDNN
                hrvtable = new DataTable("HRV");
                hrvtable.Columns.Add(new DataColumn("x", typeof(DateTime))); //time
                hrvtable.Columns.Add(new DataColumn("vlf", typeof(double)));  //VLF PWR
                hrvtable.Columns.Add(new DataColumn("lf", typeof(double)));  //LF PWR
                hrvtable.Columns.Add(new DataColumn("hf", typeof(double)));  //HF PWR
        
        }
        private string[] checkXML(string input)
        {
            string pattern = @"(<B>.*?</B>)"; // 規則字串
            Regex regex = new Regex(pattern, RegexOptions.IgnoreCase); // 宣告 Regex 忽略大小寫
            MatchCollection matches = regex.Matches(input); // 將比對後集合傳給 MatchCollection

            int index = 0;
            string[] ary;
            if (matches.Count > 0)
            {
                 ary = new string[matches.Count];

                foreach (Match match in matches) // 一一取出 MatchCollection 內容
                {
                    ary[index] = match.Value.Trim();
                    index++;
                }
            }
            else
            {
               ary= new string[0];
            }
            return ary;
        }
        private void rxWorking(string sBuffer){
            string rawdata = "";
            string HR = "", tag = "", Rpeak = "", HQ = "", F1 = "", F2 = "", Y = "";
            try
            {
                    XmlTextReader readingxml = new XmlTextReader(new StringReader(sBuffer.ToString()));
                    XmlDocument  doc = new XmlDocument();
                    doc.Load(readingxml);
                    try
                    {
                        HR = doc.SelectSingleNode("//B/E/H").InnerText;                       
                    }
                    catch 
                    { 
                        HR = "0";
                    }
                    modulename = doc.SelectSingleNode("//B/E/M").InnerText;
                    rawdata = doc.SelectSingleNode("//B/E/D").InnerText;
                    tag = doc.SelectSingleNode("//B/E/T").InnerText;
                    Rpeak = doc.SelectSingleNode("//B/E/P").InnerText;
                    HQ = doc.SelectSingleNode("//B/E/S").InnerText;
                    F1 = doc.SelectSingleNode("//B/E/F1").InnerText;//function (gain) 
                    F2 = doc.SelectSingleNode("//B/E/F2").InnerText; // 1/0
                    Y = doc.SelectSingleNode("//B/E/Y").InnerText; //電量
                    string samplerate = doc.SelectSingleNode("//B/E/R").InnerText;
                    if (HQ == "1")
                    {
                        string RRI;
                        try
                        {
                            RRI = doc.SelectSingleNode("//B/E/I").InnerText;
                            if (RRI != "")
                            {
                                string[] dataRRI = RRI.Split(',');
                                for (int i = 0; i < dataRRI.Length; i++)
                                {
                                    rritable.Rows.Add(getTime(cnt), Convert.ToInt32(dataRRI[i]) * (1000 / 255));
                                    if (dataRRI[i] != "" && dataRRI[i] != "0")
                                        saveFile(asciidata, string.Format("{0:0.000}", Convert.ToDecimal(dataRRI[i]) / 255));
                                }

                            }
                        }
                        catch
                        {
                            RRI = "";
                        }
                        bubbleRRI(RRI);
                        procRRI4SDNN(RRI);


                    } 
                    cnt++;
                    if (rawdata != "")
                    {
                        procData(rawdata, HR, tag, Rpeak,HQ,Y,F1,F2);
                        tranXML = sBuffer.ToString();
                        //#define 如果data相同
                        if (rawdata == pastdata)
                        {
                            string bodyCont = DateTime.Now.ToString("[ yyyy/MM/dd HH:mm:ss ] : \n");
                            bodyCont += "now =" + rawdata + "\n";
                            bodyCont += "past =" + pastdata + "\n";
                            saveFile("sameDATA.txt", bodyCont);
                        }
                        pastdata = rawdata;
                        //#define 如果data相同
                    }
                }
                catch (Exception ex) 
                { 
                    Console.WriteLine(ex.ToString());
                    tranXML = "";

                    string bodyCont = DateTime.Now.ToString("[ yyyy/MM/dd HH:mm:ss ] - ") + " <Exception: " + ex.ToString()+">";
                    bodyCont += sBuffer.ToString() + "\n";                    
                    saveFile("errorlog.txt", bodyCont);
                }     
        }
        void com_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            //這是讓程式也能處理其他的訊息，別當住了.
            System.Windows.Forms.Application.DoEvents();
            if (cnt == 0)
            {
                MyDelegate listText = setLog;
                object[] Obj;
                Obj = new object[2] { "", "panel1" };
                panel1.Invoke(listText, Obj);    
            }
            if (!stopFlag)
            {
                // Use either the binary OR the string technique (but not both)         
                // Buffer string data
                sBuffer += com.ReadExisting();
                if (sBuffer.EndsWith("</B>"))
                {
                    string[] sBufferAry = checkXML(sBuffer);
                    if (sBufferAry.Length > 1)
                    {
                        for (int i = 0; i < sBufferAry.Length; i++)
                        {
                            if (!stopFlag)    
                                rxWorking(sBufferAry[i]);
                        }
                    }
                    else
                    {
                        rxWorking(sBuffer);
                    }
                    sBuffer = string.Empty;
                }
            }
        }
        
        private void bubbleRRI(string RRI)
        {
            if (RRI != "")
            {
                string[] dataRRI = RRI.Split(',');
                for (int i = 0; i < dataRRI.Length; i++)
                {
                    if (lastRRI == 0)
                        lastRRI = Convert.ToInt32(dataRRI[i]) * (1000 / 255);
                    else
                    {
                        if (lastRRI >= 150 && Convert.ToInt32(dataRRI[i]) * (1000 / 255) >= 150 && lastRRI <= 1500 && Convert.ToInt32(dataRRI[i]) * (1000 / 255) <= 1500)
                        {
                            
                            RRI1 = lastRRI;
                            RRI2 = Convert.ToInt32(dataRRI[i]) * (1000 / 255) ;
                            addbubble(RRI1, RRI2);
                        }
                        lastRRI = Convert.ToInt32(dataRRI[i]) * (1000 / 255);
                    }
                }
            }
        }
        public void setLog(string msgtext, string fieldname)
        {
            switch (fieldname)
            {

                case "HR":
                    HR_label.Text = msgtext;
                    break;
                case "HQ":
                    HQ_label.Text = msgtext;
                    break;
                case "Y":
                    Y_label.Text = msgtext;
                    break;
                case "F1":
                    F1_label.Text = msgtext;
                    break;
                case "F2":
                    F2_label.Text = msgtext;
                    break;
                case "NAME":
                    name_label.Text = msgtext;
                    break;
                case "tag":
                    tag_label.Text = msgtext;
                    break;
                case "Rpeak":
                    R_label.Text = msgtext;
                    break;
                case "chart2":
                    if (hrtable.Rows.Count > 0)
                    {
                        chart2.Visible = true;
                        chart2.DataSource = hrtable;
                        chart2.Series[0].XValueMember = "x";
                        chart2.Series[0].YValueMembers = "y";                        
                        chart2.DataBind();
                    }
                    break;
                case "chart3":
                    if (rritable.Rows.Count > 0)
                    {
                        chart3.Visible = true;
                        chart3.DataSource = rritable;
                        chart3.Series[0].XValueMember = "x";
                        chart3.Series[0].YValueMembers = "y";
                        chart3.DataBind();
                    }
                    break;
                case "chart4":
                    if (sdnntable.Rows.Count > 0 )
                    {
                        chart4.Visible = true;
                        chart4.DataSource = sdnntable;
                        chart4.Series[0].XValueMember = "x";
                        chart4.Series[0].YValueMembers = "y";
                        chart4.Series[1].XValueMember = "x";
                        chart4.Series[1].YValueMembers = "y2";
                        chart4.Series[2].XValueMember = "x";
                        chart4.Series[2].YValueMembers = "y3";
                        chart4.DataBind();
                        chart4.DataManipulator.Filter(CompareMethod.LessThan, 1, chart4.Series[0], chart4.Series[0]);
                        chart4.DataManipulator.Filter(CompareMethod.LessThan, 1, chart4.Series[1], chart4.Series[1]);
                        chart4.DataManipulator.Filter(CompareMethod.LessThan, 1, chart4.Series[2], chart4.Series[2]);
                    }
                    break;
                case "chart5":
                    if (hrvtable.Rows.Count > 0)
                    {
                        chart5.Visible = true;
                        chart5.DataSource = hrvtable;
                        chart5.Series[0].XValueMember = "x";
                        chart5.Series[0].YValueMembers = "vlf";
                        chart5.Series[1].XValueMember = "x";
                        chart5.Series[1].YValueMembers = "lf";
                        chart5.Series[2].XValueMember = "x";
                        chart5.Series[2].YValueMembers = "hf";
                        chart5.DataBind();
                    }
                    break;
                 
                case "chart6":
                        chart6.Visible = true;
                        chart6.Series[0].Points.Clear();
                        chart6.Series[0].Points.AddXY("VLF",hrvPer[0]);
                        chart6.Series[0].Points.AddXY("LF", hrvPer[1]);
                        chart6.Series[0].Points.AddXY("HF", hrvPer[2]);
                        chart6.Series[0].Points.AddXY("Total", hrvPer[3]);
                    break;
                case "pic3":
                    pictureBox3.Visible = true;
                    pictureBox4.Visible = true;
                    if (BB != null)
                    {
                        pictureBox3.Image = BB;                        
                    }
                    break;
                case "panel2":
                    panel2.Visible = false;
                    break;
                case "panel1":
                    panel1.Visible = false;
                    break;
                case "grp4":
                   // groupBox4.Visible = true;

                    break;
                case "grp5":
                  //  groupBox5.Visible = true;
                    break;
                default:
                    break;
            }
        }
        private void OnFrameChanged(object o, EventArgs e)
        {

            //Force a call to the Paint event handler.
            this.Invalidate();
        }

       private byte[] data2s = new byte[255 * 2];
       private void procData(string data, string HR, string tag, string Rpeak, string HQ, string Y, string F1, string F2)
        {
            hrtable.Rows.Add(getTime(cnt - 1), Convert.ToInt32(HR));
            if (cnt > thrMax)
            {
                hrtable = dtUpdate(hrtable, "HR");
             
            }
            MyDelegate hrText = setLog;
            MyDelegate hqText = setLog;
            MyDelegate yText = setLog;
            MyDelegate f1Text = setLog;
            MyDelegate f2Text = setLog;
            MyDelegate nameText = setLog;
            MyDelegate tagText = setLog;
            MyDelegate RText = setLog;
            MyDelegate chart2Text = setLog;
            MyDelegate chart3Text = setLog;
            object[] hrObj, nameObj, tagObj, chart2Obj, chart3Obj, RObj, HQObj, YObj, F1Obj, F2Obj;
            hrObj         = new object[2] { HR, "HR" };
            nameObj  = new object[2] { modulename, "NAME" };
            tagObj       = new object[2] { tag, "tag" };
            RObj          = new object[2] { Rpeak, "Rpeak" };
            chart2Obj = new object[2] { "", "chart2" };
            chart3Obj = new object[2] { "", "chart3" };
            if(HQ == "1")
                HQObj = new object[2] { "HQ", "HQ" };
            else
                HQObj = new object[2] { "", "HQ" };
            float battery = Convert.ToInt32(Y) / 4096F * 5;
            double f1val = Convert.ToDouble(F1) / 1.6384F;
            YObj = new object[2] { String.Format("{0:0.00}", battery) +"V", "Y" };
            F1Obj = new object[2] { String.Format("{0:0.00}",f1val), "F1" };
            F2Obj = new object[2] { F2, "F2" };
            HR_label.Invoke(hrText, hrObj);
            name_label.Invoke(nameText, nameObj);
            tag_label.Invoke(tagText, tagObj);
            chart2.Invoke(chart2Text, chart2Obj);
            chart3.Invoke(chart3Text, chart3Obj);
            R_label.Invoke(RText,RObj);
            HR_label.Invoke(hqText, HQObj);
            Y_label.Invoke(yText, YObj);
            F1_label.Invoke(f1Text, F1Obj);
            F2_label.Invoke(f2Text, F2Obj);
            byte[] decodestr = Convert.FromBase64String(data);
            if (filtercheck.Checked)
                decodestr = filter60hz(decodestr);
            
            if (decodestr.Length == 255)
            {
                drawLine(decodestr,revcheckBox.Checked,Rpeak);
                lastdecodestr = decodestr;
                saveFile(dumpname, tranXML);
                string decode="";
                for (int i = 0; i < decodestr.Length; i++)
                    decode += decodestr[i].ToString() + ",";
                saveFile(rawfile, decode);
            }
            else
            {
                // 不該發生
                string bodyCont = DateTime.Now.ToString("[ yyyy/MM/dd HH:mm:ss ] -");
                bodyCont += sBuffer.ToString() + "\n";
                bodyCont += "decodestr.Length = " + decodestr.Length.ToString();
                saveFile("errorlog.txt", bodyCont);

            }
        }
        private byte[] lastdecodestr;
        private void drawLine(byte[] ecgAryPaint,bool reverse,string Rpeak)
        {
            if (pastAry != null)
            {
                if (BitConverter.ToString(ecgAryPaint) == BitConverter.ToString(pastAry))
                {
                    string bodyCont = DateTime.Now.ToString("[ yyyy/MM/dd HH:mm:ss ] -");
                    string now = "", past = "";
                    for (int i = 0; i < ecgAryPaint.Length; i++)
                        now += ecgAryPaint[i].ToString() + ",";
                    for (int i = 0; i < pastAry.Length; i++)
                        past += pastAry[i].ToString() + ",";
                    bodyCont += "now =" + now + "\n";
                    bodyCont += "past =" + now + "\n";
                    saveFile("same.txt", bodyCont);
                }
            }
            this.SuspendLayout();
           

            Graphics g = pictureBox1.CreateGraphics();
            Brush mybrush = new SolidBrush(Color.Black) ;          
            // Create pen.
            Pen pen = new Pen(Color.Orange, 1);
            //762 = 255/2 *6
            if (a >= 762) a = a % 762;
            int startA = a;
            if(a==0)
                g.FillRectangle(mybrush, a, 0, 150, pictureBox1.Size.Height);           
            else
                g.FillRectangle(mybrush, a + 1, 0, 150, pictureBox1.Size.Height);//才不會不小心清到上一個點~.~
            if (a / (127 * 5) == 1)
                g.FillRectangle(mybrush, 0, 0, 50, pictureBox1.Size.Height);
       //   string content = DateTime.Now.ToString("[ yyyy/MM/dd HH:mm:ss ] - \n");
            for (int i = 2; i < ecgAryPaint.Length; i = i + 2)         
            {
                if (a >= 762) a = a % 762;
                int y1,y2;
                if(reverse)
                    y1 = pictureBox1.Size.Height - (ecgAryPaint[i - 2] * (pictureBox1.Size.Height-1) / 255);
                else
                    y1 =  (ecgAryPaint[i - 2] * (pictureBox1.Size.Height-1) / 255); //反向
                //避免斷層
                if (a != 0 && i == 2)             
                {
                    if (y1 != lastY)
                        y1 = lastY;
                }
                if(reverse)
                    y2 = pictureBox1.Size.Height - (ecgAryPaint[i] * (pictureBox1.Size.Height-1) / 255);
                else
                    y2 = (ecgAryPaint[i] * (pictureBox1.Size.Height-1) / 255);      //反向

                // Create point that define line to draw.
                g.DrawLine(pen, a, y1, a + 1, y2);

                a = a + 1;
                if (i == ecgAryPaint.Length - 1)
                {
                    lastY = y2;        
                }
              
            }
            
            #region draw R peak
           Pen pen_c = new Pen(Color.Yellow, 1);
           Pen pen_p = new Pen(Color.Purple, 1);
           Pen pen_w = new Pen(Color.White, 1);
            try{
                if (Rpeak != "")
                {
                    string[] RaryS = Rpeak.Split(',');
                    int[] Rary = new int[RaryS.Length];
                    for (int p = 0; p < RaryS.Length; p++)
                    {
                        Rary[p] = Convert.ToInt32(RaryS[p]);
                    }
                    Array.Sort(Rary);
                    for (int p = 0; p < Rary.Length; p++)
                    {
                        int Rpoint;
                        int yPoint;

                      
                        //  if (Convert.ToInt32(Rary[p]) > 151)
                        // {
                        /*
                        if (Convert.ToInt32(Rary[p]) > 190)
                            Rpoint = (Convert.ToInt32(Rary[p]) - 13) / 2; //往回扣13 & draw sample *2
                        else
                            Rpoint = (Convert.ToInt32(Rary[p]) + 65) / 2; //往回扣13 & draw sample *2
                        */
                        int lastY = 0;
                        if (Rary[p] < 0)
                        {
                            Rpoint = (Rary[p] + 255)/2;
                            lastY = Convert.ToInt32( pastAry[Rpoint * 2]);
                        }
                        else
                        {
                            Rpoint = Rary[p] / 2; //draw sample *2
                            lastY = Convert.ToInt32(ecgAryPaint[Rpoint * 2]);
                        }
                        /* 用來過濾抓太近抓不準的bug
                        if (p < Rary.Length - 1)
                        {
                            int RpointL = Rary[p+1];
                            if (Math.Abs(RpointL - (Rpoint*2)) < 85)
                            {
                                continue;
                            }
                        }
                         */
                        // }
                        /*    else
                            {
                                Rpoint = (Convert.ToInt32(Rary[p])+126 ) / 2; //往回扣13 & draw sample *2
                             // continue;
                            }
                          */
                        if (reverse)
                            yPoint = pictureBox1.Size.Height - (lastY * (pictureBox1.Size.Height - 1) / 255) - 2;
                        else
                            yPoint = (lastY * (pictureBox1.Size.Height - 1) / 255) - 2;      //反向
                        //  g.DrawLine(pen, startA, 0, startA, pictureBox1.Size.Height);
                        if (Rary[p] < 0)
                        {
                            if(startA <127)
                                g.DrawEllipse(pen_c, 635 + Rpoint - 2, yPoint, 5, 5); //draw circle
                            else
                                g.DrawEllipse(pen_c, (startA -127)+ Rpoint - 2, yPoint, 5, 5); //draw circle
                        }else
                            g.DrawEllipse(pen_c, startA + Rpoint - 2, yPoint, 5, 5); //draw circle
                     
                        // g.DrawLine(pen_p, startA + Rpoint, 0, startA + Rpoint, pictureBox1.Size.Height);
                        // g.DrawLine(pen_w, startA + (Rpoint * 2), 0, startA + (Rpoint * 2), pictureBox1.Size.Height);
                    }
                }
            }
            catch // (IndexOutOfRangeException ex)
            {

            }
            #endregion
            pictureBox1.Paint += new PaintEventHandler(this.pictureBox1_Paint);
            g.Dispose();
            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch { }
            pastAry = ecgAryPaint;
        }
        private DateTime BaseTime;
        private DateTime getDateTime(int c)
        {
            return BaseTime.AddSeconds(c);
        }        
        private string getTime(int c)
        {
            string time="";
            int h = c / 3600;
            if (h > 0)
            {
                time = h.ToString() + ":";
                c = c - 3600;
            }
            int min = c / 60;
            int sec = c % 60;
            time += min.ToString() + ":" + string.Format("{0:00}", sec);
            return time;
        }
        private string getPortName()
        {
            if(interfaceToolStripMenuItem.DropDownItems.Count<=0)
                return "";

             for (int i = 0; i < interfaceToolStripMenuItem.DropDownItems.Count; i++)
            {
              
               if( ((ToolStripMenuItem)(interfaceToolStripMenuItem.DropDownItems[i])).Checked)
                   return ((ToolStripMenuItem)(interfaceToolStripMenuItem.DropDownItems[i])).Text;
            }
             return "";
        }
     
        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
        }
        
        private void saveFile(string filepath, string filebody)
        {
            filepath = userpath + "\\" + filepath;
            if (!System.IO.File.Exists(filepath))
            {
                // Create a file to write to.
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    try
                    {
                        sw.WriteLine(filebody);
                        sw.Flush();
                        sw.Close();
                    }
                    catch (Exception)
                    {
                        // MessageBox.Show(ex1.Message.ToString());
                    }
                }
            }
            else
            {
                try
                {
                    StreamWriter sw = File.AppendText(filepath);
                    sw.WriteLine(filebody);
                    sw.Flush();
                    sw.Close();
                }
                catch
                {
                    // MessageBox.Show(ex2.Message.ToString());
                }
            }
        }
        private List<int> procRRIdata(string data)
        {
             List<int> intList = new List<int>();
             if (data != "")
             {
                 string[] datastr = data.Split(',');

                 for (int i = 0; i < datastr.Length; i++)
                 {
                     intList.Add(Convert.ToInt32(datastr[i]) * (1000 / 255));
                 }
             }
             return intList;
             
        }
        private List<int> procRRIdataList(List<string> data)
        {
            List<int> intList = new List<int>();
            try
            {
                // for (int i = 0; i < data.Count; i++)
                for (int i = data.Count - 300; i < data.Count; i++)
                {
                    if (data[i] != "" && data[i] != null)
                    {
                        string[] datastr = data[i].Split(',');

                        for (int j = 0; j < datastr.Length; j++)
                        {
                            intList.Add(Convert.ToInt32(datastr[j]) * (1000 / 255));
                        }
                    }
                }

            }
            catch { }
            return intList;
        }
        private List<int> procRRI490dataList(List<string> data)
        {
            List<int> intList = new List<int>();
            try
            {
                // for (int i = 0; i < data.Count; i++)
                for (int i = data.Count - 90; i < data.Count; i++)
                {
                    if (data[i] != "" && data[i] != null)
                    {
                        string[] datastr = data[i].Split(',');

                        for (int j = 0; j < datastr.Length; j++)
                        {
                            intList.Add(Convert.ToInt32(datastr[j]) * (1000 / 255));
                        }
                    }
                }

            }
            catch { }
            return intList;
        }
        private List<int> procRRI4250dataList(DataTable data)
        {
            List<int> intList = new List<int>();
            try
            {
                for (int i = data.Rows.Count - 250; i < data.Rows.Count; i++)
                {
                    if (data.Rows[i]["y"].ToString() != "" && data.Rows[i]["y"] != null )
                    {
                        intList.Add(Convert.ToInt32(data.Rows[i]["y"]) );                     
                    }
                }

            }
            catch { }
            return intList;
        }
       
        private void cleanMem4List()
        {
            List<string> RRIList2 = new List<string>();
            for (int i = RRIList.Count-thrlim; i < RRIList.Count; i++) 
                RRIList2.Add(RRIList[i]);
            RRIList.Clear();
            RRIList = new List<string>();
            RRIList = RRIList2;
        }
        private int hTag = 0;
       
        private void procRRI4SDNN(string newRRI)
        {
            RRIList.Add(newRRI);
            if (cnt >=50)
            {
                if (cnt >= 90)
                {
                    List<int> intList = procRRI490dataList(RRIList);
                    if (intList.Count > 0)
                    {
                        double SDNN = calSDNN(intList);
                        sdnntable.Rows.Add(getDateTime(cnt), SDNN, 0, 0);
                    }
                }
                if (cnt >= 300)
                {
                    List<int> intList2 = procRRIdataList(RRIList);
                    if (intList2.Count > 0)
                    {
                        double SDNN = calSDNN(intList2);
                        sdnntable.Rows.Add(getDateTime(cnt), 0, SDNN, 0);
                    }
                }
                if (rritable.Rows.Count > 50 && rritable.Rows.Count / 50 > hTag)
                {

                    hTag = rritable.Rows.Count / 50;
                    procHRV(rritable, cnt);
                    MyDelegate chart5Text = setLog;
                    object[] Obj5;
                    Obj5 = new object[2] { "", "chart5" };
                    chart5.Invoke(chart5Text, Obj5);
                }

                if (rritable.Rows.Count > 250)
                {
                    List<int> intList3 = procRRI4250dataList(rritable);
                    if (intList3.Count > 0)
                    {
                        double SDNN = calSDNN(intList3);     
                        sdnntable.Rows.Add(getDateTime(cnt), 0, 0, SDNN);
                    }


                }

                if (cnt > thrMax && rritable.Rows.Count > thrMax)
                {
                    sdnntable = SDNNUpdate(sdnntable);
                    rritable = dtUpdate(rritable, "RRI");
                    hTag = rritable.Rows.Count/50; //before:0
                }
                MyDelegate chartText = setLog;
                object[] Obj;
                Obj = new object[2] { "", "chart4" };
                chart4.Invoke(chartText, Obj);

            }
            //避免無止盡的累加
            if (RRIList.Count > thrMax)
                cleanMem4List();
        }
        private byte[] filter60hz(byte[] ecgAryPaint)
        {
            /*output = (t2-2*cos(2*pi*60/255)*t1+t0)*G  G=1/(2-2*cos(2*pi*60/255))
            參考網頁http://www.scienceprog.com/removing-60hz-from-ecg-using-digital-band-stop-filter/
             */
            byte[] filterRes = new byte[255];
            double W0=1.478;
            double G=1/(2-2*Math.Cos(W0));
            //y(t)=x(t)-2*cos(w0)x(t-1)+x(t-2)
            filterRes[0] = ecgAryPaint[0];
            filterRes[1] = ecgAryPaint[0];
            for(int i=2;i<ecgAryPaint.Length;i++){
                filterRes[i] =Convert.ToByte( (ecgAryPaint[i] - 2 * Math.Cos(W0) * ecgAryPaint[i - 1] + ecgAryPaint[i-2]) * G);
            }
            filterRes[1] = filterRes[2];
            filterRes[0] = filterRes[1];
            return filterRes;

        }
  
        private void Form1_Load(object sender, EventArgs e)
        {

        }
    
        private void selftest()
        {
            if (cnt == 0)
            {
                MyDelegate listText = setLog;
                object[] Obj;
                Obj = new object[2] { "", "panel1" };
                panel1.Invoke(listText, Obj);
            }
            string rawdata = "";
            string HR = "", tag = "", Rpeak = "", HQ = "", Y = "", F1 = "", F2 = "";
            tranXML = "";
            try
            {
                tranXML = Testary[k];
                XmlTextReader readingxml = new XmlTextReader(new StringReader(tranXML));
                XmlDocument doc = new XmlDocument();
                doc.Load(readingxml);
                try
                {
                    HR = doc.SelectSingleNode("//B/E/H").InnerText;
                }
                catch 
                { 
                    HR = "0";
                }
                modulename = doc.SelectSingleNode("//B/E/M").InnerText;
                rawdata = doc.SelectSingleNode("//B/E/D").InnerText;
                tag = doc.SelectSingleNode("//B/E/T").InnerText;
                Rpeak = doc.SelectSingleNode("//B/E/P").InnerText;
                HQ = doc.SelectSingleNode("//B/E/S").InnerText;
                Y = doc.SelectSingleNode("//B/E/Y").InnerText;
                F1 = doc.SelectSingleNode("//B/E/F1").InnerText;
                F2 = doc.SelectSingleNode("//B/E/F2").InnerText;
                string samplerate = doc.SelectSingleNode("//B/E/R").InnerText;
                if (HQ == "1")
                {
                    string RRI;
                    try
                    {
                        RRI = doc.SelectSingleNode("//B/E/I").InnerText;
                        if (RRI != "")
                        {
                            string[] dataRRI = RRI.Split(',');
                            for (int i = 0; i < dataRRI.Length; i++)
                            {
                                rritable.Rows.Add(getTime(cnt), Convert.ToInt32(dataRRI[i]) * (1000 / 255));
                                if (dataRRI[i] != "" && dataRRI[i] != "0")
                                {
                                    saveFile(asciidata, string.Format("{0:0.000}", Convert.ToDecimal(dataRRI[i]) / 255));

                                }
                            }
                        }

                    }
                    catch
                    {
                        RRI = "";
                    }
                    bubbleRRI(RRI);
                    procRRI4SDNN(RRI);
                }
                cnt++;

                if (rawdata != "")
                {
                    procData(rawdata,HR,tag,Rpeak,HQ,Y,F1,F2);
                }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                string bodyCont = DateTime.Now.ToString("[ yyyy/MM/dd HH:mm:ss ] - ") + tranXML + " \n <Exception: " + ex.ToString() + ">";
                bodyCont += sBuffer.ToString() + "\n";
                saveFile("Replay_errorlog.txt", bodyCont);
            }
            k++;
            if (k == Testary.Length)
            {
                k = 0;
                timer1.Enabled = false;
                replayECGToolStripMenuItem.Enabled = true;
                replayECGToolStripMenuItem.Text = "Replay ECG";
                pflag = false;
                connectToolStripMenuItem.Enabled = true;
                pictureBox2.Image = ECGDisplay.Properties.Resources.heart1;
               // panel2.Visible = true;
                backgroundWorker1.RunWorkerAsync();                    
              
            }
       }
        
        private void timer1_Tick(object sender, EventArgs e)
        {
            selftest();
        }
        private void testRegax(string file)
        {
            if (file != "")
            {
                string input = System.IO.File.ReadAllText(file);
                string pattern = @"(<B>.*</B>)"; // 規則字串
                Regex regex = new Regex(pattern, RegexOptions.IgnoreCase); // 宣告 Regex 忽略大小寫
                MatchCollection matches = regex.Matches(input); // 將比對後集合傳給 MatchCollection
                int index = 0;
                Testary = new string[matches.Count];
                foreach (Match match in matches) // 一一取出 MatchCollection 內容
                {
                    Testary[index] = match.Value.Trim();
                    index++;
                }
            }
        }
        private string getRecentFile()
        {
            string[] files = Directory.GetFiles(userpath);
            string filename = "";
            DateTime recent = new DateTime(1991, 1, 1);
            for (int i = 0; i < files.Length;i++ )
            {
                if (filename == "")
                {
                    if (  files[i].Contains("dumpXML_")){
                        filename = files[i];
                        recent = File.GetLastAccessTime(filename);
                    }
                }
                else
                {
                    if (files[i].Contains("dumpXML_") && recent < File.GetLastAccessTime(files[i]))
                    {
                        filename = files[i];
                        recent = File.GetLastAccessTime(filename);
                    }
                }

            }
            return filename;
        }
        private void var_init()
        {
            pastdata = "";
            tranXML = "";
            lastY = 0;
            lastRRI = 0;
            RRIList.Clear();
            cnt = 0;
            hTag = 0;
            rritable.Rows.Clear();
            hrtable.Rows.Clear();
            sdnntable.Rows.Clear();
            hrvtable.Rows.Clear();
            k = 0;
            pictureBox3.Visible = false;
            pictureBox4.Visible = false;
          
            chart2.Visible = false;
            chart3.Visible = false;
            chart4.Visible = false; 
            chart5.Visible = false;
            chart6.Visible = false;
            pictureBox2.Visible = true;
            pictureBox2.Image = ECGDisplay.Properties.Resources.heart;
            filtercheck.Visible = true;
            revcheckBox.Visible = true;
            connectToolStripMenuItem.Enabled = false;
            Graphics g = pictureBox1.CreateGraphics();
            Brush mybrush = new SolidBrush(Color.Black);
            g.FillRectangle(mybrush, 0, 0, 787, pictureBox1.Size.Height);
          
            a = 0;
        }
      

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
          //  saveXML4replay();           
        }

        private double calSDNN(List<int> list)
        {
            /*
             SDNN = sqrt(sum of (Ri-MeanRR)^2/(n-1));
             Cal after 5 min 
             */
            int meanRR = calMean(list);
            double sum = 0;
            for (int i = 0; i < list.Count; i++)
            {
                sum +=Math.Pow(Convert.ToInt32(list[i]) - meanRR, 2.0);
            }
           double SDNN = Math.Sqrt(sum / (list.Count - 1));
           return SDNN;
        }
        private int calMean(List<int> list)
        {
            int mean,sum;
            sum = 0;
            for (int i = 0; i < list.Count; i++)
            {
                sum +=Convert.ToInt32( list[i]);
            }
            mean = sum / list.Count;
            return mean;
        }
     
        private void pictureBox3_Paint(object sender, PaintEventArgs e)
        {
        }

        private void createbmp() {
          
            BB = new Bitmap( pictureBox3.Width, pictureBox3.Height);
            gp = Graphics.FromImage(BB);
            gp.DrawImage(BB, 0, 0, 366, 192);

            FontStyle fs = Font.Style;
            FontFamily fm = new FontFamily("Arial");
            Font f = new Font(fm, 8, fs);
          
            Brush mybrush = new SolidBrush(Color.White);
            Brush mybrushb = new SolidBrush(Color.Black);
            Pen pen = new Pen(Color.White, 1);
            Pen bpen = new Pen(Color.Black, 1);
            RectangleF rect = new RectangleF(0.0F, 0.0F, pictureBox3.Width, pictureBox3.Height);

            gp.FillRectangle(mybrushb, rect);
            gp.DrawLine(pen, 100, pictureBox3.Height - 50, 220, pictureBox3.Height - 50); //x asix         
            gp.DrawLine(pen, 100, pictureBox3.Height - 50, 100, pictureBox3.Height - 170); // y axis
            gp.DrawString("RRn (ms)", f, mybrush, 150, pictureBox3.Height - 20);
            gp.DrawString("RRn+1\n (ms)", f, mybrush, 5, pictureBox3.Height - 120);

            gp.DrawString("150", f, mybrush, 90, pictureBox3.Height - 50);
            gp.DrawString("1500", f, mybrush, 210, pictureBox3.Height - 50);
            gp.DrawString("1500", f, mybrush, 70, pictureBox3.Height - 170);
            gp.Save();
        
           // gp.Dispose();
           
        }
        private void Form1_Load_1(object sender, EventArgs e)
        {
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            if (BB != null)
            {
                pictureBox3.Image = BB;
            }
        }
       
        private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {

            addbubble(RRI1, RRI2);
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // Change the value of the ProgressBar to the BackgroundWorker progress.
           // progressBar1.Value = e.ProgressPercentage;
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
          //  label2.Text = "檔案儲存完畢。";
        }

        private void backgroundWorker3_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                com.Close();
            }
            catch (Exception ex)
            {
            //    listBox1.Items.Add("Error closing port:" + ex.Message);
            }
        }
      
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {

        }

        private void com1ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void connectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            panel1.Visible = true;
            if (!conn)
            {
                //connect to comport
                listBox1.Items.Clear();
                if (getPortName() == "")
                    MessageBox.Show("請至'設定'選擇'Interface'才能執行!");
                else
                {
                    connectToolStripMenuItem.Text = "Disconnect";
                    replayECGToolStripMenuItem.Enabled = false;                                    
                  //  panel2.Visible = false;
                    conn = true;
                    stopFlag = false;
                    Graphics g = pictureBox1.CreateGraphics();
                    Brush mybrush = new SolidBrush(Color.Black);
                    g.FillRectangle(mybrush, 0, 0, 787, pictureBox1.Size.Height);
                    a = 0;
                    lastRRI = 0;
                    pictureBox2.Image = ECGDisplay.Properties.Resources.heart;
                    dumpname = "dumpXML_" + DateTime.Now.ToString(" yyyyMMddHHmmss") + ".xml";
                    asciidata = "ascii_rr_data_" + DateTime.Now.ToString(" yyyyMMddHHmmss") + ".txt";
                    rawfile = "rawdata_" + DateTime.Now.ToString(" yyyyMMddHHmmss") + ".txt";
                    if (File.Exists(userpath + "\\" + dumpname))
                        File.Delete(userpath + "\\" + dumpname);
                    if (File.Exists(userpath + "\\" + asciidata))
                        File.Delete(userpath + "\\" + asciidata);
                    if (File.Exists(userpath + "\\" + rawfile))
                        File.Delete(userpath + "\\" + rawfile);
                    main();
                    createbmp();
                }
            }
            else
            {
                //disconnect to comport
                connectToolStripMenuItem.Text = "Connect";
                replayECGToolStripMenuItem.Enabled = true;      
                conn = false;
                stopFlag = true;
                
                listBox1.Items.Clear();
                pictureBox2.Image = ECGDisplay.Properties.Resources.heart1;
                Application.DoEvents();
                backgroundWorker3.RunWorkerAsync();
           
            }
        }

        private void replayECGToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!pflag)
            {
                openFileDialog1.AddExtension = true;
                openFileDialog1.CheckFileExists = false;
                openFileDialog1.DefaultExt = "xml";
                openFileDialog1.Filter = "XML Files(*.xml)|dumpXML_ *.xml";
                openFileDialog1.InitialDirectory = userpath;
                string filename = getRecentFile();
                openFileDialog1.FileName = Path.GetFileName(filename);

                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    string hfile = openFileDialog1.FileName;
                    dumpname = "dumpXML_" + DateTime.Now.ToString(" yyyyMMddHHmmss") + ".xml";
                    asciidata = "ascii_rr_data_" + DateTime.Now.ToString(" yyyyMMddHHmmss") + ".txt";
                    rawfile = "rawdata_" + DateTime.Now.ToString(" yyyyMMddHHmmss") + ".txt";
                    if (File.Exists(userpath + "\\" + dumpname))
                        File.Delete(userpath + "\\" + dumpname);
                    if (File.Exists(userpath + "\\" + asciidata))
                        File.Delete(userpath + "\\" + asciidata);
                    if (File.Exists(userpath + "\\" + rawfile))
                        File.Delete(userpath + "\\" + rawfile);

                    testRegax(hfile);
                    var_init();
                    createbmp();
                    pflag = true;
                    replayECGToolStripMenuItem.Text = "Stop";
                    timer1.Enabled = true;
                }
            }
            else
            {
                pflag = false;
                replayECGToolStripMenuItem.Text = "Replay ECG";
                timer1.Enabled = false;
                connectToolStripMenuItem.Enabled = true;
                pictureBox2.Image = ECGDisplay.Properties.Resources.heart1;
            }
        }
        private void procHRV(DataTable RRItable,int count)
       {
           string lomb = Application.StartupPath + "\\lomb.exe";
           string fftFile =userpath +"\\foo.fft";
           string pwrFile = Application.StartupPath + "\\foo.pwr";
           string nnFile = userpath + "\\foo.nn";
           createNNfile(RRItable);
           if (File.Exists(pwrFile))
               File.Delete(pwrFile);
             //實例一個Process類，啟動一個獨立進程
              Process p = new Process();
  
              //Process類有一個StartInfo屬性，這個是ProcessStartInfo類，包括了一些屬性和方法，下面我們用到了他的幾個屬性：  
              p.StartInfo.FileName = lomb;           //設定程序名
              p.StartInfo.Arguments = " " + fftFile + " " + nnFile;    //設定程式執行參數argv[1]:output_fft;argv[2]:input_nn
             p.StartInfo.UseShellExecute = false;        //關閉Shell的使用
             p.StartInfo.RedirectStandardInput = true;   //重定向標準輸入
             p.StartInfo.RedirectStandardOutput = true;  //重定向標準輸出
             p.StartInfo.RedirectStandardError = true;   //重定向錯誤輸出
             p.StartInfo.CreateNoWindow = true;          //設置不顯示窗口
 
             p.Start();   //啟動
             
             //p.StandardInput.WriteLine(command);       //也可以用這種方式輸入要執行的命令
             //p.StandardInput.WriteLine("exit");        //不過要記得加上Exit要不然下一行程式執行的時候會當機
             
            string  output = p.StandardOutput.ReadToEnd();        //從輸出流取得命令執行結果
             
             try
             {
                 string pwrText = "";
                 string hrvlist = "";
                 using (FileStream fs = new FileStream(pwrFile, FileMode.Open))
                 {
                     using (StreamReader sr = new StreamReader(fs))
                     {
                         int pe = 0;
                      
                         while (sr.Peek() >= 0)
                         {
                             if (pe == 0)
                                 hrvlist = sr.ReadLine();
                            pwrText += sr.ReadLine() + "\n";
                            pe++;
                         }
                     }
                 }
                 if (hrvlist != "")
                 {
                    string[] hrvStr = hrvlist.Split(';');
                    double VLFp=0, LFp=0, HFp = 0;
                    VLFp = ((Convert.ToDouble(hrvStr[1]) + Convert.ToDouble(hrvStr[2])) / Convert.ToDouble(hrvStr[0])) * 100;
                    LFp = Convert.ToDouble(hrvStr[3]) / Convert.ToDouble(hrvStr[0]) * 100;
                    HFp  = Convert.ToDouble(hrvStr[4]) / Convert.ToDouble(hrvStr[0]) * 100;

                    hrvtable.Rows.Add(getDateTime(count), VLFp, LFp, HFp);
                    hrvPer[0] = Math.Round(Convert.ToDouble(hrvStr[1]) + Convert.ToDouble(hrvStr[2]), 5);
                    hrvPer[1] = Math.Round(Convert.ToDouble(hrvStr[3]),5)  ;
                    hrvPer[2] = Math.Round(Convert.ToDouble(hrvStr[4]), 5);
                    hrvPer[3] = Math.Round(Convert.ToDouble(hrvStr[0]), 5);
                 }
                 float nLFP = Convert.ToSingle(hrvPer[1] / (hrvPer[1] + hrvPer[2]) * 100);
                 float nHFP = 100 - nLFP;
                 MyDelegate chart6Text = setLog;
                 object[] Obj6;
                 Obj6 = new object[2] { "", "chart6" };
                 chart6.Invoke(chart6Text, Obj6);
                
              
             }
             catch(Exception e) {
               //  Console.WriteLine(e.Message);
             }
       }
        private void createNNfile( DataTable RRItable)
        {
            string filename = "foo.nn";
            if (File.Exists(userpath+"\\"+ filename))
                File.Delete(userpath + "\\" + filename);
            string bodytext="";
		    float timeser=0;
            for (int i = rritable.Rows.Count - 49; i < rritable.Rows.Count; i++)
		    {
			    timeser += Convert.ToSingle( rritable.Rows[i]["y"])/255; 
			    //fwrite (buffer , 1 , sizeof(buffer) , pFile );
                bodytext += string.Format("{0:0.000}", timeser) + string.Format(" {0:0.000}\n", Convert.ToSingle(rritable.Rows[i]["y"]) / 255);
		    }
            saveFile(filename, bodytext);
	    }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox5_Paint(object sender, PaintEventArgs e)
        {

        }
        private void pictureBox8_Paint(object sender, PaintEventArgs e)
        {

        }

        private void pictureBox7_Click(object sender, EventArgs e)
        {

        }

        private void version251ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void 設定ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GetSerialPort();
        }
        
    }
}
