using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Timers;
using WebSocketSharp;
using WebSocketSharp.Server;
using Timer = System.Timers.Timer;

namespace SolidCode.Atlas.Telescope;

#if DEBUG

internal class Log
{
    public Log(string content)
    {
        type = "log";

        this.content = content;
    }

    // ReSharper disable once InconsistentNaming
    public string type { get; set; }

    // ReSharper disable once InconsistentNaming
    public string content { get; set; }
}

internal class ProfilerData
{
    public ProfilerData(Dictionary<string, Dictionary<string, float>> data)
    {
        type = "profiler";
        times = data;
    }

    // ReSharper disable once InconsistentNaming
    public string type { get; set; }

    // ReSharper disable once InconsistentNaming
    public Dictionary<string, Dictionary<string, float>> times { get; set; }
}

internal class DebuggerSocketBehaviour : WebSocketBehavior
{
    private Timer _timer;
    private int id;
    private Timer liveDataTimer;
    private Timer profilerDataTimer;
    public ConcurrentQueue<string> queuedLogs = new();

    protected override void OnMessage(MessageEventArgs e)
    {
        if (Debug.actions.ContainsKey(e.Data)) Debug.actions[e.Data].Invoke();
    }

    protected override void OnOpen()
    {
        DebugServer.Connections++;
        id = DebugServer.instance.AddListener(this);

        _timer = new Timer(50);
        _timer.Elapsed += SendLogs;
        _timer.AutoReset = true;
        _timer.Start();

        liveDataTimer = new Timer(200);
        liveDataTimer.Elapsed += SendLiveData;
        liveDataTimer.AutoReset = true;
        liveDataTimer.Start();

        profilerDataTimer = new Timer(250);
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
        if (State != WebSocketState.Open || Debug.LiveData == null) return;

        var jsonString = JsonSerializer.Serialize(Debug.LiveData);
        try
        {
            Send(jsonString);
        }
        catch (Exception ex)
        {
            Debug.Error(0, "Error in DebuggerSocketBehaviour: " + ex);
        }
    }


    public void SendProfilerData(object sender, ElapsedEventArgs e)
    {
        if (State != WebSocketState.Open) return;
        try
        {
            var times = Profiler.GetAverageTimes();

            var jsonString = JsonSerializer.Serialize(new ProfilerData(times));

            Send(jsonString);
        }
        catch (Exception ex)
        {
            Debug.Error(0, "Error in DebuggerSocketBehaviour: " + ex);
        }
    }

    public void SendLogs(object sender, ElapsedEventArgs e)
    {
        if (State != WebSocketState.Open) return;
        while (queuedLogs.Count > 0)
        {
            string? log = null;
            queuedLogs.TryDequeue(out log);
            if (log != null)
            {
                var jsonString = JsonSerializer.Serialize(new Log(log));
                try
                {
                    if (State != WebSocketState.Open) return;
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

internal class DebugServer
{
    public static DebugServer instance;
    public static int Connections;
    private readonly HttpListener _listener;
    private readonly byte[] bytesToSend = new byte[0];
    private readonly ConcurrentDictionary<int, DebuggerSocketBehaviour> listeners = new();
    private int listenerID;
    public int Port = 8787;
    private WebSocketServer? wssv;

    public DebugServer()
    {
        _listener = new HttpListener();
        instance = this;
        _listener.Prefixes.Add("http://127.0.0.1:" + Port + "/");
        _listener.Start();
        var assembly = Assembly.GetExecutingAssembly();
        using (var stream =
               assembly.GetManifestResourceStream(assembly.GetManifestResourceNames()
                   .Single(str => str.EndsWith("LogViewer.html"))))
        using (var reader = new StreamReader(stream))
        {
            var text = reader.ReadToEnd();
            bytesToSend = Encoding.UTF8.GetBytes(text);
        }

        var t = new Thread(StartWebsocket);
        t.Name = "Telescope Server";
        t.Start();

        Receive();
    }

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
            Debug.Error(0, "Couldn't Start Telescope Server! " + e);
        }
    }


    public void Log(string log)
    {
        var curlisteners = listeners.Values.ToArray();
        foreach (var item in curlisteners) item.queuedLogs.Enqueue(log);
    }


    public int AddListener(DebuggerSocketBehaviour listener)
    {
        var id = Interlocked.Increment(ref listenerID);
        listeners.AddOrUpdate(id, index => listener, (index, debugger) =>
        {
            Debug.Error(0, "Websocket listener already exists, updating listener instead.");
            return listener;
        });
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
        _listener.BeginGetContext(ListenerCallback, _listener);
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