/*
 * Andrew Laychak
 * http://www.alaychak.com/
 * 9/5/2014
 * Logitech G15 Display and Media Keys for Kodi
 */

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

        string processToWatch = "Kodi.exe"; // Name of application that will be watched
        string rpcLocation = "http://192.168.1.9:8282/jsonrpc"; // IP of the computer running Kodi (as well as the port)

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

        private enum MusicKeys { Previous = 177, Next = 176, Stop = 178, PlayPause = 179, VolumeDown = 174, VolumeUp = 175 } // Key input numbers for expanded keyboards

        public frmKodiMediaKeys()
        {
            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException); // Catch specific errors
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            InitializeComponent();

            _rawinput = new RawInput(Handle);
            _rawinput.CaptureOnlyIfTopMostWindow = false;    // Setting that will determine whether to capture keys all the time or just when in focus
            _rawinput.AddMessageFilter();                   // Adding a message filter will cause keypresses to be handled
            _rawinput.KeyPressed += OnKeyPressed;

            //Win32.DeviceAudit();                            // Writes a file DeviceAudit.txt to the current directory

            if (ProcessAlreadyRunning()) // Determines whether or not Kodi is currently running or not. 
            {
                lDisplay.Initialize(niKodi_MediaKeys, pbLCD); // Initializes the keyboard
                eWatcher = WatchForProcessEnd(processToWatch); // Will exit the application when Kodi is exited.
            }
            else
            {
                sWatcher = WatchForProcessStart(processToWatch); // Will start the application when Kodi is started
            }

            EventLogSetup();
        }
        
        void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            string eMessage = e.Exception.Message;
            EventLog.WriteEntry("Application", eMessage); // Writes an entry in the event log with the error.
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            string eMessage = (e.ExceptionObject as Exception).Message;
            EventLog.WriteEntry("Application", eMessage); // Writes an entry in the event log with the error.
        }

        private void OnKeyPressed(object sender, InputEventArg e)
        {
            string vkStatus = e.KeyPressEvent.KeyPressState; // Retrieves information from the user input
            int vkNumber = e.KeyPressEvent.VKey;
            string vkName = e.KeyPressEvent.VKeyName;

            currentKey = vkStatus;
            if (previousKey == "BREAK")
            {
                previousKey = currentKey;
                return;
            }

            if (currentKey == previousKey) // Determiens if a key is being held down.
            {
                switch (vkNumber)
                {
                    case (int)MusicKeys.Previous: // Determines whether or not the rewind or fast-forward a track.
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
                if (!t.Enabled) // Enables the timer to determine whether or not the application should check for a key press.
                {
                    SetTimer(); // Enables the timer. Prevents repeated calls (e.g. calling "Next Track" multiple times, which would skip a few tracks)
                }
                if (canCheck)
                {
                    switch (vkNumber) // Depending on the key pressed, executes an action.
                    {
                        case (int)MusicKeys.Previous:
                            PreviousSong();
                            break;
                        case (int)MusicKeys.Next:
                            NextSong();
                            break;
                        case (int)MusicKeys.Stop:
                            StopSong();
                            break;
                        case (int)MusicKeys.PlayPause:
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
            t.Interval = 500; // Half a second (this is in milliseconds)
            t.Enabled = true;
            t.Start();
            t.Elapsed += new ElapsedEventHandler(FinishedTimer); // Executes the FinishedTimer method when finished
        }

        private void FinishedTimer(object source, ElapsedEventArgs e)
        {
            currentKey = "BREAK"; // Sets some variables back to their defaults, which then allows the application to check for any new key presses.
            previousKey = "BREAK";
            canCheck = true;
            t.Stop();
        }

        private ManagementEventWatcher WatchForProcessStart(string processName)
        {
            // Uses the WMI (Windows Management Instrumentation) to watch for when Kodi is opened.
            // See documentation: https://msdn.microsoft.com/en-us/library/aa394649%28v=vs.85%29.aspx
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
            // Uses the WMI (Windows Management Instrumentation) to watch for when Kodi is closed.
            // See documentation: https://msdn.microsoft.com/en-us/library/aa394650%28v=vs.85%29.aspx
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
            //Console.WriteLine(String.Format("{0} process ended", processName));

            if (processName == processToWatch) // Stops executing the application when Kodi is closed.
            {
                OnStop();
            }
            sWatcher = WatchForProcessStart(processToWatch); // Creates a watcher for Kodi to listen for when Kodi is opened again
        }

        private void ProcessStarted(object sender, EventArrivedEventArgs e)
        {
            ManagementBaseObject targetInstance = (ManagementBaseObject)e.NewEvent.Properties["TargetInstance"].Value;
            string processName = targetInstance.Properties["Name"].Value.ToString();
            Console.WriteLine(String.Format("{0} process started", processName));

            if (processName == processToWatch) // Initializes the keyboard display when Kodi is opened.
            {
                lDisplay.Initialize(niKodi_MediaKeys, pbLCD);

            }
            eWatcher = WatchForProcessEnd(processToWatch); // Creates a watcher to listen for when Kodi is closed.
        }

        private bool ProcessAlreadyRunning()
        {
            // Uses the WMI to check if Kodi is currently running or not
            // See documentation: https://msdn.microsoft.com/en-us/library/aa394372%28v=vs.85%29.aspx
            string query = "SELECT * FROM Win32_Process WHERE Name='" + processToWatch + "'";
            string scope = @"\\.\root\CIMV2";
            var searcher = new ManagementObjectSearcher(scope, query);

            bool isRunning = searcher.Get().Count > 0;

            return isRunning;
        }

        protected void OnStop()
        {
            //eLog.WriteEntry("In OnStop");

            lDisplay.ResetAllSongInfo();

            if (sWatcher != null) // Removes any watchers that are opened when Kodi is closed
            {
                sWatcher.Dispose();
            }
            if (eWatcher != null)
            {
                eWatcher.Dispose();
            }

            lDisplay.Disable();
        }

        private void frmKodiMediaKeys_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (sWatcher != null) // Removes any watchers that are opened and removes the keypress listeners when the media key application is closed (not Kodi)
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
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(rpcLocation); // Creates a RPC that POSTs to the media center server with the "Previous Song" method
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
                    JObject o = JObject.Parse(result); // Parses the returned value into an object. Likely the only one you want to consider using is "Result"
                }
            }
        }

        private void NextSong()
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(rpcLocation); // Creates a RPC that POSTs to the media center server with the "Next Song" method
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
                    JObject o = JObject.Parse(result); // Parses the returned value into an object. Likely the only one you want to consider using is "Result"
                }
            }
        }

        private void StopSong()
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(rpcLocation); // Creates a RPC that POSTs to the media center server with the "Stop Song" method
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
                    JObject o = JObject.Parse(result); // Parses the returned value into an object. Likely the only one you want to consider using is "Result"
                }
            }
        }

        private void PlayPauseSong()
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(rpcLocation); // Creates a RPC that POSTs to the media center server with the "Play/Pause Song" method
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
                    JObject o = JObject.Parse(result); // Parses the returned value into an object. Likely the only one you want to consider using is "Result"
                }
            }
        }

        private void RewindSong()
        {
            // Creates a RPC that POSTs to the media center server with the "Set Speed" method
            // In this I use -2 as a value. This is one of the slowest values in regards to rewinding. Valid values are between negative 32 and postive 32. Remember, positive values are fast-forward.

            isRewinding = true; // Sets this value to true to prevent any further actions while the song is being rewinded
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
                    JObject o = JObject.Parse(result); // Parses the returned value into an object. Likely the only one you want to consider using is "Result"
                }
            }
        }

        private void FastForwardSong()
        {
            // Creates a RPC that POSTs to the media center server with the "Set Speed" method
            // In this I use -2 as a value. This is one of the slowest values in regards to rewinding. Valid values are between negative 32 and postive 32. Remember, positive values are fast-forward.

            isFastForwarding = true; // Sets this value to true to prevent any further actions while the song is being rewinded
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
                    JObject o = JObject.Parse(result); // Parses the returned value into an object. Likely the only one you want to consider using is "Result"
                }
            }
        }

        private void niKodi_MediaKeys_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Show(); // Shows the form when the icon is double clicked. This displays the same information as on the keyboard.
            this.WindowState = FormWindowState.Normal;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit(); // Exits the application when the user right-clicks the icon and clicks the 'Exit' button
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