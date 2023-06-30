using System.Diagnostics;
#if Windows
using SolidCode.Atlas.Rendering.Windows;
#endif
namespace SolidCode.Atlas
{
    using System.Timers;
    using SolidCode.Atlas.Rendering;
    using SolidCode.Atlas.ECS;
    using SolidCode.Atlas.Components;
    using Veldrid.Sdl2;
    using SolidCode.Atlas.AssetManagement;
    using SolidCode.Atlas.Audio;
    using SolidCode.Atlas.Telescope;

    public enum LogCategory
    {
        General,
        Framework,
        Rendering,
        ECS
    }

    public enum DebuggingMode
    {
        Auto,
        Disabled
    }

    public static class Atlas
    {
        public static string ActiveDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly()?.Location) ?? "";
        public static string DataDirectory = Path.Join(ActiveDirectory, "data" + Path.DirectorySeparatorChar);
        public static string ShaderDirectory = Path.Join(DataDirectory, "shaders" + Path.DirectorySeparatorChar);

        public static string AssetsDirectory = Path.Join(DataDirectory, "assets" + Path.DirectorySeparatorChar);
        public static string AssetPackDirectory = Path.Join(ActiveDirectory, "assets" + Path.DirectorySeparatorChar);
        public const string Version = "iced-coffee@1.0-pre.3";
        public static int TickFrequency = 100;
        public static Timer? timer;
        internal static System.Diagnostics.Stopwatch? primaryStopwatch { get; private set; }
        internal static Stopwatch? ecsStopwatch { get; private set; }

        static Window? _w;
        static bool _doTick = true;
        private static DebuggingMode mode = DebuggingMode.Auto;

        public static void DisableMultiProcessDebugging()
        {
            if (Debug.LogsInitialized && mode == DebuggingMode.Auto)
                throw new NotSupportedException("Multi-Process Debugging can only be disabled before any logs have been printed.");
            mode = DebuggingMode.Disabled;
        }
        internal static void InitializeLogging()
        {
            if(mode != DebuggingMode.Disabled)
                Telescope.Debug.UseMultiProcessDebugging(Atlas.Version);
            Telescope.Debug.StartLogs(new string[] { "General", "Framework", "Rendering", "ECS" });
            Telescope.Debug.RegisterTelescopeAction("showwindow", ShowWindow);
            Telescope.Debug.RegisterTelescopeAction("quit", Quit);
        }
        private static void ShowWindow()
        {
            Window.MoveToFront();
        }
        public static void Quit()
        {
            Window.Close();
        }

        public static bool StartupArgumentExists(string argument)
        {
            string[] args = Environment.GetCommandLineArgs();
            foreach (var arg in args)
            {
                if (arg.ToLower() == argument.ToLower())
                    return true;
            }

            return false;
        }

        public static void StartCoreFeatures(string windowTitle, SDL_WindowFlags flags = 0)
        {
            Debug.CheckLog();
            #if Windows
            ForceHighPerformance.InitializeDedicatedGraphics();
            #endif
            primaryStopwatch = System.Diagnostics.Stopwatch.StartNew();
            ecsStopwatch = new System.Diagnostics.Stopwatch();
            Debug.Log(LogCategory.Framework, "Atlas/" + Version + " starting up...");
            Audio.Audio.InitializeAudio();
            AssetManager.LoadAssetMap();
#if DEBUG
            if (Directory.Exists("./data/shaders"))
            {
                // Were running in from the development path. Lets grab our shaders from there instead!
                ShaderDirectory = "./data/shaders";
                Debug.Log(LogCategory.Framework, "It looks like Atlas is running from a development environment. Loading shaders from dev environment instead.");
            }
#endif


            Debug.Log(LogCategory.Framework, "Core framework functionalities started after " + primaryStopwatch.ElapsedMilliseconds + "ms");
            _w = new Window(windowTitle, flags);
            Debug.Log(LogCategory.Framework, "Window created after " + primaryStopwatch.ElapsedMilliseconds + "ms");
            if (timer != null)
                timer.Stop();
        }

