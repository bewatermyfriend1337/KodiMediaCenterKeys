/*
 * Andrew Laychak
 * 9/5/2014
 * Logitech G15 Display and Media Keys for Kodi
 */

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;

namespace Kodi_Media_Keys
{
    public class LogitechDisplay
    {
        private static System.Timers.Timer updateTimer;

        public int connection = DMcLgLCD.LGLCD_INVALID_CONNECTION;
        public int device = DMcLgLCD.LGLCD_INVALID_DEVICE;
        public int deviceType = DMcLgLCD.LGLCD_INVALID_DEVICE;

        private int config = 0;

        private string album;
        private string artist;
        private string title;

        private int currentMinutes = 0;
        private int currentSeconds = 0;

        private int totalMinutes = 0;
        private int totalSeconds = 0;

        private string cTime;
        private string tTime;

        private RectangleF rTitle = new RectangleF(22.5f, 20, 1000, 10);
        private int tmpLocationX;
        private bool reverseTitle = false;

        private bool setStartPosition = false;
        private int startPositionX;

        private int maxCharsTitle = 33;

        PictureBox pbLCD;
        Bitmap LCD;

        System.Timers.Timer t;

        NotifyIcon niKodi_MediaKeys;
        string previousSong = "";

        string rpcLocation = "http://192.168.1.9:8282/jsonrpc"; // IP of the computer running Kodi (as well as the port)

        public string Title { get { return title; } }
        public string Artist { get { return artist; } }
        public string Album { get { return album; } }
        public string Time { get { return cTime + " / " + tTime; } }

        public void Initialize(NotifyIcon niKodi_MediaKeys, PictureBox pbLCD)
        {
            this.niKodi_MediaKeys = niKodi_MediaKeys;

            this.pbLCD = pbLCD;
            tmpLocationX = (int)rTitle.X;

            Kodi_Applet_Load(this, null);
        }

        private void Kodi_Applet_Load(object sender, EventArgs e)
        {
            if (DMcLgLCD.ERROR_SUCCESS == DMcLgLCD.LcdInit())
            {
                connection = DMcLgLCD.LcdConnectEx("Kodi Media Keys", 0, 0);

                if (DMcLgLCD.LGLCD_INVALID_CONNECTION != connection)
                {
                    device = DMcLgLCD.LcdOpenByType(connection, DMcLgLCD.LGLCD_DEVICE_QVGA);

                    if (DMcLgLCD.LGLCD_INVALID_DEVICE == device)
                    {
                        device = DMcLgLCD.LcdOpenByType(connection, DMcLgLCD.LGLCD_DEVICE_BW);
                        if (DMcLgLCD.LGLCD_INVALID_DEVICE != device)
                        {
                            deviceType = DMcLgLCD.LGLCD_DEVICE_BW;
                        }
                    }
                    else
                    {
                        deviceType = DMcLgLCD.LGLCD_DEVICE_QVGA;
                    }

                    if (DMcLgLCD.LGLCD_DEVICE_BW == deviceType)
                    {
                        LCD = new Bitmap(160, 43);
                        Graphics g = Graphics.FromImage(LCD);
                        g.Clear(Color.White);
                        g.Dispose();

                        DMcLgLCD.LcdUpdateBitmap(device, LCD.GetHbitmap(), DMcLgLCD.LGLCD_DEVICE_BW);
                        DMcLgLCD.LcdSetAsLCDForegroundApp(device, DMcLgLCD.LGLCD_FORE_YES);

                        pbLCD.Width = 160;
                        pbLCD.Height = 43;
                    }

                    if (deviceType > 0)
                    {
                        DMcLgLCD.LcdSetConfigCallback(cfgCallback);

                        updateTimer = new System.Timers.Timer(100);

                        updateTimer.Elapsed += new System.Timers.ElapsedEventHandler(OnTimedEvent);

                        updateTimer.Interval = 100;
                        updateTimer.Enabled = true;
                    }
                }
            }
        }

