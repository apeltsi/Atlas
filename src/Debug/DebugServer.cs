#if DEBUG
namespace SolidCode.Caerus
{

    using System.Net;
    using System.Reflection;
    using System.Text;
    using WebSocketSharp;
    using WebSocketSharp.Server;
    using System.Text.Json;
    using System.Timers;
    using SolidCode.Caerus.Rendering;

    class Log
    {
        public string type { get; set; }
        public string content { get; set; }

        public Log(string content)
        {
            this.type = "log";

            this.content = content;
        }
    }
    class LiveData
    {
        public string type { get; set; }
        public int framerate { get; set; }
        public float runtime { get; set; }

        public LiveData(int framerate, float runtime)
        {
            this.type = "livedata";
            this.framerate = framerate;
            this.runtime = runtime;
        }
    }
    class ProfilerData
    {
        public string type { get; set; }
        public float[] times { get; set; }

        public ProfilerData(float[] data)
        {
            this.type = "profiler";
            this.times = data;
        }
    }

    class Behaviour : WebSocketBehavior
    {
        System.Timers.Timer timer;
        System.Timers.Timer liveDataTimer;

        bool closed = false;
        bool sendingLogs = false;
        protected override void OnMessage(MessageEventArgs e)
        {
            if (e.Data == "showwindow")
            {
                Window.MoveToFront();
            }
            if (e.Data == "quit")
            {
                Debug.Log("Remotely shutting down");
                Window.window.Close();
            }
            if (e.Data == "reloadshaders")
            {
                Window.reloadShaders = true;
            }
        }
        protected override void OnOpen()
        {
            Debug.Log("Socket opened");
            DebugServer.instance.AddListener();

            timer = new System.Timers.Timer(50);
            timer.Elapsed += new System.Timers.ElapsedEventHandler(SendLogs);
            timer.AutoReset = true;
            timer.Start();

            liveDataTimer = new System.Timers.Timer(1000);
            liveDataTimer.Elapsed += new System.Timers.ElapsedEventHandler(SendLiveData);
            liveDataTimer.AutoReset = true;
            liveDataTimer.Start();

        }


        protected override void OnClose(CloseEventArgs e)
        {
            closed = true;
            Debug.Log("Closing websocket");
            DebugServer.instance.RemoveListener();
            timer.Stop();
            timer.Close();
            liveDataTimer.Stop();
            liveDataTimer.Close();
            Debug.Log("Socket closed");
        }

        public void SendLiveData(object sender, ElapsedEventArgs e)
        {
            if (closed)
            {
                return;
            }

            string jsonString = JsonSerializer.Serialize(new LiveData((int)Math.Round(Window.AverageFramerate), Caerus.GetUptime() * 1000));

            Send(jsonString);
            float[] times = Profiler.GetAverageTimes();

            jsonString = JsonSerializer.Serialize(new ProfilerData(times));

            Send(jsonString);
        }

        public void SendLogs(object sender, ElapsedEventArgs e)
        {
            if (sendingLogs)
            {
                return;
            }
            sendingLogs = true;
            List<QueuedLog> queue = new List<QueuedLog>(DebugServer.instance.queuedLogs);
            foreach (QueuedLog log in queue)
            {
                if (closed)
                {
                    return;
                }
                string jsonString = JsonSerializer.Serialize(new Log(log.log));

                try
                {
                    Send(jsonString);

                    DebugServer.instance.queuedLogs[DebugServer.instance.queuedLogs.IndexOf(log)].listeners += 1;
                }
                catch (Exception ex)
                {
                    // Log probably got removed already
                }
            }
            DebugServer.instance.RemoveDuplicates();
            sendingLogs = false;
        }

    }
    class QueuedLog
    {
        public string log;
        public int listeners;
    }
    class DebugServer
    {
        public int Port = 8787;

        private HttpListener _listener;
        private byte[] bytesToSend = new byte[0];
        public delegate void LogListener(string log);
        public static DebugServer instance;
        public List<QueuedLog> queuedLogs = new List<QueuedLog>();
        int listeners = 0;
        bool locked = false;
        WebSocketServer wssv;
        private void StartWebsocket()
        {
            wssv = new WebSocketServer(8989);

            wssv.AddWebSocketService<Behaviour>("/ws");
            wssv.Start();
            Debug.Log("Live Debug is now active on port " + Port.ToString());
        }
        public DebugServer()
        {
            _listener = new HttpListener();
            instance = this;
            _listener.Prefixes.Add("http://127.0.0.1:" + Port.ToString() + "/");
            _listener.Start();
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(assembly.GetManifestResourceNames().Single(str => str.EndsWith("LogViewer.html"))))
            using (StreamReader reader = new StreamReader(stream))
            {
                string text = reader.ReadToEnd();
                bytesToSend = Encoding.UTF8.GetBytes(text);
            }

            Thread t = new Thread(new ThreadStart(StartWebsocket));
            t.Start();

            Receive();
        }


        public void Log(string log)
        {
            while (locked)
            {
                Thread.Sleep(20);
            }
            queuedLogs.Add(new QueuedLog { log = log, listeners = 0 });
        }

        public void RemoveDuplicates()
        {
            List<QueuedLog> removeList = new List<QueuedLog>();
            locked = true;
            List<QueuedLog> logs = new List<QueuedLog>(queuedLogs);
            foreach (QueuedLog item in logs)
            {
                if (item.listeners >= listeners)
                {
                    removeList.Add(item);
                }
            }
            foreach (QueuedLog item in removeList)
            {
                queuedLogs.Remove(item);
            }
            locked = false;

        }

        public void AddListener()
        {
            listeners++;
        }
        public void RemoveListener()
        {
            listeners--;
            if (listeners < 0)
            {
                listeners = 0;
            }
        }

        public void Stop()
        {
            wssv.Stop();
            _listener.Stop();
        }

        private void Receive()
        {
            _listener.BeginGetContext(new AsyncCallback(ListenerCallback), _listener);
        }

        private void ListenerCallback(IAsyncResult result)
        {
            try
            {


                if (_listener.IsListening)
                {
                    var context = _listener.EndGetContext(result);
                    var request = context.Request;
                    var response = context.Response;
                    response.ContentType = "text/html; charset=UTF-8";
                    response.StatusCode = (int)HttpStatusCode.OK;

                    response.OutputStream.Write(bytesToSend, 0, bytesToSend.Length);
                    response.OutputStream.Close();


                    Receive();
                }
            }
            catch (Exception e)
            {

            }
        }
    }
}
#endif