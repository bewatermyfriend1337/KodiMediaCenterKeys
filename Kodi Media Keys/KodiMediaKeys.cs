using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Drawing;
using System.IO;
using System.Net;
using System.Management;
using RawInput_dll;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Timers;
using System.Threading;

namespace Kodi_Media_Keys
{
    public partial class frmKodiMediaKeys : Form
    {
        //Watchers
        ManagementEventWatcher sWatcher;
        ManagementEventWatcher eWatcher;

        string processToWatch = "Kodi.exe";
        string rpcLocation = "http://192.168.1.9:8282/jsonrpc";

        //Display
        LogitechDisplay lDisplay = new LogitechDisplay();

        //Keyboard Input
        string previousKey = "BREAK";
        string currentKey = "BREAK";
        bool canCheck = true;
        bool isRewinding = false;
        bool isFastForwarding = false;
        System.Timers.Timer t = new System.Timers.Timer();
        private readonly RawInput _rawinput;

        //Event Log
        string sSource = "Kodi Media Keys";
        string sLog = "Kodi Media Keys";

        private enum MusicKeys { Previous = 177, Next = 176, Stop = 178, PlayPause = 179, VolumeDown = 174, VolumeUp = 175 }

        public frmKodiMediaKeys()
        {
            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            InitializeComponent();

            _rawinput = new RawInput(Handle);
            _rawinput.CaptureOnlyIfTopMostWindow = false;    // Otherwise default behavior is to capture always
            _rawinput.AddMessageFilter();                   // Adding a message filter will cause keypresses to be handled
            _rawinput.KeyPressed += OnKeyPressed;

            //Win32.DeviceAudit();                            // Writes a file DeviceAudit.txt to the current directory

            if (ProcessAlreadyRunning())
            {
                lDisplay.Initialize(niKodi_MediaKeys, pbLCD);
                eWatcher = WatchForProcessEnd(processToWatch);
            }
            else
            {
                sWatcher = WatchForProcessStart(processToWatch);
            }

            EventLogSetup();
        }

            void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            string eMessage = e.Exception.Message;
            EventLog.WriteEntry("Application", eMessage);
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            string eMessage = (e.ExceptionObject as Exception).Message;
            EventLog.WriteEntry("Application", eMessage);
        }

        private void OnKeyPressed(object sender, InputEventArg e)
        {
            string vkStatus = e.KeyPressEvent.KeyPressState;
            int vkNumber = e.KeyPressEvent.VKey;
            string vkName = e.KeyPressEvent.VKeyName;

            currentKey = vkStatus;
            if (previousKey == "BREAK")
            {
                previousKey = currentKey;
                return;
            }
            if (currentKey == previousKey)
            {
                switch (vkNumber)
                {
                    case (int)MusicKeys.Previous:
                        if (!isRewinding)
                        {
                            RewindSong();
                        }
                        break;
                    case (int)MusicKeys.Next:
                        if (!isFastForwarding)
                        {
                            FastForwardSong();
                        }
                        break;
                }
                canCheck = false;
            }
            else
            {
                if (!t.Enabled)
                {
                    SetTimer();
                }
                if (canCheck)
                {
                    switch (vkNumber)
                    {
                        case (int)MusicKeys.Previous:
                            Console.WriteLine("PRESSED PREVIOUS KEY");
                            PreviousSong();
                            break;
                        case (int)MusicKeys.Next:
                            Console.WriteLine("PRESSED NEXT KEY");
                            NextSong();
                            break;
                        case (int)MusicKeys.Stop:
                            Console.WriteLine("PRESSED STOP KEY");
                            StopSong();
                            break;
                        case (int)MusicKeys.PlayPause:
                            Console.WriteLine("PRESSED PLAY/PAUSE KEY");
                            PlayPauseSong();
                            break;
                    }
                    isRewinding = false;
                    isFastForwarding = false;
                    previousKey = "BREAK";
                }
            }
        }

        void SetTimer()
        {
            t.AutoReset = false;
            t.Interval = 500;
            t.Enabled = true;
            t.Start();
            t.Elapsed += new ElapsedEventHandler(FinishedTimer);
        }

        private void FinishedTimer(object source, ElapsedEventArgs e)
        {
            currentKey = "BREAK";
            previousKey = "BREAK";
            canCheck = true;
            t.Stop();
        }

        private ManagementEventWatcher WatchForProcessStart(string processName)
        {
            string query = @"SELECT TargetInstance FROM __InstanceCreationEvent WITHIN 5 WHERE TargetInstance ISA 'Win32_Process'  AND TargetInstance.Name = '" + processName + "'";
            string scope = @"\\.\root\CIMV2";

            // Create a watcher and listen for events
            ManagementEventWatcher watcher = new ManagementEventWatcher(scope, query);
            watcher.EventArrived += ProcessStarted;
            watcher.Start();
            return watcher;
        }

        private ManagementEventWatcher WatchForProcessEnd(string processName)
        {
            string query = @"SELECT TargetInstance FROM __InstanceDeletionEvent WITHIN 5 WHERE TargetInstance ISA 'Win32_Process' AND TargetInstance.Name = '" + processName + "'";
            string scope = @"\\.\root\CIMV2";

            // Create a watcher and listen for events
            ManagementEventWatcher watcher = new ManagementEventWatcher(scope, query);
            watcher.EventArrived += ProcessEnded;
            watcher.Start();
            return watcher;
        }

        private void ProcessEnded(object sender, EventArrivedEventArgs e)
        {
            ManagementBaseObject targetInstance = (ManagementBaseObject)e.NewEvent.Properties["TargetInstance"].Value;
            string processName = targetInstance.Properties["Name"].Value.ToString();
            Console.WriteLine(String.Format("{0} process ended", processName));

            if (processName == processToWatch)
            {
                OnStop();
            }
            sWatcher = WatchForProcessStart(processToWatch);
        }