        private void OnTimedEvent(object sender, System.Timers.ElapsedEventArgs e)
        {
            Font sFont;

            Graphics g = Graphics.FromImage(LCD);
            g.Clear(Color.White);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

            sFont = new Font("Arial", 7, FontStyle.Regular);

            if (DMcLgLCD.LGLCD_DEVICE_BW == deviceType)
            {
                RectangleF rtTitle = new RectangleF(0, 20, 24, 10);
                g.DrawString("Album: " + album, sFont, Brushes.Black, 0, 0);
                g.DrawString("Artist: " + artist, sFont, Brushes.Black, 0, 10);
                g.DrawString(title, sFont, Brushes.Black, rTitle);
                g.FillRectangle(Brushes.White, rtTitle);
                g.DrawString("Title: ", sFont, Brushes.Black, rtTitle);
                g.DrawString("Duration: " + cTime + " / " + tTime, sFont, Brushes.Black, 0, 30);
                DMcLgLCD.LcdUpdateBitmap(device, LCD.GetHbitmap(), DMcLgLCD.LGLCD_DEVICE_BW);

                #region
                int charsTitle = 0;
                SizeF stringSize;

                if (title != null)
                {
                    charsTitle = title.Length;
                    stringSize = g.MeasureString(title, sFont);
                    rTitle.Width = stringSize.Width;
                }

                if (charsTitle > maxCharsTitle)
                {
                    if (!reverseTitle)
                    {
                        if (tmpLocationX >= -rTitle.Width + 150)
                        {
                            tmpLocationX = (int)rTitle.X - 1;
                            if (tmpLocationX <= -rTitle.Width + 150)
                            {
                                reverseTitle = true;
                            }
                        }
                    }
                    else
                    {
                        tmpLocationX = (int)rTitle.X + 1;
                        if (tmpLocationX >= 30)
                        {
                            reverseTitle = false;
                        }
                    }
                    rTitle.X = tmpLocationX;
                }
                else
                {
                    //Console.WriteLine(rtTitle.Width);
                    rTitle.X = rtTitle.Width;
                }
                #endregion
            }

            pbLCD.Image = LCD;

            sFont.Dispose();
            g.Dispose();

            bool audioPlaying = checkAudio();

            if (audioPlaying)
            {
                if (artist != "")
                {
                    if (title != previousSong)
                    {
                        ResetTitleBox();
                        BalloonTimer();
                    }
                }

                btnRequest_Click(this, null);
            }
        }

        public void cfgCallback(int cfgConnection)
        {
            config = cfgConnection;
        }

        private void btnRequest_Click(object sender, EventArgs e)
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(rpcLocation);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                string json = "{\"jsonrpc\": \"2.0\", \"method\": \"Player.GetItem\", \"params\": { \"properties\": [\"title\", \"album\", \"artist\", \"duration\", \"thumbnail\", \"file\", \"fanart\", \"streamdetails\"], \"playerid\": 0 }, \"id\": \"AudioGetItem\"}";

                streamWriter.Write(json);
                streamWriter.Flush();
                streamWriter.Close();

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                    var results = JsonConvert.DeserializeObject<dynamic>(result);
                    JObject o = JObject.Parse(result);

                    JToken currentlyPlaying = (JToken)o["result"]["item"]["album"];

                    if (currentlyPlaying != null)
                    {
                        album = (string)o["result"]["item"]["album"];
                        artist = (string)o["result"]["item"]["artist"][0];
                        //duration = (int)o["result"]["item"]["duration"];
                        title = (string)o["result"]["item"]["title"];
                    }
                    else
                    {
                        album = "";
                        artist = "";
                        cTime = "0";
                        tTime = "0";
                        title = "";
                    }
                }
            }
        }

        private bool checkAudio()
        {
            bool isAudioPlaying = false;

            try
            {
                HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(rpcLocation);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    //string json = "{\"jsonrpc\": \"2.0\", \"method\": \"Player.GetActivePlayers\", \"id\": 1}";
                    string json = "{\"jsonrpc\": \"2.0\", \"method\": \"Player.GetProperties\", \"params\": { \"properties\": [\"time\", \"totaltime\", \"speed\"], \"playerid\": 0 }, \"id\": \"AudioPlayer\"}";

                    streamWriter.Write(json);
                    streamWriter.Flush();
                    streamWriter.Close();

                    var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        var result = streamReader.ReadToEnd();

                        var results = JsonConvert.DeserializeObject<dynamic>(result);
                        JObject o = JObject.Parse(result);

                        //JToken currentlyPlaying = (JToken)o["result"];
                        JToken currentlyPlaying = (JToken)o["result"]["speed"];
                        currentMinutes = (int)o["result"]["time"]["minutes"];
                        currentSeconds = (int)o["result"]["time"]["seconds"];

                        totalMinutes = (int)o["result"]["totaltime"]["minutes"];
                        totalSeconds = (int)o["result"]["totaltime"]["seconds"];

                        cTime = currentMinutes + ":" + currentSeconds.ToString("D2");
                        tTime = totalMinutes + ":" + totalSeconds.ToString("D2");

                        if ((int)currentlyPlaying != 0)
                        {
                            isAudioPlaying = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //Do Nothing
            }

            return isAudioPlaying;
        }

        public void Disable()
        {
            updateTimer.Enabled = false;

            if (LCD != null)
            {
                LCD.Dispose();
            }
            DMcLgLCD.LcdClose(device);
            DMcLgLCD.LcdDisconnect(connection);
            DMcLgLCD.LcdDeInit();
        }

        private void BalloonTimer()
        {
            t = new System.Timers.Timer();
            t.AutoReset = false;
            t.Interval = 1000;
            t.Enabled = true;
            t.Start();
            t.Elapsed += new ElapsedEventHandler(FinishedTimer);
        }

        private void FinishedTimer(object source, ElapsedEventArgs e)
        {
            ShowBalloon();
            previousSong = title;
            t.Stop();
        }

        private void ShowBalloon()
        {
            string bText = "Artist: " + artist + "\nTitle: " + title + "\nAlbum: " + album;
            niKodi_MediaKeys.ShowBalloonTip(500, "Current Song", bText, ToolTipIcon.None);
        }

        public void ResetTitleBox()
        {
            tmpLocationX = (int)rTitle.X;
        }
    }
}