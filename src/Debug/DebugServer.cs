#if DEBUG
namespace SolidCode.Atlas
{

    using System.Net;
    using System.Reflection;
    using System.Text;
    using WebSocketSharp;
    using WebSocketSharp.Server;
    using System.Text.Json;
    using System.Timers;
    using SolidCode.Atlas.Rendering;
    using System.Collections.Concurrent;
    using static SolidCode.Atlas.ECS.EntityComponentSystem;

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
        public int updateRate { get; set; }
        public float runtime { get; set; }
        public ECSElement hierarchy { get; set; }

        public LiveData(int framerate, float runtime, ECSElement hierarchy, int updateRate)
        {
            this.type = "livedata";
            this.framerate = framerate;
            this.runtime = runtime;
            this.hierarchy = hierarchy;
            this.updateRate = updateRate;
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

    class DebuggerSocketBehaviour : WebSocketBehavior
    {
        System.Timers.Timer timer;
        System.Timers.Timer liveDataTimer;
        System.Timers.Timer profilerDataTimer;
        public ConcurrentQueue<string> queuedLogs = new ConcurrentQueue<string>();
        int id;
        protected override void OnMessage(MessageEventArgs e)
        {
            if (e.Data == "showwindow")
            {
                Window.MoveToFront();
            }
            if (e.Data == "quit")
            {
                Debug.Log("Remotely shutting down");
                Window.Close();
            }
            if (e.Data == "reloadshaders")
            {
                Window.reloadShaders = true;
            }
        }
        protected override void OnOpen()
        {
            id = DebugServer.instance.AddListener(this);

            timer = new System.Timers.Timer(50);
            timer.Elapsed += new System.Timers.ElapsedEventHandler(SendLogs);
            timer.AutoReset = true;
            timer.Start();

            liveDataTimer = new System.Timers.Timer(200);
            liveDataTimer.Elapsed += new System.Timers.ElapsedEventHandler(SendLiveData);
            liveDataTimer.AutoReset = true;
            liveDataTimer.Start();

            profilerDataTimer = new System.Timers.Timer(250);
            profilerDataTimer.Elapsed += SendProfilerData;
            profilerDataTimer.AutoReset = true;
            profilerDataTimer.Start();

        }


        protected override void OnClose(CloseEventArgs e)
        {
            DebugServer.instance.RemoveListener(id);
            timer.Stop();
            timer.Close();
            liveDataTimer.Stop();
            liveDataTimer.Close();
            profilerDataTimer.Stop();
            profilerDataTimer.Close();
        }

        public void SendLiveData(object sender, ElapsedEventArgs e)
        {
            if (this.State != WebSocketState.Open)
            {
                return;
            }

            string jsonString = JsonSerializer.Serialize(new LiveData((int)Math.Round(Window.AverageFramerate), Atlas.GetUptime() * 1000, GetECSHierarchy(), Atlas.TicksPerSecond));
            try
            {

                Send(jsonString);
            }
            catch (Exception ex)
            {
                Debug.Error("Error in DebuggerSocketBehaviour: " + ex.ToString());
            }
        }


        public void SendProfilerData(object sender, ElapsedEventArgs e)
        {
            if (this.State != WebSocketState.Open)
            {
                return;
            }
            try
            {
                float[] times = Profiler.GetAverageTimes();

                string jsonString = JsonSerializer.Serialize(new ProfilerData(times));

                Send(jsonString);
            }
            catch (Exception ex)
            {
                Debug.Error("Error in DebuggerSocketBehaviour: " + ex.ToString());
            }
        }

        public void SendLogs(object sender, ElapsedEventArgs e)
        {
            if (this.State != WebSocketState.Open) return;
            while (queuedLogs.Count > 0)
            {
                string? log = null;
                queuedLogs.TryDequeue(out log);
                if (log != null)
                {
                    string jsonString = JsonSerializer.Serialize(new Log(log));
                    try
                    {
                        if (this.State != WebSocketState.Open) return;
                        Send(jsonString);
                    }
                    catch (Exception ex)
                    {
                        // Log probably got removed already
                    }
                }
            }
        }

    }
    class DebugServer
    {
        public int Port = 8787;
        private HttpListener _listener;
        private byte[] bytesToSend = new byte[0];
        public static DebugServer instance;
        WebSocketServer wssv;
        int listenerID = 0;
        ConcurrentDictionary<int, DebuggerSocketBehaviour> listeners = new ConcurrentDictionary<int, DebuggerSocketBehaviour>();
        private void StartWebsocket()
        {
            wssv = new WebSocketServer(8989);
            // This is probably not smart, but the server sometimes outputs errors that don't really affect our app in any way, so lets just ignore them for now :/
            wssv.Log.Output = (_, __) => { };
            wssv.AddWebSocketService<DebuggerSocketBehaviour>("/ws");
            wssv.Start();
            Debug.Log("Telescope is now active on port " + Port.ToString());
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
            t.Name = "Telescope Server";
            t.Start();

            Receive();
        }


        public void Log(string log)
        {
            DebuggerSocketBehaviour[] curlisteners = listeners.Values.ToArray();
            foreach (DebuggerSocketBehaviour item in curlisteners)
            {
                item.queuedLogs.Enqueue(log);
            }
        }


        public int AddListener(DebuggerSocketBehaviour listener)
        {
            int id = Interlocked.Increment(ref listenerID);
            listeners.AddOrUpdate(id, (index) => listener, (index, debugger) => { Debug.Error("Websocket listener already exists, updating listener instead."); return listener; });
            return id;
        }
        public void RemoveListener(int listener)
        {

            listeners.TryRemove(listener, out _);
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