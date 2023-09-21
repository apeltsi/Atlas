using System.Diagnostics;
using SolidCode.Atlas.Rendering;
using SolidCode.Atlas.Telescope;

namespace SolidCode.Atlas.ECS;

/// <summary>
/// Manages multiple tick threads, specified in the FrameworkConfiguration.
/// </summary>
public static class TickManager
{
    private static TickThread[] _threads = Array.Empty<TickThread>();
    private static bool _isRunning;

    internal static void Initialize()
    {
        _isRunning = true;
        var threads = new List<TickThread>();
        var threadsToStart = new List<Thread>();
        foreach (var conf in Atlas.Configuration.ECS.Threads)
        {
            var tt = TickThread.FromSettings(conf);
            threads.Add(tt);
            var t = new Thread(() => InitializeTickThread(tt));
            t.Name = "TickThread_" + tt.Name;
            threadsToStart.Add(t);
        }

        _threads = threads.ToArray();
        foreach (var t in threadsToStart) t.Start();
    }

    private static void InitializeTickThread(TickThread t)
    {
        Debug.Log(LogCategory.Framework, $"Starting tick thread '{t.Name}' with a frequency of " + t.Frequency);
        var updateDuration = new Stopwatch();
        while (_isRunning)
        {
            if (t.Frequency == 0 && EntityComponentSystem.HasStarted)
            {
                Thread.Sleep(10);
                continue;
            }

            updateDuration.Restart();
            t.RunTick();
            updateDuration.Stop();
            if (t.Frequency == 0)
                continue;
            var delay = 1000 / t.Frequency - (int)Math.Ceiling(updateDuration.Elapsed.TotalMilliseconds);
            if (delay > 0) Thread.Sleep(delay);
        }
    }

    internal static double GetThreadTime()
    {
        var threadName = DetermineThreadName();
        foreach (var t in _threads)
            if (t.Name == threadName)
                return t.Time;

        return 0.0;
    }

    internal static double GetThreadDelta()
    {
        var threadName = DetermineThreadName();
        foreach (var t in _threads)
            if (t.Name == threadName)
                return t.DeltaTime;

        return 0.0;
    }

    private static string DetermineThreadName()
    {
        // We're going to determine the name of the current thread by looking at the thread name
        // If the thread name is "TickThread_Main", we're going to return "Main"

        var name = Thread.CurrentThread.Name ?? "";
        if (name.StartsWith("TickThread_"))
            name = name.Substring(11);
        else
            return "Main";

        return name;
    }

    internal static bool ThreadIsSynced(string name)
    {
        foreach (var t in _threads)
            if (t.Name == name)
                return t.Sync;

        return false;
    }


    internal static void Dispose()
    {
        _isRunning = false;

        _threads = Array.Empty<TickThread>();
    }

    /// <summary>
    /// Sets the frequency of a Tick Thread
    /// </summary>
    /// <param name="thread">The Name of the thread to update</param>
    /// <param name="frequency">The frequency of the thread</param>
    public static void SetTickFrequency(string thread, int frequency)
    {
        foreach (var t in _threads)
            if (t.Name == thread)
                t.Frequency = frequency;
    }

    /// <summary>
    /// Sets the frequency of the Main Tick Thread
    /// </summary>
    /// <param name="frequency">The frequency of the Main Tick Thread</param>
    public static void SetTickFrequency(int frequency)
    {
        SetTickFrequency("Main", frequency);
    }


    private class TickThread
    {
        private static int _lastDataUpdate;

        /// <summary>
        /// Time elapsed between ticks, in seconds.
        /// </summary>
        private readonly Stopwatch _tickCounterStopwatch = new();

        private readonly Stopwatch _tickDeltaStopwatch = new();
        public readonly string Name;
        public readonly bool Sync;
        private int _ticksThisSecond;
        public double DeltaTime;
        public int Frequency;
        public double Time;

        public TickThread(string name, int frequency, bool sync)
        {
            Name = name;
            Frequency = frequency;
            Sync = sync;
        }

        public int TicksPerSecond { get; private set; }

        public static TickThread FromSettings(ECSThreadSettings settings)
        {
            return new TickThread(settings.Name, settings.Frequency, settings.Sync);
        }

        public void RunTick()
        {
            _tickDeltaStopwatch.Stop();
            DeltaTime = _tickDeltaStopwatch.Elapsed.TotalSeconds;
            Time = Atlas.ECSStopwatch!.Elapsed.TotalSeconds;
            if (!_tickCounterStopwatch.IsRunning) _tickCounterStopwatch.Start();
            if (_tickCounterStopwatch.ElapsedMilliseconds >= 1000)
            {
                TicksPerSecond = _ticksThisSecond;
                _ticksThisSecond = 0;
                _tickCounterStopwatch.Restart();
            }

            var sw = new Stopwatch();
            sw.Start();
            _ticksThisSecond++;
            _tickDeltaStopwatch.Restart();

#if DEBUG
            if (Name == "Main")
                Profiler.StartTimer(Profiler.TickType.Tick);
#endif
            if (Sync)
            {
                var t = TickScheduler.RequestTick();
                t.Wait();
            }
#if DEBUG
            if (Name == "Main")
                Profiler.EndTimer(Profiler.TickType.Tick, "Wait");
#endif

            if (!Atlas.ECSStopwatch.IsRunning) Atlas.ECSStopwatch.Start();

            EntityComponentSystem.Tick(Name);
#if DEBUG
            if (Name == "Main")
            {
                _lastDataUpdate++;
                if (_lastDataUpdate > 50)
                {
                    _lastDataUpdate = 0;
                    var data = new string[3 + _threads.Length];
                    data[0] = "FPS: " + Math.Round(Window.AverageFramerate) + "/" +
                              (Window.MaxFramerate == 0 ? "VSYNC" : Window.MaxFramerate);
                    var index = 1;
                    for (var i = 0; i < _threads.Length; i++)
                    {
                        var tt = _threads[i];
                        data[index] = tt.Name + "-TPS: " + tt.TicksPerSecond + "/" + tt.Frequency;
                        index += 1;
                    }

                    data[index] = "Runtime: " + Atlas.GetTotalUptime().ToString("0.0") + "s";
                    data[index + 1] = Atlas.Version;

                    Telescope.Debug.LiveData = new LiveData(data, EntityComponentSystem.GetECSHierarchy());
                }

                Profiler.SubmitTimes(Profiler.TickType.Tick);
            }
#endif
            if (Sync)
                TickScheduler.FreeThreads();
            sw.Stop();
        }
    }
}