namespace SolidCode.Atlas.Telescope;

#if DEBUG
using System.Net;
using System.Reflection;
using System.Text;
using WebSocketSharp;
using WebSocketSharp.Server;
using System.Text.Json;
using System.Timers;
using System.Collections.Concurrent;
class Log
{
    // ReSharper disable once InconsistentNaming
    public string type { get; set; }
    // ReSharper disable once InconsistentNaming
    public string content { get; set; }

    public Log(string content)
    {
        this.type = "log";

        this.content = content;
    }
}

class ProfilerData
{
    // ReSharper disable once InconsistentNaming
    public string type { get; set; }
    // ReSharper disable once InconsistentNaming
    public Dictionary<string,Dictionary<string,float>> times { get; set; }

    public ProfilerData(Dictionary<string,Dictionary<string,float>> data)
    {
        this.type = "profiler";
        this.times = data;
    }
}

class DebuggerSocketBehaviour : WebSocketBehavior
{
    Timer _timer;
    Timer liveDataTimer;
    Timer profilerDataTimer;
    public ConcurrentQueue<string> queuedLogs = new ConcurrentQueue<string>();
    int id;
    protected override void OnMessage(MessageEventArgs e)
    {
        if (Debug.actions.ContainsKey(e.Data))
        {
            Debug.actions[e.Data].Invoke();
        }
    }
    protected override void OnOpen()
    {
        DebugServer.Connections++;
        id = DebugServer.instance.AddListener(this);

        _timer = new System.Timers.Timer(50);
        _timer.Elapsed += new System.Timers.ElapsedEventHandler(SendLogs);
        _timer.AutoReset = true;
        _timer.Start();

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
        DebugServer.Connections--;
        DebugServer.instance.RemoveListener(id);
        _timer.Stop();
        _timer.Close();
        liveDataTimer.Stop();
        liveDataTimer.Close();
        profilerDataTimer.Stop();
        profilerDataTimer.Close();
    }

    public void SendLiveData(object sender, ElapsedEventArgs e)
    {
        if (this.State != WebSocketState.Open || Debug.LiveData == null)
        {
            return;
        }

        string jsonString = JsonSerializer.Serialize(Debug.LiveData);
        try
        {

            Send(jsonString);
        }
        catch (Exception ex)
        {
            Debug.Error(0, "Error in DebuggerSocketBehaviour: " + ex.ToString());
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
            Dictionary<string,Dictionary<string,float>> times = Profiler.GetAverageTimes();

            string jsonString = JsonSerializer.Serialize(new ProfilerData(times));

            Send(jsonString);
        }
        catch (Exception ex)
        {
            Debug.Error(0, "Error in DebuggerSocketBehaviour: " + ex.ToString());
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
    WebSocketServer? wssv;
    int listenerID = 0;
    public static int Connections = 0;
    ConcurrentDictionary<int, DebuggerSocketBehaviour> listeners = new ConcurrentDictionary<int, DebuggerSocketBehaviour>();
    private void StartWebsocket()
    {
        wssv = new WebSocketServer(8989);
        // This is probably not smart, but the server sometimes outputs errors that don't really affect our app in any way, so lets just ignore them for now :/
        wssv.Log.Output = (_, __) => { };
        wssv.AddWebSocketService<DebuggerSocketBehaviour>("/ws");
        try
        {
            wssv.Start();
        }
        catch (Exception e)
        {
            Debug.Error(0, "Couldn't Start Telescope Server! " + e.ToString());
        }
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
        listeners.AddOrUpdate(id, (index) => listener, (index, debugger) => { Debug.Error(0, "Websocket listener already exists, updating listener instead."); return listener; });
        return id;
    }
    public void RemoveListener(int listener)
    {

        listeners.TryRemove(listener, out _);
    }

    public void Stop()
    {
        wssv?.Stop();
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

#endif