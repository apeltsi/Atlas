﻿using System.Diagnostics;

namespace SolidCode.Atlas
{
    using System.Timers;
    using SolidCode.Atlas.Rendering;
    using SolidCode.Atlas.ECS;
    using SolidCode.Atlas.Components;
    using SolidCode.Atlas.ECS.SceneManagement;
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

    public static class Atlas
    {
        public static string ActiveDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly()?.Location) ?? "";
        public static string DataDirectory = Path.Join(ActiveDirectory, "data" + Path.DirectorySeparatorChar);
        public static string ShaderDirectory = Path.Join(DataDirectory, "shaders" + Path.DirectorySeparatorChar);

        public static string AssetsDirectory = Path.Join(DataDirectory, "assets" + Path.DirectorySeparatorChar);
        public static string AssetPackDirectory = Path.Join(ActiveDirectory, "assets" + Path.DirectorySeparatorChar);
        public static string AppName = "Atlas";
        public const string Version = "marble-soda@1.0";
        public static int TickFrequency = 100;
        public static Timer? timer;
        internal static System.Diagnostics.Stopwatch? primaryStopwatch { get; private set; }
        internal static Stopwatch? ecsStopwatch { get; private set; }

        static Window? _w;
        static bool _doTick = true;

        public static void InitializeLogging()
        {
            Telescope.Debug.UseMultiProcessDebugging(Version);
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

        public static void StartCoreFeatures(string windowTitle, SDL_WindowFlags flags = 0)
        {
            AppName = windowTitle;
            primaryStopwatch = System.Diagnostics.Stopwatch.StartNew();
            ecsStopwatch = new System.Diagnostics.Stopwatch();
            Debug.Log(LogCategory.Framework, "Atlas/" + Version + " starting up...");
            AudioManager.InitializeAudio();
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
            EntityComponentSystem.window = _w;
            if (timer != null)
                timer.Stop();
        }

        public static void Start(Scene defaultScene)
        {

            if (_w == null)
            {
                throw new NullReferenceException("Window hasn't been created yet!");
            }
            SceneManager.LoadScene(defaultScene);
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
            AudioManager.Dispose();
            EntityComponentSystem.Dispose();
            AssetManager.Dispose();

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
                long delay = (1000 / TickFrequency - updateDuration.ElapsedMilliseconds);
                if (delay > 0)
                {
                    await Task.Delay((int)delay);
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
            Task t = TickScheduler.RequestTick();
            t.Wait();
            if (!ecsStopwatch.IsRunning)
            {
                ecsStopwatch.Start();
            }
            EntityComponentSystem.Tick();
            Telescope.Debug.LiveData = new LiveData(new [] { "FPS: " + Math.Round(Window.AverageFramerate), "Runtime: " + Atlas.GetTotalUptime() * 1000, "TPS: " + Atlas.TicksPerSecond }, EntityComponentSystem.GetECSHierarchy());
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