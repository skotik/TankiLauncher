using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Xml;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace ArmataLauncher
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            if (Properties.Settings.Default.cbLogin)
            {
                tbLogin.Text = Properties.Settings.Default.tbLogin;
                if (Properties.Settings.Default.cbPassword &&
                    !string.IsNullOrEmpty(Properties.Settings.Default.refresh))
                {
                    
                    cbPassword.Checked = true;
                    tbPassword.Enabled = false;
                    tbLogin.Enabled = false;
                    tbPassword.Text = "************";
                    tokenrefresh = Properties.Settings.Default.refresh;

                    if (Properties.Settings.Default.cbAuto) {
                        cbStart.Checked = true;
                    }
                }
            }
        }

        string tokenauth = "";
        string tokenrefresh = "";
        string tokenmpop = "";
//		string ProjectId = "";


        private void DoMagic() {
            bStart.Enabled = false;
            lStat.Text = "Авторизуемся";
            Application.DoEvents();
            GetAuthToken();
            if (string.IsNullOrEmpty(tokenauth))
            {
                lStat.Text = "Авторизация не прошла";
                Application.DoEvents();
                return;
            }
            if (string.IsNullOrEmpty(tokenmpop))
            {
                lStat.Text = "Авторизация фаза 2";
                Application.DoEvents();
                GetMPop();
            }
            if (string.IsNullOrEmpty(tokenmpop))
            {
				return;
			}
            lStat.Text = "Авторизация фаза 3";
            Application.DoEvents();
            Auth();
            SaveProperties();
            bStart.Enabled = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DoMagic();
        }

        CookieContainer myCookie = new CookieContainer();

        private void SaveProperties() {
            Properties.Settings.Default.cbLogin = cbLogin.Checked;
            if (cbLogin.Checked) {
                Properties.Settings.Default.tbLogin = tbLogin.Text;
            }
            Properties.Settings.Default.cbPassword = cbPassword.Checked;
            if (cbPassword.Checked)
            {
                Properties.Settings.Default.refresh = tokenrefresh;
            }
            else {
                Properties.Settings.Default.refresh = "";
            }
            Properties.Settings.Default.cbAuto = cbStart.Checked;
            Properties.Settings.Default.Save();
        }

        private void GetAuthToken() {
            HttpWebRequest myWebRequest = (HttpWebRequest)WebRequest.Create(@"https://o2.mail.ru/token");
            myWebRequest.ContentType = "application/x-www-form-urlencoded";
            myWebRequest.Method = "POST";
            myWebRequest.UserAgent = "Downloader/11450";
//            myWebRequest.CookieContainer = myCookie;
            string postdata = "";
            if (string.IsNullOrEmpty(tokenrefresh))
            {
                postdata = "client_id=gamecenter.mail.ru&grant_type=password&username=" + tbLogin.Text + "&password=" + tbPassword.Text;
            }
            else {
                postdata = "client_id=gamecenter.mail.ru&grant_type=refresh_token&refresh_token=" + tokenrefresh;
            }

            byte[] postbyte = Encoding.UTF8.GetBytes(postdata);
            myWebRequest.ContentLength = postdata.Length;
            Stream MyRequest = myWebRequest.GetRequestStream();
            MyRequest.Write(postbyte, 0, postbyte.Length);
            MyRequest.Close();
            HttpWebResponse myResponse = (HttpWebResponse)myWebRequest.GetResponse();
            Stream myStream1 = myResponse.GetResponseStream();
            StreamReader sr1 = new StreamReader(myStream1);
            String myRespString1 = sr1.ReadToEnd();
            Regex re = new Regex(@"""error"":""(.*?)""", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            MatchCollection mc = re.Matches(myRespString1);
            if (mc.Count > 0) {
                MessageBox.Show(mc[0].Groups[1].ToString());
                tokenauth = "";
                return;
            }

            re = new Regex(@"""refresh_token"":""(.*?)""", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            mc = re.Matches(myRespString1);
            if (mc.Count > 0)
            {
                tokenrefresh = mc[0].Groups[1].ToString();
            }
            re = new Regex(@"""access_token"":""(.*?)""", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            mc = re.Matches(myRespString1);
            if (mc.Count > 0) { 
                tokenauth = mc[0].Groups[1].ToString(); 
            }
            myResponse.Close();

        }

        private void GetMPop()
        {
            HttpWebRequest myWebRequest = (HttpWebRequest)WebRequest.Create(@"https://authdl.mail.ru/ec.php?hint=MrPage2");
            myWebRequest.ContentType = "application/x-www-form-urlencoded";
            myWebRequest.Method = "POST";
            myWebRequest.UserAgent = "Downloader/11450";
//            myWebRequest.CookieContainer = myCookie;
            string postdata = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><MrPage2 SessionKey=\"" + tokenauth + "\" Page=\"https://dl.mail.ru/robots.txt\"/>";

            byte[] postbyte = Encoding.UTF8.GetBytes(postdata);
            myWebRequest.ContentLength = postdata.Length;
            Stream MyRequest = myWebRequest.GetRequestStream();
            MyRequest.Write(postbyte, 0, postbyte.Length);
            MyRequest.Close();
            HttpWebResponse myResponse = (HttpWebResponse)myWebRequest.GetResponse();
            Stream myStream1 = myResponse.GetResponseStream();
            StreamReader sr1 = new StreamReader(myStream1);
            String myRespString1 = sr1.ReadToEnd();
            myResponse.Close();

            Regex re = new Regex(@"Location=""(.*?)""", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            MatchCollection mc = re.Matches(myRespString1);
            string relocation = "";
            if (mc.Count > 0)
            {
                relocation = mc[0].Groups[1].ToString();
                relocation = relocation.Replace("&amp;","&");
            } else {
				lStat.Text = "Нет Location :(";
				return;
			}

            HttpWebRequest myWebRequest2 = (HttpWebRequest)WebRequest.Create(relocation);
            myWebRequest2.UserAgent = "Downloader/11450";
            myWebRequest2.AllowAutoRedirect = false;
            myWebRequest2.CookieContainer = myCookie;
            HttpWebResponse myResponse2 = (HttpWebResponse)myWebRequest2.GetResponse();
            myResponse2.Close();

            string mpop = "";
            foreach (Cookie cook in myResponse2.Cookies)
            {
                if (cook.Name.ToUpper() == "MPOP")
                {
                    mpop = cook.Value; 
                }
            }
            if (string.IsNullOrEmpty(mpop))
            {
				lStat.Text = "Нет Печеньки :(";
				return;
                //error
            }
            else {
                tokenmpop = mpop;
            }

        }


        private void Auth() {
            string mpopdata = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><AutoLogin ProjectId=\"11321\" SubProjectId=\"0\" ShardId=\"0\" Mpop=\""+tokenmpop+"\"/>";
            HttpWebRequest myWebRequest2 = (HttpWebRequest)WebRequest.Create(@"https://authdl.mail.ru/sz.php?hint=AutoLogin");
            myWebRequest2.ContentType = "application/x-www-form-urlencoded";
            myWebRequest2.Method = "POST";
            myWebRequest2.UserAgent = "Downloader/11450";
//            myWebRequest2.CookieContainer = myCookie;

            byte[] mpoppostbyte = Encoding.UTF8.GetBytes(mpopdata);
            Stream MyRequest2 = myWebRequest2.GetRequestStream();
            MyRequest2.Write(mpoppostbyte, 0, mpoppostbyte.Length);
            MyRequest2.Close();
            HttpWebResponse myResponse2 = (HttpWebResponse)myWebRequest2.GetResponse();

            Stream myStream = myResponse2.GetResponseStream();
            StreamReader sr = new StreamReader(myStream);
            String myRespString = sr.ReadToEnd();
            myResponse2.Close();
            XmlDocument xmldoc = new XmlDocument();
            xmldoc.LoadXml(myRespString);
            string myAPersId = xmldoc.GetElementsByTagName("AutoLogin").Item(0).Attributes["PersId"].Value;
            string myAKey = xmldoc.GetElementsByTagName("AutoLogin").Item(0).Attributes["Key"].Value;
            Environment.SetEnvironmentVariable("GC_PROJECT_ID", "11321");
            Environment.SetEnvironmentVariable("GC_TYPE_ID", "0");
            lStat.Text = "Запуск игры";
            Application.DoEvents();
            System.Threading.Thread.Sleep(5000);
            Process Armata = Process.Start("ArmoredWarfare.exe", " --sz_pers_id=" + myAPersId + " --sz_token=" + myAKey);
            System.Threading.Thread.Sleep(5000);
            lStat.Text = "Играем";
            Armata.EnableRaisingEvents = true;
            Armata.Exited += new EventHandler(Armata_Exited);
        }

        private void Armata_Exited(object sender, System.EventArgs e)
        {

            lStat.Text = "Game Over";
        }



        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveProperties();
        }

        private void cbPassword_CheckedChanged(object sender, EventArgs e)
        {
            if (!cbPassword.Checked) {
                tbPassword.Enabled = true;
                tbLogin.Enabled = true;
                cbStart.Checked = false;
            }
        }

        private void tbPassword_TextChanged(object sender, EventArgs e)
        {
            tokenrefresh = "";
        }

        private void tbLogin_TextChanged(object sender, EventArgs e)
        {
            tokenrefresh = "";
        }

        private void cbLogin_CheckedChanged(object sender, EventArgs e)
        {
            if (!cbLogin.Checked)
            {
                tbPassword.Enabled = true;
                tbLogin.Enabled = true;
                cbPassword.Checked = false;
                cbStart.Checked = false;
            }
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            if (cbStart.Checked)
            {
                DoMagic();
            }
        }
    }
}