        public static void Start()
        {

            if (_w == null)
            {
                throw new NullReferenceException("Window hasn't been created yet!");
            }
            Debug.Log(LogCategory.Rendering, "Rendering first frame after " + primaryStopwatch?.ElapsedMilliseconds + "ms");
            try
            {
                _w.StartRenderLoop();
            }
            catch (Exception ex)
            {
                Debug.Error(ex.ToString());
            }
            _doTick = false;
            Audio.Audio.Dispose();
            EntityComponentSystem.Dispose();
            Renderer.Dispose();
            Input.Input.Dispose();
            primaryStopwatch?.Stop();
            Debug.Log(LogCategory.Framework, "Atlas shutting down after " + (Math.Round((primaryStopwatch?.ElapsedMilliseconds ?? 0) / 100f) / 10) + "s...");
            Telescope.Debug.Dispose();
        }

        public static void StartTickLoop()
        {
            Thread t = new Thread(StartTickLoopTimer);
            t.Name = "ECS";
            t.Start();
        }
        private static async void StartTickLoopTimer()
        {
            Debug.Log(LogCategory.Framework, "Starting tick loop with a frequency of " + TickFrequency);
            System.Diagnostics.Stopwatch updateDuration = new System.Diagnostics.Stopwatch();
            while (_doTick)
            {
                if (TickFrequency == 0 && EntityComponentSystem.HasStarted)
                {
                    await Task.Delay(20);
                    continue;
                }
                updateDuration.Restart();
                RunTick();
                updateDuration.Stop();
                if (TickFrequency == 0)
                    continue;
                int delay = (1000 / TickFrequency - (int)Math.Ceiling(updateDuration.Elapsed.TotalMilliseconds));
                if (delay > 0)
                {
                    Thread.Sleep(delay);
                }
            }
        }
        private static int _ticksThisSecond = 0;
        public static int TicksPerSecond { get; private set; }
        /// <summary>
        /// Time elapsed between ticks, in seconds.
        /// </summary>

        private static readonly System.Diagnostics.Stopwatch TickCounterStopwatch = new System.Diagnostics.Stopwatch();
        private static readonly System.Diagnostics.Stopwatch TickDeltaStopwatch = new System.Diagnostics.Stopwatch();

        private static int lastDataUpdate = 0;
        static void RunTick()
        {
            TickDeltaStopwatch.Stop();
            Time.tickDeltaTime = TickDeltaStopwatch.Elapsed.TotalSeconds;
            Time.tickTime = ecsStopwatch!.Elapsed.TotalSeconds;
            if (!TickCounterStopwatch.IsRunning)
            {
                TickCounterStopwatch.Start();
            }
            if (TickCounterStopwatch.ElapsedMilliseconds >= 1000)
            {
                TicksPerSecond = _ticksThisSecond;
                _ticksThisSecond = 0;
                TickCounterStopwatch.Restart();
            }
            Stopwatch sw = new Stopwatch();
            sw.Start();
            _ticksThisSecond++;
            TickDeltaStopwatch.Restart();
            
#if DEBUG
            Profiler.StartTimer(Profiler.TickType.Tick);
#endif
            Task t = TickScheduler.RequestTick();
            t.Wait();
#if DEBUG
            Profiler.EndTimer(Profiler.TickType.Tick, "Wait");
#endif

            if (!ecsStopwatch.IsRunning)
            {
                ecsStopwatch.Start();
            }
            
            EntityComponentSystem.Tick();
#if DEBUG
            lastDataUpdate++;
            if (lastDataUpdate > 50)
            {
                lastDataUpdate = 0;
                Telescope.Debug.LiveData = new LiveData(new [] { "FPS: " + Math.Round(Window.AverageFramerate) + "/" + (Window.MaxFramerate == 0 ? "VSYNC" : Window.MaxFramerate), "TPS: " + Atlas.TicksPerSecond + "/" + Atlas.TickFrequency, "Runtime: " + Atlas.GetTotalUptime().ToString("0.0") + "s", Version }, EntityComponentSystem.GetECSHierarchy());
            }
            Profiler.SubmitTimes(Profiler.TickType.Tick);
#endif
            TickScheduler.FreeThreads();
            sw.Stop();
        }

        public static float GetTotalUptime()
        {
            if (primaryStopwatch == null)
            {
                return 0f;
            }
            return (float)primaryStopwatch.Elapsed.TotalSeconds;
        }
    }
}