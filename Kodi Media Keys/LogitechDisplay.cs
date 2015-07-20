/*
 * Andrew Laychak
 * http://www.alaychak.com/
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
            tmpLocationX = (int)rTitle.X; // Sets the location of the text to the default. Used for overflowing text

            Kodi_Applet_Load(this, null);
        }

        private void Kodi_Applet_Load(object sender, EventArgs e)
        {
            if (DMcLgLCD.ERROR_SUCCESS == DMcLgLCD.LcdInit())
            {
                connection = DMcLgLCD.LcdConnectEx("Kodi Media Keys", 0, 0); // Initializes the display with a useful name to identify the application

                if (DMcLgLCD.LGLCD_INVALID_CONNECTION != connection)
                {
                    device = DMcLgLCD.LcdOpenByType(connection, DMcLgLCD.LGLCD_DEVICE_QVGA);

                    if (DMcLgLCD.LGLCD_INVALID_DEVICE == device) // Determines whether or not the keyboard is a G15 or G19 (currently the application is only tested on a G15)
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
                        LCD = new Bitmap(160, 43); // Sets the screen size of the keyboard display
                        Graphics g = Graphics.FromImage(LCD);
                        g.Clear(Color.White);
                        g.Dispose();

                        DMcLgLCD.LcdUpdateBitmap(device, LCD.GetHbitmap(), DMcLgLCD.LGLCD_DEVICE_BW);
                        DMcLgLCD.LcdSetAsLCDForegroundApp(device, DMcLgLCD.LGLCD_FORE_YES);

                        pbLCD.Width = 160; // Sets the picture box display (that is shown when the user double clicks the icon).
                        pbLCD.Height = 43;
                    }

                    if (deviceType > 0) // Only update the screen if there is a screen to update.
                    {
                        DMcLgLCD.LcdSetConfigCallback(cfgCallback);

                        updateTimer = new System.Timers.Timer(100);

                        updateTimer.Elapsed += new System.Timers.ElapsedEventHandler(OnTimedEvent);

                        updateTimer.Interval = 100; // Wil. update the display every 100ms. Likely only required to use a value of 1000. This is required to update the current song time.
                        updateTimer.Enabled = true;
                    }
                }
            }
        }

        private void OnTimedEvent(object sender, System.Timers.ElapsedEventArgs e)
        {
            Font sFont;

            Graphics g = Graphics.FromImage(LCD); // Creates a graphics for the display
            g.Clear(Color.White);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

            sFont = new Font("Arial", 7, FontStyle.Regular); // Creates a font that is used to write to the display. Should be able to change this to whatever you want to use. 

            if (DMcLgLCD.LGLCD_DEVICE_BW == deviceType)
            {
                RectangleF rtTitle = new RectangleF(0, 20, 24, 10); // Writes all the information that the RPC has returned to the display using the font specified above.
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

            sFont.Dispose(); // Disposes the fonts and graphics after the values were displayed on the screen.
            g.Dispose();

            bool audioPlaying = checkAudio(); // Checks to determine whether the audio is currently playing or not, which is then used to update the balloon.

            if (audioPlaying)
            {
                if (artist != "")
                {
                    if (title != previousSong) // Only display the balloon popup on a new song.
                    {
                        ResetTitleBox();
                        BalloonTimer();
                    }
                }

                btnRequest_Click(this, null); // Requests the song information. Required because the song updates the time every second.
            }
        }

        public void cfgCallback(int cfgConnection)
        {
            config = cfgConnection;
        }

        private void btnRequest_Click(object sender, EventArgs e)
        {
            // Creates a RPC that POSTs to the media center server with the "Get Item" method
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
                    JObject o = JObject.Parse(result);  // Parses the returned value into an object.

                    JToken currentlyPlaying = (JToken)o["result"]["item"]["album"];

                    if (currentlyPlaying != null)
                    {
                        album = (string)o["result"]["item"]["album"]; // Uses the above object to retrieve the required information
                        artist = (string)o["result"]["item"]["artist"][0];
                        title = (string)o["result"]["item"]["title"];
                    }
                    else
                    {
                        ResetAllSongInfo(); // Wil reset any values to its defaults if it detects that there is nothing being played.
                    }
                }
            }
        }

        private bool checkAudio()
        {
            // Creates a RPC that POSTs to the media center server with the "Get Properties" method
            bool isAudioPlaying = false; // Sets the playing to the default value of false. Required to return this value.

            try
            {
                HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(rpcLocation);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    string json = "{\"jsonrpc\": \"2.0\", \"method\": \"Player.GetProperties\", \"params\": { \"properties\": [\"time\", \"totaltime\", \"speed\"], \"playerid\": 0 }, \"id\": \"AudioPlayer\"}";

                    streamWriter.Write(json);
                    streamWriter.Flush();
                    streamWriter.Close();

                    var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        var result = streamReader.ReadToEnd();

                        var results = JsonConvert.DeserializeObject<dynamic>(result);
                        JObject o = JObject.Parse(result); // Parses the returned value into an object.

                        JToken currentlyPlaying = (JToken)o["result"]["speed"]; // Retrieves the speed value of the object in order to determine the time.
                        currentMinutes = (int)o["result"]["time"]["minutes"];
                        currentSeconds = (int)o["result"]["time"]["seconds"];

                        totalMinutes = (int)o["result"]["totaltime"]["minutes"];
                        totalSeconds = (int)o["result"]["totaltime"]["seconds"];

                        cTime = currentMinutes + ":" + currentSeconds.ToString("D2"); // Formats the time of both the current time and total time of the song.
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
            updateTimer.Enabled = false; // Will completely disable the display and close it.

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
            t = new System.Timers.Timer(); // Creates a timer in order to show the balloon after 1 second (1000ms).
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
            t.Stop(); // Stops the timer so that we don't keep displaying the balloon.
        }

        private void ShowBalloon()
        {
            string bText = "Artist: " + artist + "\nTitle: " + title + "\nAlbum: " + album; // Displays the balloon with all the values that is also displayed on the keyboard.
            niKodi_MediaKeys.ShowBalloonTip(500, "Current Song", bText, ToolTipIcon.None);
        }

        public void ResetTitleBox()
        {
            tmpLocationX = (int)rTitle.X; // Resets the title (in case the song ends when the text is off-screen)
        }

        public void ResetAllSongInfo()
        {
            album = ""; // Wil reset any values to its defaults
            artist = "";
            cTime = "0";
            tTime = "0";
            title = "";
        }
    }
}