        private void ProcessStarted(object sender, EventArrivedEventArgs e)
        {
            ManagementBaseObject targetInstance = (ManagementBaseObject)e.NewEvent.Properties["TargetInstance"].Value;
            string processName = targetInstance.Properties["Name"].Value.ToString();
            Console.WriteLine(String.Format("{0} process started", processName));

            if (processName == processToWatch)
            {
                lDisplay.Initialize(niKodi_MediaKeys, pbLCD);

            }
            eWatcher = WatchForProcessEnd(processToWatch);
        }

        private bool ProcessAlreadyRunning()
        {
            string query = "SELECT * FROM Win32_Process WHERE Name='" + processToWatch + "'";
            string scope = @"\\.\root\CIMV2";
            var searcher = new ManagementObjectSearcher(scope, query);

            bool isRunning = searcher.Get().Count > 0;

            return isRunning;
        }

        protected void OnStop()
        {
            //eLog.WriteEntry("In OnStop");

            sWatcher.Dispose();
            eWatcher.Dispose();

            lDisplay.Disable();
        }

        private void frmKodiMediaKeys_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (sWatcher != null)
            {
                sWatcher.Dispose();
            }
            if (eWatcher != null)
            {
                eWatcher.Dispose();
            }

            _rawinput.KeyPressed -= OnKeyPressed;
        }

        private void PreviousSong()
        {
            //{"jsonrpc": "2.0", "method": "Player.GoTo", "params": { "playerid": 0, "to": "previous" }, "id": 1}
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(rpcLocation);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                string json = "{\"jsonrpc\": \"2.0\", \"method\": \"Player.GoTo\", \"params\": { \"playerid\": 0, \"to\": \"previous\" }, \"id\": 1}";

                streamWriter.Write(json);
                streamWriter.Flush();
                streamWriter.Close();

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                    var results = JsonConvert.DeserializeObject<dynamic>(result);
                    JObject o = JObject.Parse(result);
                }
            }
        }

        private void NextSong()
        {
            //{"jsonrpc": "2.0", "method": "Player.GoTo", "params": { "playerid": 0, "to": "next" }, "id": 1}
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(rpcLocation);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                string json = "{\"jsonrpc\": \"2.0\", \"method\": \"Player.GoTo\", \"params\": { \"playerid\": 0, \"to\": \"next\" }, \"id\": 1}";

                streamWriter.Write(json);
                streamWriter.Flush();
                streamWriter.Close();

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                    var results = JsonConvert.DeserializeObject<dynamic>(result);
                    JObject o = JObject.Parse(result);
                }
            }
        }

        private void StopSong()
        {
            //{"jsonrpc": "2.0", "method": "Player.Stop", "params": { "playerid": 0 }, "id": 1}
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(rpcLocation);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                string json = "{\"jsonrpc\": \"2.0\", \"method\": \"Player.Stop\", \"params\": { \"playerid\": 0 }, \"id\": 1}";

                streamWriter.Write(json);
                streamWriter.Flush();
                streamWriter.Close();

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                    var results = JsonConvert.DeserializeObject<dynamic>(result);
                    JObject o = JObject.Parse(result);
                }
            }
        }

        private void PlayPauseSong()
        {
            //{"jsonrpc": "2.0", "method": "Player.PlayPause", "params": { "playerid": 0 }, "id": 1}
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(rpcLocation);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                string json = "{\"jsonrpc\": \"2.0\", \"method\": \"Player.PlayPause\", \"params\": { \"playerid\": 0 }, \"id\": 1}";

                streamWriter.Write(json);
                streamWriter.Flush();
                streamWriter.Close();

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                    var results = JsonConvert.DeserializeObject<dynamic>(result);
                    JObject o = JObject.Parse(result);
                }
            }
        }

        private void RewindSong()
        {
            isRewinding = true;
            //{"jsonrpc":"2.0","method":"Player.SetSpeed","params":{"playerid":0,"speed":-2 },"id":1}
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(rpcLocation);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                string json = "{\"jsonrpc\":\"2.0\",\"method\":\"Player.SetSpeed\",\"params\":{\"playerid\":0,\"speed\":-2 },\"id\":1}";

                streamWriter.Write(json);
                streamWriter.Flush();
                streamWriter.Close();

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                    var results = JsonConvert.DeserializeObject<dynamic>(result);
                    JObject o = JObject.Parse(result);
                }
            }
        }

        private void FastForwardSong()
        {
            isFastForwarding = true;
            //{"jsonrpc":"2.0","method":"Player.SetSpeed","params":{"playerid":0,"speed":2 },"id":1}
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(rpcLocation);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                string json = "{\"jsonrpc\":\"2.0\",\"method\":\"Player.SetSpeed\",\"params\":{\"playerid\":0,\"speed\":2 },\"id\":1}";

                streamWriter.Write(json);
                streamWriter.Flush();
                streamWriter.Close();

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                    var results = JsonConvert.DeserializeObject<dynamic>(result);
                    JObject o = JObject.Parse(result);
                }
            }
        }

        private void niKodi_MediaKeys_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Show();
            this.WindowState = FormWindowState.Normal;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        #region Event Log
        private void EventLogSetup()
        {
            string sEvent = "Initialized";

            if (!EventLog.SourceExists(sSource))
            {
                EventLog.CreateEventSource(sSource, sLog);
            }

            EventLog.WriteEntry(sSource, sEvent, EventLogEntryType.Information, 1);
        }
        #endregion
    }
}
