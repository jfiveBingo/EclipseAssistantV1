using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace EclipseAssistantV1
{
    public partial class MainForm_EA : Form
    {
        public static string compilationMode = "SCC";
        UdpClient Client;
        private string data;
        private string dataIn17;
        string xmlFilePath = "d:\\GIDS\\Config\\ALCOMSAT_1\\ALCOMSAT_1\\CacheData\\60E00AB0.xml";
        private StringBuilder strLogText = new StringBuilder();
        
        System.Media.SoundPlayer player0x11;
        private bool ack = false;
        private string dataIn7;
        private long jStartTime;
        private long jNowTime;
        //private bool flag30minOk = false;
        /// <summary>
        /// was x66 or x77 befor became x11
        /// </summary>
        private bool wasContinues = false;
        private int[] stageTrackingTable = new int[6];
        private bool newDay;
        private long jOldStartTime;
        Color myGreenColor = Color.FromArgb(197, 224, 180);
        private bool _30minBeforEclPassed = false;
        private bool itWasx11 = false;
        //private int lastIndex;
        private bool afterEclipse = false;
        /// <summary>
        /// 9 hours from 16 utc to 01 utc on jtime (next day)
        /// </summary>
        private int jHoursBeforeStart = 32400;
        private bool deltaVerificationStarted = false;
        private double deltaIn7;
        private double deltaIn17;
        private bool itWasx44 = false;
        private bool endOfcharge = false;

        private List<TabPage> hiddenPages = new List<TabPage>();
        private bool noEclipse = false;
        private bool autoScreenShot = false;

        private void EnablePage(TabPage page, bool enable)
        {
            if (enable)
            {
                tabControl1.TabPages.Add(page);
                hiddenPages.Remove(page);
            }
            else
            {
                tabControl1.TabPages.Remove(page);
                hiddenPages.Add(page);
            }
        }

        private void EnablePage(TabControl tab, TabPage page, bool enable)
        {
            if (enable)
            {
                tab.TabPages.Add(page);
                hiddenPages.Remove(page);
            }
            else
            {
                tab.TabPages.Remove(page);
                hiddenPages.Add(page);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            foreach (var page in hiddenPages) page.Dispose();
            base.OnFormClosed(e);
        }

        public MainForm_EA()
        {
            InitializeComponent();
            EnablePage(tabPageTest, false);
            EnablePage(tabPageInfo, false);
            EnablePage(tabControl2, tpgEvent, false);
        }

        private void WriteLog(string v)
        {
            //witre log in textBox

            strLogText.AppendLine(v);
            string stringLog = strLogText.ToString();
            txbLog.Text = stringLog.Remove(stringLog.Length - 2, 2);
        }

        private string MyDate()
        {
            return DateTime.Now.ToShortDateString().Replace("/", "-") + " " + DateTime.Now.ToLongTimeString();
        }

        //public static long CalculJTimeSlash(string inStr)
        //{
        //    string[] yt = inStr.Split(' ');
        //    if (yt.Length != 2)
        //        return -1;
        //    long jd = 0, js = 0;
        //    try
        //    {
        //        string[] YMD = yt[0].Split('/');
        //        jd = YMDtoJD(System.Convert.ToInt32(YMD[2]), System.Convert.ToInt32(YMD[1]), System.Convert.ToInt32(YMD[0]));

        //        string[] HMS = yt[1].Split(':');
        //        js = (int)HMStoJS(System.Convert.ToInt32(HMS[0]), System.Convert.ToInt32(HMS[1]), System.Convert.ToInt32(HMS[2]));
        //    }
        //    catch
        //    {
        //        return -1;
        //    }

        //    return (jd * 86400) + js;
        //}

        public static long CalculJTimeSlash(DateTime dateTime)
        {
            long jd = YMDtoJD(dateTime.Year,dateTime.Month,dateTime.Day);

            long js = (long)Math.Round(HMStoJS(dateTime.Hour,dateTime.Minute,dateTime.Second));

            return (jd * 86400) + js;
        }

        public static long CalculJTime(string inStr)
        {
            string[] yt = inStr.Split(' ');
            if (yt.Length != 2)
                return -1;
            long jd = 0, js = 0;
            try
            {
                string[] YMD = yt[0].Split('-');
                jd = YMDtoJD(System.Convert.ToInt32(YMD[0]), System.Convert.ToInt32(YMD[1]), System.Convert.ToInt32(YMD[2]));

                string[] HMS = yt[1].Split(':');
                js = (int)HMStoJS(System.Convert.ToInt32(HMS[0]), System.Convert.ToInt32(HMS[1]), System.Convert.ToInt32(HMS[2]));
            }
            catch
            {
                return -1;
            }

            return (jd * 86400) + js;
        }

        /// <summary>
        /// 年月日转换成积日
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        /// <returns></returns>
        public static int YMDtoJD(int year, int month, int day)
        {
            int JD;

            if (month < 3)
            {
                month = month + 12;
                year = year - 1;
            }

            JD = (int)((year - 1900) * 365.25) + (int)((month * 153.0 - 1.5) / 5.0) + day - 18294;

            return JD;
        }

        /// <summary>
        /// 时分秒转换成积秒
        /// </summary>
        /// <returns></returns>
        public static double HMStoJS(int hour, int minute, double second)
        {
            return hour * 3600 + minute * 60 + second;
        }

        public static string InverseCalculJTime(long input)
        {
            long secondsInDay = 86400;
            long jd = input / secondsInDay;
            long js = input % secondsInDay;

            int[] iYMD = new int[3];
            JDtoYMD(jd, iYMD);

            int[] iHMS = new int[4];
            JStoHMS(js, iHMS);

            string reStr = iYMD[0].ToString() + "-" + iYMD[1].ToString() + "-" + iYMD[2].ToString() + " ";
            reStr = reStr + iHMS[0].ToString() + ":" + iHMS[1].ToString();

            return reStr;
        }

        /// <summary>
        /// 积日转换成年月日
        /// </summary>
        /// <param name="jd"></param>
        /// <param name="YMD"></param>
        public static void JDtoYMD(long jd, int[] YMD)
        {
            int IAF, IBXH, J, IBB, JXH, ICXH, IB, IC, ID;

            IAF = Convert.ToInt32(jd + 18294);
            IBXH = (int)((IAF * 4 - 365) / 1461);
            J = IAF - (int)(IBXH * 365.25);
            if (J >= 92)
            {
                IBB = IBXH;
                JXH = J;
            }
            else
            {
                IBB = IBXH - 1;
                JXH = J + 365;
            }
            ICXH = (int)((JXH * 5 + 1.5) / 153);
            ID = JXH - (int)((ICXH * 153 - 1.5) / 5);
            if (ICXH >= 13)
            {
                IC = ICXH - 12;
                IB = IBB + 1;
            }
            else
            {
                IC = ICXH;
                IB = IBB;
            }
            IB += 1900;
            YMD[0] = IB;
            YMD[1] = IC;
            YMD[2] = ID;
        }

        /// <summary>
        /// 积秒到时分秒
        /// </summary>
        /// <param name="js"></param>
        /// <param name="HMS"></param>
        public static void JStoHMS(long js, int[] HMS)
        {
            int JS0;

            JS0 = (int)(js);
            HMS[0] = (int)(js / 3600.0);
            HMS[1] = (int)((js - HMS[0] * 3600.0) / 60.0);
            HMS[2] = (int)(js - HMS[0] * 3600.0 - HMS[1] * 60.0);
            HMS[3] = (int)((js - JS0) * 1000);
        }

        public static string InverseCalculJTime2(long input)
        {
            long secondsInDay = 86400;
            long jd = input / secondsInDay;
            long js = input % secondsInDay;

            // Convert JD to YMD
            int year, month, day;
            JDToYMD((int)jd, out year, out month, out day);

            // Convert JS to HMS
            int hours, minutes;
            double seconds;
            JSToHMS(js, out hours, out minutes, out seconds);

            return $"{year:D4}-{month:D2}-{day:D2} {hours:D2}:{minutes:D2}:{seconds:F0}";
        }

        //public static void JDToYMD2(int JD, out int year, out int month, out int day)
        //{
        //    string julianDate = "13324";

        //    int jDate = Convert.ToInt32(julianDate);
        //    day = jDate % 1000;
        //    year = (jDate - day) / 1000;
        //    var date1 = new DateTime(year, 1, 1);
        //    var result = date1.AddDays(day - 1);
        //}

        // Convert Julian Day to Year, Month, Day
        //TODO regler probleme de la date
        public static void JDToYMD(int JD, out int year, out int month, out int day)
        {
            JD += 18294;  // Revenir à l'original en ajoutant 18294

            int a = (int)((JD + 0.5) / 365.25);
            int b = JD - (int)(365.25 * a);
            int c = (int)((b + 0.5) / 30.6001);

            day = b - (int)(30.6001 * c);
            //month = c - 1;
            month = c;

            switch (c)
            {
                case 1:
                    day = day - 1;
                    break;

                case 2:
                    day = day - 1;
                    break;

                case 5:
                    day = day + 1;
                    break;

                case 7:
                    day = day + 1;
                    break;

                case 10:
                    day = day + 1;
                    break;
                //case 11:
                //    day = day - 1;
                //    break;
                case 12:
                    day = day + 1;
                    break;


                default:
                    break;
            }

            //if (c > 4)
            //{
            //    day = day + 1;
            //}
            //else
            //{
            //    if (c < 4)
            //    {
            //        day = day - 1;
            //    }

            //}

            if (month > 12) month -= 12;
            year = a + 1900;

            if (month > 12)
            {
                month -= 12;
                year += 1;
            }
        }

        // Convert Julian Seconds to Hours, Minutes, Seconds
        public static void JSToHMS(double js, out int hours, out int minutes, out double seconds)
        {
            hours = (int)(js / 3600);
            minutes = (int)((js % 3600) / 60);
            seconds = js % 60;
        }



        public void UpdatejOldStartTime()
        {
            jNowTime = CalculJTimeSlash(DateTime.Now);
            XmlDocument doc = new XmlDocument();

            try
            {
                doc.Load(xmlFilePath);
            }
            catch (Exception docExeption)
            {
                WriteLog(MyDate() + " : Error XML : " + docExeption.Message);
            }

            XmlNode node = doc.DocumentElement.SelectSingleNode("/CCR/YB");
            StringBuilder stringBuilder = new StringBuilder();

            if (node != null)
            {
                int i = 1;
                foreach (XmlNode childNode in node.ChildNodes)
                {
                    string nodeTime = childNode.ChildNodes[1].ChildNodes[0].InnerText;
                    
                    nodeTime = nodeTime.Remove(nodeTime.Length - 4);
                    long jNodeTime = CalculJTime(nodeTime);
                    
                    //Recherche de la derniere date of start eclipse: on change la date apres 18 utc (on passe a la date de demain)
                    i++;
                    if (jNowTime - jNodeTime <= -jHoursBeforeStart)
                    {
                        stringBuilder.AppendLine("[" + i + "] : " + jNowTime + " - " + jNodeTime + " = " + (jNowTime - jNodeTime));
                        break;
                    }
                    jStartTime = CalculJTime(childNode.ChildNodes[1].ChildNodes[0].InnerText.Remove(childNode.ChildNodes[1].ChildNodes[0].InnerText.Length - 5));
                    lblDateEclipse.Text = childNode.ChildNodes[1].ChildNodes[1].InnerText;
                    lblStartEclipseTime.Text = childNode.ChildNodes[1].ChildNodes[2].InnerText.Remove(childNode.ChildNodes[1].ChildNodes[2].InnerText.Length - 5);
                    lblEndEclipseTime.Text = childNode.ChildNodes[4].ChildNodes[2].InnerText.Remove(childNode.ChildNodes[4].ChildNodes[2].InnerText.Length - 5);
                    stringBuilder.AppendLine("[" + i + "] : " + nodeTime + " :: " + (jNowTime - jNodeTime));
                }
            }

            jOldStartTime = jStartTime;
        }

        private void MainForm_EA_Load(object sender, EventArgs e)
        {
            //***************************AppSetting******************
            string xmlFilePath = ConfigurationManager.AppSettings["xml" + compilationMode];
            player0x11 = new System.Media.SoundPlayer(@ConfigurationManager.AppSettings["sound" + compilationMode]);
            //*******************************************************




            newDay = false;
            stageTrackingTable = new int[] { 0, 0, 0, 0, 0, 0 };


            WriteLog(MyDate() + " : Application started !");
            jNowTime = CalculJTimeSlash(DateTime.Now);
            XmlDocument doc = new XmlDocument();
                        
            try
            {
                doc.Load(xmlFilePath);
            }
            catch (Exception docExeption)
            {
                WriteLog(MyDate() + " : Error XML : " + docExeption.Message);
            }

            XmlNode node = null;

            if (doc.DocumentElement != null)
            {
                node = doc.DocumentElement.SelectSingleNode("/CCR/YB");
            }
            else
            {
                MessageBox.Show("You must open the Eclipse Prediction page in GIDS to load the XML file!",
                "Attention",
                MessageBoxButtons.OK,
                MessageBoxIcon.Exclamation);

                this.Close();
            }

            StringBuilder stringBuilder = new StringBuilder();

            if (node != null)
            {
                int i = 1;
                foreach (XmlNode childNode in node.ChildNodes)
                {
                    string nodeTime = childNode.ChildNodes[1].ChildNodes[0].InnerText;
                    nodeTime = nodeTime.Remove(nodeTime.Length - 4);
                    long jNodeTime = CalculJTime(nodeTime);

                    //Recherche de la derniere date of start eclipse: on change la date apres 18 utc (on passe a la date de demain)
                    i++;
                    if (jNowTime - jNodeTime <= -jHoursBeforeStart)
                    {
                        stringBuilder.AppendLine("[" + i + "] : " + jNowTime + " - " + jNodeTime + " = " + (jNowTime - jNodeTime));
                        if (lblStartEclipseTime.Text == "88:88")
                        {
                            jStartTime = CalculJTime(childNode.ChildNodes[1].ChildNodes[0].InnerText.Remove(childNode.ChildNodes[1].ChildNodes[0].InnerText.Length - 5));
                            lblDateEclipse.Text = childNode.ChildNodes[1].ChildNodes[1].InnerText;
                            lblStartEclipseTime.Text = childNode.ChildNodes[1].ChildNodes[2].InnerText.Remove(childNode.ChildNodes[1].ChildNodes[2].InnerText.Length - 5);
                            lblEndEclipseTime.Text = childNode.ChildNodes[4].ChildNodes[2].InnerText.Remove(childNode.ChildNodes[4].ChildNodes[2].InnerText.Length - 5);
                            stringBuilder.AppendLine("[" + i + "] : " + nodeTime + " :: " + (jNowTime - jNodeTime));
                        }
                        break;
                    }
                    jStartTime = CalculJTime(childNode.ChildNodes[1].ChildNodes[0].InnerText.Remove(childNode.ChildNodes[1].ChildNodes[0].InnerText.Length - 5));
                    lblDateEclipse.Text = childNode.ChildNodes[1].ChildNodes[1].InnerText;
                    lblStartEclipseTime.Text = childNode.ChildNodes[1].ChildNodes[2].InnerText.Remove(childNode.ChildNodes[1].ChildNodes[2].InnerText.Length - 5);
                    lblEndEclipseTime.Text = childNode.ChildNodes[4].ChildNodes[2].InnerText.Remove(childNode.ChildNodes[4].ChildNodes[2].InnerText.Length - 5);
                    stringBuilder.AppendLine("[" + i + "] : " + nodeTime + " :: " + (jNowTime - jNodeTime));
                }
            }

            //jTimeBeforStart time left before eclipse start time !
            long jTimeBeforStart = jStartTime - jNowTime;

            if (jTimeBeforStart <= jHoursBeforeStart && jTimeBeforStart > 30*60)
            {
                //activer le paneaux 1
                pnl1.BackColor = myGreenColor;
                // if time < 30 min Stageclosed!
            }
            else
            {
                if (jTimeBeforStart < 0 && endOfcharge)
                {
                    afterEclipse = true;
                    WriteLog(MyDate() + " : New start time updated !");
                    WriteLog(MyDate() + " : Calculation for new day started!");

                    UpdatejOldStartTime();
                    timerNewDay.Start();
                    
                }
                else
                {
                    //Max -24 h 60*60*24 (-86 400) plus que 100 000
                    if (jTimeBeforStart < 30 * 60 && jTimeBeforStart > - 80000)
                    {
                        _30minBeforEclPassed = true;
                    }
                    else
                    {
                        //No eclipse
                        //Activer l'onglet TM
                        EnablePage(tabPageInfo, true);
                        tabControl1.SelectedTab = tabPageInfo;

                        //Afficher Next or Last Date!

                        if ((jStartTime - jNowTime) > 0)
                        {
                            //Next date
                            lblTextDate.Text = "Next Start Eclipse : ";
                        }
                        else
                        {
                            //Last date
                            lblTextDate.Text = "Last Start Eclipse : ";
                        }


                        //afficher la date !
                        lblRestDateEclipse.Text = InverseCalculJTime(jStartTime);
                        noEclipse = true;
                    }
                }
            }

            jOldStartTime = jStartTime;
            txbEvent.Text = stringBuilder.ToString();
            BeginRecieve();
            
        }

        private string LittleEndian(string num)
        {
            string retval = "";
            if (num != "N/A")
            {
                Int64 number = Convert.ToInt64(num, 16);
                byte[] bytes = BitConverter.GetBytes(number);
                foreach (byte b in bytes)
                    retval += b.ToString("X2");
            }
            
            return retval;
        }

        private void BeginRecieve()
        {
            try
            {
                Client = new UdpClient(2246);
                Client.JoinMulticastGroup(IPAddress.Parse("239.0.0.5"));
                Client.BeginReceive(new AsyncCallback(recv), null);
                WriteLog(MyDate() + " : TM recieved !");
            }
            catch (Exception ex)
            {
                lblZS82.Text = ex.Message.ToString();
                WriteLog(MyDate() + " : Port 2246 already used ");
            }
        }

        void recv(IAsyncResult res)
        {
            IPEndPoint RemoteIP = new IPEndPoint(IPAddress.Any, 2246);


            try
            {
                byte[] received = Client.EndReceive(res, ref RemoteIP);
                //Fonction de trouver la valeur zs82 puis la retourner en valeur string
                data = GetTmValue_ZS82(received);
                dataIn17 = GetTmValue_In17(received);
                dataIn7 = GetTmValue_In7(received);
            }
            catch  (ArgumentException axp)
            {
                MessageBox.Show("Erreur REC00 : " + axp.ToString());
            }
            catch (Exception exp)
            {
                MessageBox.Show("Erreur REC01 : " + exp.ToString());
            }
            //To avoid cross threading we use methode invoker
            this.Invoke(new MethodInvoker(delegate
            {
                double in17 = ConvertHexToDouble(dataIn17);
                double in7 = ConvertHexToDouble(dataIn7);

                if (in7 >= 0)
                {
                    AfficherIn7(ConvertHexToDouble(dataIn7));
                    
                }
                if (in17 >= 0)
                {
                    AfficherIn17(ConvertHexToDouble(dataIn17));
                }

                if (data != "N/A")
                {
                    AfficherZS82(data);
                    switch (data)
                    {
                        case "0x22":

                            //afficher "pas d'eclipse"
                            //date debut eclipse !!

                            if (!noEclipse)
                            {
                                //Activer l'onglet TM
                                EnablePage(tabPageInfo, true);
                                tabControl1.SelectedTab = tabPageInfo;

                                //Afficher Next or Last Date!

                                if ((jStartTime-jNowTime) > 0)
                                {
                                    //Next date
                                    lblTextDate.Text = "Next Start Eclipse : ";
                                }
                                else
                                {
                                    //Last date
                                    lblTextDate.Text = "Last Start Eclipse : ";
                                }


                                //afficher la date !
                                lblRestDateEclipse.Text = InverseCalculJTime(jStartTime);
                                noEclipse = true;
                            }

                            break;

                        case "0x11":
                            itWasx11 = true;

                            if (afterEclipse)
                            {
                                //green table
                                //stages closed
                                pnl1.BackColor = myGreenColor;
                                pnl2.BackColor = myGreenColor;
                                pnl3.BackColor = myGreenColor;
                                pnl4.BackColor = myGreenColor;
                                pnl5.BackColor = myGreenColor;

                                //stages closed 1
                                lbl30MinBefor.Text = "        Stage Closed, No Data " + Environment.NewLine + " ";
                                lbl30MinBefor.Visible = true;
                                lbl30MinBefor.ForeColor = Color.Black;

                                //stage closed 2
                                lblEclipseEntryTimeKO.Visible = true;

                                //stage closed 3
                                lblOutOfEclipseKO.Visible = true;

                                //stage closed 4
                                lblStage4_x66_Ko.Visible = true;

                                //stage closed 5
                                lblEndOfChargeMssg.Text = "        Stage Closed, No Data " + Environment.NewLine + " ";
                                lblEndOfChargeMssg.Visible = true;
                                lblEndOfChargeMssg.ForeColor = Color.Black;


                            }
                            else
                            {
                                if (_30minBeforEclPassed)
                                {
                                    lbl30MinBefor.Text = "        Stage Closed, No Data " + Environment.NewLine + " ";
                                    lbl30MinBefor.Visible = true;
                                    lbl30MinBefor.ForeColor = Color.Black;
                                    pnl1.BackColor = myGreenColor;
                                    _30minBeforEclPassed = false;
                                    WriteLog(MyDate() + " :  Error : Application not open in this stage [30 min befor Eclipse] - ");
                                    WriteLog(MyDate() + " :  No data available!");

                                }
                                else
                                {
                                    if (!ack && wasContinues)
                                    {
                                        pnl5.BackColor = myGreenColor;
                                        btnAck.Visible = true;
                                        lblEndOfChargeMssg.Visible = true;
                                        wasContinues = false;
                                        lblEndOfChargeTime.Text = DateTime.Now.ToString("dd/MM/yyyy") + " " + DateTime.Now.ToString("HH:mm:ss");
                                        WriteLog(MyDate() + " : End of charge");
                                        

                                        if (!cbxMutePnl5.Checked)
                                        {
                                            Play0x11AlarmSound();
                                        }

                                        //END (end of eclipse)
                                        endOfcharge = true;
                                        deltaVerificationStarted = false;
                                        itWasx44 = false;
                                        timerNewDay.Start();
                                        UpdatejOldStartTime();

                                        WriteLog(MyDate() + " : New start time updated !");
                                        WriteLog(MyDate() + " : Calculation for new day started !");

                                        //Take screen shot
                                        if (autoScreenShot)
                                        {
                                            CaptureAndSaveScreenshot();
                                        }


                                    }
                                    else
                                    {
                                        if (newDay)
                                        {
                                            //START
                                            newDay = false;
                                            endOfcharge = false;
                                            //remettre tout a zero
                                            //tableau de stage
                                            stageTrackingTable = new int[] { 0, 0, 0, 0, 0, 0 };
                                            //couleur des stages
                                            ResetPanelsColor();
                                        }
                                    }
                                }
                            }


                            break;

                        case "0x33":
                            ack = false;
                            //part1 notblue ..GradientActiveCaption;
                            pnl1.BackColor = myGreenColor;
                            pnl2.BackColor = myGreenColor;
                            //pnl3.BackColor = myGreenColor;

                            // test si l'application a suivi l'etape 2 //dans le tableu de Suivi des etapes
                            //Inaccessible Stage // by using Stage Tracking Table
                            if (stageTrackingTable[0] == 0)
                            {
                                if (!lbl30MinBefor.Visible)
                                {
                                    WriteLog(MyDate() + " :  Error : Application not open in this stage [30 min befor Eclipse] - ");
                                    WriteLog(MyDate() + " :  No data available!");
                                    lbl30MinBefor.Text = "        Stage Closed, No Data " + Environment.NewLine + " ";
                                    lbl30MinBefor.ForeColor = Color.Black;
                                    lbl30MinBefor.Visible = true;
                                }
                            }
                            if (stageTrackingTable[1] == 0 && itWasx11)
                            {
                                //passage to x33 pour la premiere fois
                                lblEclipseEntryTime.Text = DateTime.Now.TimeOfDay.ToString();
                                stageTrackingTable[1] = 1;
                                itWasx11 = false;
                            }
                            else
                            {
                                if (stageTrackingTable[1] == 0)
                                {
                                    if (!lblEclipseEntryTimeKO.Visible)
                                    {
                                        WriteLog(MyDate() + " : Error : Application not open in this stage [Eclipse Entry Time] ");
                                        WriteLog(MyDate() + " : No data available!");
                                    }
                                    lblEclipseEntryTimeKO.Visible = true;
                                }
                            }
                            break;
                        case "0x44":
                            pnl3.BackColor = myGreenColor;

                            if (stageTrackingTable[0] == 1)
                            {
                                //u can use delta verification
                                if (!itWasx44)
                                {
                                    WriteLog(MyDate() + " : 0x44 : Out of Eclipse!");

                                    lblOutOfEclipseTime.Text = DateTime.Now.TimeOfDay.ToString();
                                    itWasx44 = true;
                                }
                                //Start deltaIn7In17 condition verifications
                                StartDeltaVerification();

                            }
                            else
                            {
                                if (stageTrackingTable[2] == 0)
                                {
                                    //stage closed 3
                                    if (!lblOutOfEclipseKO.Visible)
                                    {
                                        WriteLog(MyDate() + " : Error : Application not open in this stage [Out of eclipse] ");
                                        WriteLog(MyDate() + " : No data available!");

                                        pnl3.BackColor = myGreenColor;
                                        lblOutOfEclipseKO.Visible = true;
                                    }
                                    
                                }
                                if (stageTrackingTable[1] == 0)
                                {
                                    //stage closed 2
                                    if (!lblEclipseEntryTimeKO.Visible)
                                    {
                                        pnl2.BackColor = myGreenColor;
                                        lblEclipseEntryTimeKO.Visible = true;

                                        WriteLog(MyDate() + " : Error : Application not open in this stage [Eclipse entry time] ");
                                        WriteLog(MyDate() + " : No data available!");
                                    }

                                    
                                }
                                if (stageTrackingTable[0] == 0)
                                {
                                    //stages closed 1
                                    if (!lbl30MinBefor.Visible)
                                    {
                                        pnl1.BackColor = myGreenColor;
                                        lbl30MinBefor.Text = "        Stage Closed, No Data " + Environment.NewLine + " ";
                                        lbl30MinBefor.Visible = true;
                                        lbl30MinBefor.ForeColor = Color.Black;

                                        WriteLog(MyDate() + " : Error : Application not open in this stage [30 min before eclipse] ");
                                        WriteLog(MyDate() + " : No data available!");
                                    }

                                }
                            }

                            if (!itWasx44)
                            {
                                itWasx44 = true;
                            }

                            break;
                        case "0x66":
                            pnl4.BackColor = myGreenColor;
                            wasContinues = true;
                            afterEclipse = false;
                            //Set time of lbl
                            //if lblA = 0xA so this is first
                            //else its second
                            if (lbl0xA.Text == "0xA" && itWasx44)
                            {
                                lbl0xA.Text = "0x66";
                                lblContinuesChargeATime.Text = DateTime.Now.ToString("dd/MM/yyyy") + " " + DateTime.Now.ToString("HH:mm:ss");
                            }
                            else
                            {
                                if (lbl0xB.Text == "0xB" && lbl0xA.Text != "0x66" && itWasx44)
                                {
                                    lbl0xB.Text = "0x66";
                                    lblContinuesChargeBTime.Text = DateTime.Now.ToString("dd/MM/yyyy") + " " + DateTime.Now.ToString("HH:mm:ss");
                                }
                                else
                                {
                                    if (!itWasx44)
                                    {
                                        //stage 4 closed
                                        if (!lblStage4_x66_Ko.Visible)
                                        {
                                            lblStage4_x66_Ko.Visible = true;

                                            WriteLog(MyDate() + " : Error : Application not open in this stage [Start continues charge] ");
                                            WriteLog(MyDate() + " : No data available!");
                                        }

                                        

                                        if (stageTrackingTable[2] == 0)
                                        {
                                            //stage closed 3
                                            if (!lblOutOfEclipseKO.Visible)
                                            {
                                                pnl3.BackColor = myGreenColor;
                                                lblOutOfEclipseKO.Visible = true;

                                                WriteLog(MyDate() + " : Error : Application not open in this stage [Out of eclipse] ");
                                                WriteLog(MyDate() + " : No data available!");
                                            }
                                            
                                        }
                                        if (stageTrackingTable[1] == 0)
                                        {
                                            //stage closed 2
                                            if (!lblEclipseEntryTimeKO.Visible)
                                            {
                                                pnl2.BackColor = myGreenColor;
                                                lblEclipseEntryTimeKO.Visible = true;

                                                WriteLog(MyDate() + " : Error : Application not open in this stage [Eclipse entry time] ");
                                                WriteLog(MyDate() + " : No data available!");
                                            }
                                            
                                        }
                                        if (stageTrackingTable[0] == 0)
                                        {
                                            //stages closed 1
                                            if (!lbl30MinBefor.Visible)
                                            {
                                                pnl1.BackColor = myGreenColor;
                                                lbl30MinBefor.Text = "        Stage Closed, No Data " + Environment.NewLine + " ";
                                                lbl30MinBefor.Visible = true;
                                                lbl30MinBefor.ForeColor = Color.Black;

                                                WriteLog(MyDate() + " : Error : Application not open in this stage [30 min before eclipse] ");
                                                WriteLog(MyDate() + " : No data available!");
                                            }
                                            
                                        }
                                    }
                                }
                            }
                            break;
                        case "0x77":
                            pnl4.BackColor = myGreenColor;
                            wasContinues = true;
                            afterEclipse = false;
                            //if lblA = 0xA so this is first
                            //else its second
                            if (lbl0xA.Text == "0xA" && itWasx44)
                            {
                                lbl0xA.Text = "0x77";
                                lblContinuesChargeATime.Text = DateTime.Now.ToString("dd/MM/yyyy") + " " + DateTime.Now.ToString("HH:mm:ss");
                            }
                            else
                            {
                                if (lbl0xB.Text == "0xB" && lbl0xA.Text != "0x77" && itWasx44)
                                {
                                    lbl0xB.Text = "0x77";
                                    lblContinuesChargeBTime.Text = DateTime.Now.ToString("dd/MM/yyyy") + " " + DateTime.Now.ToString("HH:mm:ss");
                                }
                                else
                                {
                                    if (!itWasx44)
                                    {
                                        //stage 4 closed
                                        if (!lblStage4_x66_Ko.Visible)
                                        {
                                            lblStage4_x66_Ko.Visible = true;

                                            WriteLog(MyDate() + " : Error : Application not open in this stage [Start continues charge] ");
                                            WriteLog(MyDate() + " : No data available!");
                                        }
                                        

                                        if (stageTrackingTable[2] == 0)
                                        {
                                            //stage closed 3
                                            if (!lblOutOfEclipseKO.Visible)
                                            {
                                                pnl3.BackColor = myGreenColor;
                                                lblOutOfEclipseKO.Visible = true;

                                                WriteLog(MyDate() + " : Error : Application not open in this stage [Out of Eclipse] ");
                                                WriteLog(MyDate() + " : No data available!");
                                            }
                                            
                                        }
                                        if (stageTrackingTable[1] == 0)
                                        {
                                            //stage closed 2
                                            if (!lblEclipseEntryTimeKO.Visible)
                                            {
                                                pnl2.BackColor = myGreenColor;
                                                lblEclipseEntryTimeKO.Visible = true;

                                                WriteLog(MyDate() + " : Error : Application not open in this stage [Eclipse entry time] ");
                                                WriteLog(MyDate() + " : No data available!");
                                            }
                                            
                                        }
                                        if (stageTrackingTable[0] == 0)
                                        {
                                            //stages closed 1
                                            if (!lbl30MinBefor.Visible)
                                            {
                                                pnl1.BackColor = myGreenColor;
                                                lbl30MinBefor.Text = "        Stage Closed, No Data " + Environment.NewLine + " ";
                                                lbl30MinBefor.Visible = true;
                                                lbl30MinBefor.ForeColor = Color.Black;

                                                WriteLog(MyDate() + " : Error : Application not open in this stage [30 min before eclipse] ");
                                                WriteLog(MyDate() + " : No data available!");
                                            }
                                            
                                        }
                                    }
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }

                

               

                //WriteLog(MyDate() + " timerRecord.Interval : " + timerRecord.Interval);


            }));




            try
            {
                Client.BeginReceive(new AsyncCallback(recv), null);
            }
            catch (Exception expCLBC)
            {
                MessageBox.Show("Erreur : " + expCLBC.Message.ToString());
            }
        }

        private void StartDeltaVerification()
        {
            if (!deltaVerificationStarted)
            {
                //start timer verification
                deltaVerificationStarted = true;
                timerDeltaIn7in17.Enabled = true;

                WriteLog(MyDate() + " : delta verification started!");

            }
        }

        //private void AfficherTk1(double doubleVal)
        //{
        //    try
        //    {
        //        lblTK1.Text = doubleVal.ToString();
        //    }
        //    catch (Exception e)
        //    {

        //        lblTK1.Text = e.Message;
        //    }

        //}

        private void ResetPanelsColor()
        {
            //pnl1.BackColor = myGreenColor;
            pnl1.BackColor = myGreenColor;
            pnl2.BackColor = this.BackColor;
            pnl3.BackColor = this.BackColor;
            pnl4.BackColor = this.BackColor;
            pnl5.BackColor = this.BackColor;

            //reset les btn Ack
            btnAck30min.Visible = false;
            btnAck.Visible = false;
            btnAckDelta.Visible = false;
            //stop sound
            player0x11.Stop();

            //reset labels d'alarm
            //stage 1
            lbl30MinBefor.Text = "       Time left for screen shot" + Environment.NewLine + " ";
            lbl30MinBefor.ForeColor = Color.Black;
            lbl30MinBefor.Visible = false;
            //stage 2
            lblEclipseEntryTimeKO.Visible = false;
            //stage 3
            lblOutOfEclipseKO.Visible = false;
            //stage 4
            lblStage4_x66_Ko.Visible = false;
            //stage 5
            lblEndOfChargeMssg.Text = "       Please recorde TM" + Environment.NewLine +
                                         " " + Environment.NewLine;
            lblEndOfChargeMssg.Visible = false;
            lblEndOfChargeMssg.ForeColor = Color.Black;

            //reset  label Delta
            lblDeltaOK.Visible = false;
        }

        private double ConvertHexToDouble(string hexData)
        {
            try
            {
                if (hexData != null && hexData != string.Empty)
                {
                    hexData = hexData.Replace("-", "");

                    //var hex = "0x404866E4E26d4802";
                    hexData = hexData.Replace("0x", "");

                    hexData = "0x" + LittleEndian(hexData);

                    var int64Val = Convert.ToInt64(hexData, 16);

                    var doubleVal = BitConverter.Int64BitsToDouble(int64Val);

                    return doubleVal;
                }
                else
                {
                    return -1;
                }
                
            }
            catch (Exception)
            {
                return -1;
            }
            
        }

        private void AfficherIn7(object doubleVal)
        {
            try
            {
                lblIN7.Text = doubleVal.ToString();
            }
            catch (Exception e)
            {

                lblIN7.Text = e.Message;
            }
        }


        private void AfficherIn17(double doubleVal)
        {
            try
            {
                lblIN17.Text = doubleVal.ToString();
            }
            catch (Exception e)
            {

                lblIN17.Text = e.Message;
            }

            
        }

        private string GetTmValue_In7(byte[] received)
        {
            string value = "N/A";
            //parcourir jusqu'a la valeur DE 10 00 !! index 4318 (10DE)en hex
            byte[] tmIndex = MyStringToByteArray("DE 10 00");
            //WriteLog(MyDate() + " :  MyStringToByteArray 42 C6 ");
            int index = SearchBytes(received, tmIndex);
            //index = Array.
            //prendre la valeur i+4
            if(index > -1 && received.Length >= index + 4 + 8)
            {
                value = "0x" + BitConverter.ToString(received, index + 4, 8);
            }
            //WriteLog(MyDate() + " :  value " + value.ToString());

            return value;
        }

        private string GetTmValue_In17(byte[] received)
        {
            string value = "N/A";
            //parcourir jusqu'a la valeur 42 6c !! 66 198
            byte[] tmIndex = MyStringToByteArray("E1 10 00");
            //WriteLog(MyDate() + " :  MyStringToByteArray 42 C6 ");
            int index = SearchBytes(received, tmIndex);
            //index = Array.
            //prendre la valeur i+4
            if (index > -1 && received.Length >= index + 4 + 8)
            {
                value = "0x" + BitConverter.ToString(received, index + 4, 8);
            }
            //WriteLog(MyDate() + " :  value " + value.ToString());

            return value;
        }

        private string GetTmValue_Tk1(byte[] received)
        {
            string value = "N/A";
            //parcourir jusqu'a la valeur 00 01 00 !! index 1 (0001)en hex
            byte[] tmIndex = MyStringToByteArray("00 01 00");
            //WriteLog(MyDate() + " :  MyStringToByteArray 42 C6 ");
            int index = SearchBytes(received, tmIndex);

            //lastIndex = index;
            //index = Array.
            //prendre la valeur i+4
            if (index > -1 && index + 4 < received.Length)
            {
                value = "0x" + BitConverter.ToString(received, index + 4, 8);
            }
            //WriteLog(MyDate() + " :  value " + value.ToString());

            return value;
        }

        private string GetTmValue_ZS82(byte[] received)
        {
            string value = "N/A";
            //parcourir jusqu'a la valeur 42 6c !! 66 198
            byte[] tmIndex = MyStringToByteArray("42 6C 02");
            //WriteLog(MyDate() + " :  MyStringToByteArray 42 6c ");
            int index = SearchBytes(received, tmIndex);
            //index = Array.
            //prendre la valeur i+4
            if (index > -1 && index + 4 < received.Length)
            {
                value = "0x" + BitConverter.ToString(received, index + 4, 1);
            }
            //WriteLog(MyDate() + " :  value " + value.ToString());

            return value;
        }

        private byte[] MyStringToByteArray(string v)
        {
            string text = v;

            byte[] dataNewHex = new byte[0];
            if (text.Contains("-"))
            {
                dataNewHex = StringToByteArray(text.Replace("-", ""));
            }
            else
            {
                if (text.Contains(" "))
                {
                    dataNewHex = StringToByteArray(text.Replace(" ", ""));
                }
            }

            return dataNewHex;
        }

        public static byte[] StringToByteArray(String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        static int SearchBytes(byte[] haystack, byte[] needle)
        {
            var len = needle.Length;
            var limit = haystack.Length - len;
            for (var i = 0; i <= limit; i++)
            {
                var k = 0;
                for (; k < len; k++)
                {
                    if (needle[k] != haystack[i + k]) break;
                }
                if (k == len) return i;
            }
            return -1;
        }

        private void AfficherZS82(string data)
        {
            lblZS82.Text = data;
        }

        private void btnAck_Click(object sender, EventArgs e)
        {
            lblEndOfChargeMssg.Visible = false;
            WriteLog(MyDate() + " : Ack : End of charge");
            btnAck.Visible = false;
            ack = true;
            player0x11.Stop();

            //TODO vérifier changer la couleur iniquant la fin de stage
            //pnl5.BackColor = this.BackColor;

            itWasx44 = false;
            //TODO vider on newday
        }

        private void ViderAllText()
        {
            lblIn7_30befor.Text = "";
            lblIn17_30befor.Text = "";

            lblIn7_30befor.BackColor = Color.Transparent;
            lblIn17_30befor.BackColor = Color.Transparent;
            lbl30minBeforEclipse.Text = "";
            lblOutOfEclipseTime.Text = "";
            lblIn7_After.Text = "";
            lblIn17_After.Text = "";
            lblSunlightEntryTime.Text = "";
            lblContinuesChargeATime.Text = "";
            lblContinuesChargeBTime.Text = "";
            lblEndOfChargeTime.Text = "";
            lblDeltaIn17.Text = " ";
            lblDeltaIn7.Text = " ";
            lbl0xA.Text = "0xA";
            lbl0xB.Text = "0xB";
            lblEclipseEntryTime.Text = "";
        }

        private void txbLog_TextChanged(object sender, EventArgs e)
        {
            txbLog.SelectionStart = txbLog.Text.Length;
            txbLog.ScrollToCaret();
            //TODO unselect text
        }

        private void Play0x11AlarmSound()
        {
            try
            {
                player0x11.PlayLooping();
            }
            catch (Exception sex)
            {
                WriteLog(MyDate() + " : Err : Problem playing sound  ! : " + sex.Message);
            }
        }

        private void timerRecord_Tick(object sender, EventArgs e)
        {
            txbInterval.Text = timerRecord.Interval.ToString();
            if (timerRecord.Interval > 4000)
            {
                timerRecord.Interval = 900;
            }
            jNowTime = CalculJTimeSlash(DateTime.Now);

            long jTimeBeforEclipse = jStartTime - jNowTime;
            long jTime31MinBeforEclipse = jTimeBeforEclipse - 1860;
            //lblResult.Text = CalcuJTime("2023-09-23 19:49:43").ToString() + ".050";
            if (jTime31MinBeforEclipse < 5 & jTime31MinBeforEclipse > 0)
            {
                //flag30minOk = true;

                timerRecord.Stop();
                timerTimeLeft.Start();
                WriteLog(MyDate() + " : Time before Eclipse calculation started ! : ");
                lblTimeLeft.Visible = true;
                //MessageBox.Show("Time def : " + (jTimeBeforEclipse / 60).ToString() + " Minutes ");
                lbl30MinBefor.Visible = true;
                btnAck30min.Visible = true;

                stageTrackingTable[0] = 1;

                //StopTimer();
                //timerRecord.Stop();

                if (!cbxMutePnl1.Checked)
                {
                    Play0x11AlarmSound();
                }



                //save in7 in17 of 30 min befor

                //TODO start 2nd chance value !


                //MessageBox.Show("!!  2nd Chnace timer Started  !!");

                lblIn7_30befor.Text = lblIN7.Text;
                lblIn17_30befor.Text = lblIN17.Text;

                this.lblIN7.TextChanged += new System.EventHandler(this.lblIN7_TextChanged);
                this.lblIN17.TextChanged += new System.EventHandler(this.lblIN17_TextChanged);

                timer2ndVal.Start();

            }
            else
            {
                //if (jTime31MinBeforEclipse > 7200)
                //{
                //    //TODO test if the app opened less than this time !
                //    int newInterval = (Convert.ToInt32(jTime31MinBeforEclipse) * 1000) - 240000;

                //    if (newInterval > 900)
                //    {
                //        timerRecord.Interval = (Convert.ToInt32(jTime31MinBeforEclipse) * 1000) - 240000;
                //    }

                //    timerTimeLeft.Stop();
                //}
                const long TwoHoursInSeconds = 2 * 60 * 60;
                const int FourMinutesInMilliseconds = 4 * 60 * 1000;
                const int MinimumTimerInterval = 900;

                if (jTime31MinBeforEclipse > TwoHoursInSeconds)
                {
                    long interval = (jTime31MinBeforEclipse * 1000L) - FourMinutesInMilliseconds;

                    if (interval > MinimumTimerInterval && interval <= int.MaxValue)
                    {
                        timerRecord.Interval = (int)interval;
                    }

                    timerTimeLeft.Stop();
                }

            }


            //WriteLog(MyDate() + " : jTime31MinBeforEclipse ! : " + (jTime31MinBeforEclipse).ToString());

        }

        private void timerTimeLeft_Tick(object sender, EventArgs e)
        {
            jNowTime = CalculJTimeSlash(DateTime.Now);

            long jTimeBeforEclipse = jStartTime - jNowTime;

            lblTimeLeft.Text = (jTimeBeforEclipse - 1800).ToString();

            //WriteLog(MyDate() + " : TimeBeforEclipse ! : " + jTimeBeforEclipse);

            if (jTimeBeforEclipse - 1800 < 0)
            {
                lblTimeLeft.Visible = false;
                timerTimeLeft.Stop();
                lbl30minBeforEclipse.Text = DateTime.Now.TimeOfDay.ToString();
                //Take value of In7 In17
                //lblIn7_30befor.Text = lblIN7.Text;
                //lblIn17_30befor.Text = lblIN17.Text;

                //Take screen Shot if Mode is AutoScreenShot
                if (autoScreenShot)
                {
                    CaptureAndSaveScreenshot();
                }
                


                //si la valeur dans ce temps est egal a la valeur déja inscrit, on enleve la couleur JAUNE !

                if (lblIN17.Text == lblIn17_30befor.Text)
                {
                    lblIn17_30befor.BackColor = lblDateEclipse.BackColor;
                }

                if (lblIN7.Text == lblIn7_30befor.Text)
                {
                    lblIn7_30befor.BackColor = lblDateEclipse.BackColor;
                }





            }
        }

        private void txbEvent_TextChanged(object sender, EventArgs e)
        {
            txbEvent.SelectionStart = txbLog.Text.Length;
            txbEvent.ScrollToCaret();
        }

        private void btnAck30min_Click(object sender, EventArgs e)
        {
            lbl30MinBefor.Visible = false;
            lblTimeLeft.Visible = false;
            WriteLog(MyDate() + " : Ack : 30 min befor Eclipse");
            btnAck30min.Visible = false;
            //ack = true;
            player0x11.Stop();
            
            pnl1.BackColor = myGreenColor;

        }

        private void timerNewDay_Tick(object sender, EventArgs e)
        {
            //wait for new day of eclipse , 18 utc to restart all paramas
            //WriteLog(MyDate() + " : Calculation for new day !");

            jNowTime = CalculJTimeSlash(DateTime.Now);
            XmlDocument doc = new XmlDocument();

            try
            {
                doc.Load(xmlFilePath);
            }
            catch (Exception docExeption)
            {
                WriteLog(MyDate() + " : Error FND : " + docExeption.Message);
            }

            XmlNode node = doc.DocumentElement.SelectSingleNode("/CCR/YB");

            StringBuilder stringBuilder = new StringBuilder();

            int i = 1;
            foreach (XmlNode childNode in node.ChildNodes)
            {
                string nodeTime = childNode.ChildNodes[1].ChildNodes[0].InnerText;
                //string nodteStartTime
                nodeTime = nodeTime.Remove(nodeTime.Length - 4);
                long jNodeTime = CalculJTime(nodeTime);
                //stringBuilder.AppendLine(childNode.SelectSingleNode("/Record/start1/TIME").InnerText); 

                //Recherche de la derniere date of start eclipse: on change la date apres 16h utc + qlq min (01h qlq min) (on passe a la date de demain)
                i++;
                if (jNowTime - jNodeTime <= -jHoursBeforeStart)
                {
                    //stringBuilder.AppendLine("[" + i + "] : " + jNowTime + " < " + jNodeTime);
                    stringBuilder.AppendLine("[" + i + "] : " + jNowTime + " - " + jNodeTime + " = " + (jNowTime - jNodeTime));
                    //dateEclipse = StringToDate();
                    break;
                }
                //dateEclipse = jNodeTime
                jStartTime = CalculJTime(childNode.ChildNodes[1].ChildNodes[0].InnerText.Remove(childNode.ChildNodes[1].ChildNodes[0].InnerText.Length - 5));
                lblDateEclipse.Text = childNode.ChildNodes[1].ChildNodes[1].InnerText;
                lblStartEclipseTime.Text = childNode.ChildNodes[1].ChildNodes[2].InnerText.Remove(childNode.ChildNodes[1].ChildNodes[2].InnerText.Length - 5);
                lblEndEclipseTime.Text = childNode.ChildNodes[4].ChildNodes[2].InnerText.Remove(childNode.ChildNodes[4].ChildNodes[2].InnerText.Length - 5);
                stringBuilder.AppendLine("[" + i + "] : " + nodeTime + " :: " + (jNowTime - jNodeTime));
                //TODO convert jtime (find date time to show) that makes the condition true (jNowTime - jNodeTime <= -jHoursBeforeStart)
            }

            txbEvent.Text = stringBuilder.ToString();

            if (jStartTime != jOldStartTime)
            {
                afterEclipse = false;

                ResetPanelsColor();
                ViderAllText();
                player0x11.Stop();

                newDay = true;
                
                timerNewDay.Stop();
                timerRecord.Start();

            }
        }

        private void btn1_Click(object sender, EventArgs e)
        {
            pnl1.BackColor = myGreenColor;
        }

        private void btn2_Click(object sender, EventArgs e)
        {
            pnl2.BackColor = myGreenColor;
        }

        private void btn3_Click(object sender, EventArgs e)
        {
            pnl3.BackColor = myGreenColor;
        }

        private void btn4_Click(object sender, EventArgs e)
        {
            pnl4.BackColor = myGreenColor;
        }

        private void btn5_Click(object sender, EventArgs e)
        {
            pnl5.BackColor = myGreenColor;
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            ResetPanelsColor();
            ViderAllText();

        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if ((keyData & Keys.Control) > 0 && (keyData & Keys.KeyCode) == Keys.T)
            {
                //enable buttons
                EnableTestButtons();
                
                return true;

            }
            else
            {
                if ((keyData & Keys.Control) > 0 && (keyData & Keys.KeyCode) == Keys.S)
                {
                    //enable autoScreenShot
                    if (autoScreenShot)
                    {
                        autoScreenShot = false;
                        picBoxScreenShot.Visible = false;
                        //btnShot.Visible = false;

                    }
                    else
                    {
                        autoScreenShot = true;
                        picBoxScreenShot.Visible = true;
                        //btnShot.Visible = true;
                    }
                    

                    return true;

                }
            }
            return base.ProcessCmdKey(ref msg, keyData);

            
        }

        private void EnableTestButtons()
        {
            if (btnReset.Visible)
            {
                btnReset.Visible = false;
                btn1.Visible = false;
                btn2.Visible = false;
                btn3.Visible = false;
                btn4.Visible = false;
                btn5.Visible = false;
                btnFile.Visible = false;
                btnSound.Visible = false;
                btnColor.Visible = false;
                btnUJOST.Visible = false;
                btnIn7In17.Visible = false;
                txbIn7.Visible = false;
                txbIn17.Visible = false;
                btnStartDeltaV.Visible = false;
                btnStopDeltaV.Visible = false;
                lblIntervalOld.Visible = false;
                dtpOldTime.Visible = false;
                dtpNewTime.Visible = false;
                btnNewTime.Visible = false;
                EnablePage(tabPageTest, false);
                EnablePage(tabControl2, tpgEvent, false);

            }
            else
            {
                btnReset.Visible = true;
                btn1.Visible = true;
                btn2.Visible = true;
                btn3.Visible = true;
                btn4.Visible = true;
                btn5.Visible = true;
                btnFile.Visible = true;
                btnSound.Visible = true;
                btnColor.Visible = true;
                btnUJOST.Visible = true;
                btnIn7In17.Visible = true;
                txbIn7.Visible = true;
                txbIn17.Visible = true;
                btnStartDeltaV.Visible = true;
                btnStopDeltaV.Visible = true;
                lblIntervalOld.Visible = true;
                dtpOldTime.Visible = true;
                dtpNewTime.Visible = true;
                btnNewTime.Visible = true;
                EnablePage(tabPageTest, true);
                EnablePage(tabControl2, tpgEvent, true);
            }
            
        }

        private void timerDeltaIn7in17_Tick(object sender, EventArgs e)
        {
            //WriteLog(MyDate() + " : timerDeltaIn7in17 !");
            //calculate delta in7 in17
            //test if text is not vide lblIn17_30befor.Text != "" && lblIn7_30befor.Text
            if (lblIn17_30befor.Text != "" && lblIn7_30befor.Text != "")
            {
                double oldDoubleIn17 = double.Parse(lblIn17_30befor.Text.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
                double oldDoubleIn7 = double.Parse(lblIn7_30befor.Text.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
                double newDoubleIn17 = double.Parse(lblIN17.Text.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
                double newDoubleIn7 = double.Parse(lblIN7.Text.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);

                deltaIn7 = oldDoubleIn7 - newDoubleIn7;
                deltaIn17 = oldDoubleIn17 - newDoubleIn17;
                lblDeltaIn7.Text = deltaIn7.ToString();
                lblDeltaIn17.Text = deltaIn17.ToString();
            }
            
            if (deltaIn7 <= 0.44 && deltaIn17 <= 0.44)
            {
                // condtion is verified
                lblDeltaOK.Visible = true;
                lblIn17_After.Text = lblIN17.Text;
                lblIn7_After.Text = lblIN7.Text;
                lblSunlightEntryTime.Text = DateTime.Now.TimeOfDay.ToString();
                timerDeltaIn7in17.Enabled = false;
                //TODO in newDay ! //deltaVerificationStarted = false;
                btnAckDelta.Visible = true;
                if (!cbxMutePnl3.Checked)
                {
                    Play0x11AlarmSound();
                }
                WriteLog(MyDate() + " : In7 In17 Condtion verified !");
                timerDeltaIn7in17.Stop();

                //take scrreen shot if AutoScreenShot
                if (autoScreenShot)
                {
                    CaptureAndSaveScreenshot();
                }
            }

        }

        private void Play0x11Sound()
        {
            try
            {
                player0x11.Play();
            }
            catch (Exception sex)
            {

                WriteLog(MyDate() + " : Err : Problem playing sound  ! : " + sex.Message);
            }
        }

        private void cbxMutePnl5_CheckedChanged(object sender, EventArgs e)
        {
            if (cbxMutePnl5.Checked)
            {
                cbxMutePnl5.Font = new Font(cbxMutePnl5.Font, FontStyle.Bold);
                picBoxMutePnl5.Visible = true;
                //cbxMutePnl1.BackColor = Color.IndianRed;
                IfMuteAll();
            }
            else
            {
                cbxMutePnl5.Font = new Font(cbxMutePnl5.Font, FontStyle.Regular);
                cbxMutePnl5.BackColor = Color.Transparent;
                picBoxMutePnl5.Visible = false;
                IfNotMuteAll();
            }
        }

        private void cbxMutePnl1_CheckedChanged(object sender, EventArgs e)
        {
            if (cbxMutePnl1.Checked)
            {
                cbxMutePnl1.Font = new Font(cbxMutePnl1.Font, FontStyle.Bold);
                picBoxMutePnl1.Visible = true;
                //cbxMutePnl1.BackColor = Color.IndianRed;
                IfMuteAll();
            }
            else
            {
                cbxMutePnl1.Font = new Font(cbxMutePnl1.Font, FontStyle.Regular);
                //cbxMutePnl1.BackColor = Color.Transparent;
                picBoxMutePnl1.Visible = false;
                IfNotMuteAll();
            }
        }

        private void IfMuteAll()
        {
            if (cbxMutePnl1.Checked && cbxMutePnl3.Checked && cbxMutePnl5.Checked)
            {
                cbxMuteAll.Checked = true;
            }
        }

        private void IfNotMuteAll()
        {
            if (!cbxMutePnl1.Checked && !cbxMutePnl3.Checked && !cbxMutePnl5.Checked)
            {
                cbxMuteAll.Checked = false;
            }
        }

        private void cbxMutePnl3_CheckedChanged(object sender, EventArgs e)
        {
            if (cbxMutePnl3.Checked)
            {
                cbxMutePnl3.Font = new Font(cbxMutePnl3.Font, FontStyle.Bold);
                picBoxMutePnl3.Visible = true;
                IfMuteAll();
            }
            else
            {
                cbxMutePnl3.Font = new Font(cbxMutePnl3.Font, FontStyle.Regular);
                cbxMutePnl3.BackColor = Color.Transparent;
                picBoxMutePnl3.Visible = false;
                IfNotMuteAll();
            }
        }

        private void btnAckDelta_Click(object sender, EventArgs e)
        {
            lblDeltaOK.Visible = false;
            WriteLog(MyDate() + " : Ack : Delta in7 in17");
            btnAckDelta.Visible = false;
            player0x11.Stop();

            stageTrackingTable[2] = 1;
        }

        private void cbxMuteAll_CheckedChanged(object sender, EventArgs e)
        {
            if (cbxMuteAll.Checked)
            {
                cbxMuteAll.Font = new Font(cbxMuteAll.Font, FontStyle.Bold);

                //check all
                cbxMutePnl1.Checked = true;
                cbxMutePnl3.Checked = true;
                cbxMutePnl5.Checked = true;
            }
            else
            {
                cbxMuteAll.Font = new Font(cbxMuteAll.Font, FontStyle.Regular);

                //Uncheck all
                cbxMutePnl1.Checked = false;
                cbxMutePnl3.Checked = false;
                cbxMutePnl5.Checked = false;
            }
        }

        private void btnUJOST_Click(object sender, EventArgs e)
        {
            UpdatejOldStartTime();
        }

        private void btnIn7In17_Click(object sender, EventArgs e)
        {
            lblIn7_30befor.Text = txbIn7.Text;
            lblIn17_30befor.Text = txbIn17.Text;
        }

        private void btnStartDeltaV_Click(object sender, EventArgs e)
        {
            timerDeltaIn7in17.Start();
        }

        private void btnStopDeltaV_Click(object sender, EventArgs e)
        {
            timerDeltaIn7in17.Stop();
        }

        private void btnNewTime_Click(object sender, EventArgs e)
        {
            lblIntervalOld.Text = timerRecord.Interval.ToString();
            int mDifTime = Convert.ToInt32((dtpNewTime.Value - dtpOldTime.Value).TotalSeconds) * 1000;
            lblDiffTime.Text = mDifTime.ToString();
            try
            {
                timerRecord.Interval -= mDifTime;
                timerRecord.Stop();
                timerRecord.Start();
            }
            catch (ArgumentOutOfRangeException outexp)
            {
                lblInterval.Text = outexp.Message;
            }
            catch (Exception exp)
            {
                lblInterval.Text = exp.Message;
            }

            lblInterval.Text = timerRecord.Interval.ToString();
            lblIntervalMinutes.Text = (timerRecord.Interval / (3600 * 1000)).ToString();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.WM_SHOWME)
            {
                ShowMe();
            }
            base.WndProc(ref m);
        }
        private void ShowMe()
        {
            if (WindowState == FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Normal;
            }
            // get our current "TopMost" value (ours will always be false though)
            bool top = TopMost;
            // make our form jump to the top of everything
            TopMost = true;
            // set it back to whatever it was
            TopMost = top;
        }

        private void MainForm_EA_FormClosing(object sender, FormClosingEventArgs e)
        {
            // If user (not system!) wants to close the form
            // but (s)he answered "no", do not close the form
            if (e.CloseReason == CloseReason.UserClosing)
                e.Cancel = MessageBox.Show(@"Are you sure you want to close the Eclipse Assistant?",
                                           Application.ProductName,
                                           MessageBoxButtons.YesNo) == DialogResult.No;
        }

        private void lblIN7_TextChanged(object sender, EventArgs e)
        {

            try
            {
                if (Smaller(lblIN7.Text, lblIn7_30befor.Text))
                {
                    lblIn7_30befor.Text = lblIN7.Text;
                    lblIn7_30befor.BackColor = Color.GreenYellow;

                    WriteLog(MyDate() + " : In7 value changed ! : In7 =" + lblIn7_30befor.Text);
                }
            }
            catch (Exception exp)
            {
                WriteLog(MyDate() + " : ERROR LBLIN7 : lblIN7_TextChanged : " + exp.Message);
            }
            
        }

        private bool Smaller(string text1, string text2)
        {
            double doubleText1 = double.Parse(text1.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
            double doubleText2 = double.Parse(text2.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);

            return doubleText1 < doubleText2 ;
        }

        private void btnEvent_Click(object sender, EventArgs e)
        {
            //Test
            MessageBox.Show("!!  2nd Chnace timer Started  !!");

            lblIn7_30befor.Text = lblIN7.Text;
            lblIn17_30befor.Text = lblIN17.Text;

            this.lblIN7.TextChanged += new System.EventHandler(this.lblIN7_TextChanged);
            this.lblIN17.TextChanged += new System.EventHandler(this.lblIN17_TextChanged);

            timer2ndVal.Start();
        }

        private void btnDisenvent_Click(object sender, EventArgs e)
        {
            this.lblIN7.TextChanged -= new System.EventHandler(this.lblIN7_TextChanged);
            this.lblIN17.TextChanged -= new System.EventHandler(this.lblIN17_TextChanged);
        }

        private void lblIN17_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (Smaller(lblIN17.Text, lblIn17_30befor.Text))
                {
                    lblIn17_30befor.Text = lblIN17.Text;
                    lblIn17_30befor.BackColor = Color.GreenYellow;

                    WriteLog(MyDate() + " : In17 value changed ! : In17 =" + lblIn17_30befor.Text);

                }
            }
            catch (Exception exp)
            {

                WriteLog(MyDate() + " : ERROR LBLIN17 : lblIN17_TextChanged : " + exp.Message);
            }

            
            

            
        }

        private void timer2ndVal_Tick(object sender, EventArgs e)
        {
            this.lblIN7.TextChanged -= new System.EventHandler(this.lblIN7_TextChanged);
            this.lblIN17.TextChanged -= new System.EventHandler(this.lblIN17_TextChanged);
            //MessageBox.Show("!!  -= new  !!");
            timer2ndVal.Stop();
        }

        private void btnColor_Click(object sender, EventArgs e)
        {
            lblIn17_30befor.BackColor = Color.GreenYellow;
        }

        private void btnShot_Click(object sender, EventArgs e)
        {
            CaptureAndSaveScreenshot();
        }

        public static void CaptureAndSaveScreenshot()
        {
            try
            {
                // Créer un dossier avec la date du jour
                string basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Captures");
                string folderName = DateTime.Now.ToString("yyyy-MM-dd");
                string savePath = Path.Combine(basePath, folderName);

                if (!Directory.Exists(savePath))
                {
                    Directory.CreateDirectory(savePath);
                }

                // Capturer l'écran
                using (Bitmap screenshot = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height))
                {
                    using (Graphics g = Graphics.FromImage(screenshot))
                    {
                        g.CopyFromScreen(0, 0, 0, 0, screenshot.Size);
                    }

                    // Générer un nom de fichier avec la date et l'heure
                    string fileName = DateTime.Now.ToString("HH-mm-ss") + ".png";
                    string filePath = Path.Combine(savePath, fileName);

                    // Enregistrer l'image
                    screenshot.Save(filePath, ImageFormat.Png);
                    Console.WriteLine($"Capture enregistrée : {filePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur lors de la capture : " + ex.Message);
            }
        }

        private void picBoxScreenShot_Click(object sender, EventArgs e)
        {
            CaptureAndSaveScreenshot();
        }

        private void btnFile_Click(object sender, EventArgs e)
        {
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(xmlFilePath);
                WriteLog(MyDate() + " : Test : File loaded successfully !");
            }
            catch (Exception docExeption)
            {
                WriteLog(MyDate() + " : Error XML : " + docExeption.Message);
            }
        }

        private void btnSound_Click(object sender, EventArgs e)
        {
            Play0x11Sound();
        }
    }
}
