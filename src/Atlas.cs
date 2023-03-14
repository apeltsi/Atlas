
namespace SolidCode.Atlas
{
    using System.Timers;
    using SolidCode.Atlas.Rendering;
    using SolidCode.Atlas.ECS;
    using SolidCode.Atlas.Components;
    using SolidCode.Atlas.ECS.SceneManagement;

    public enum LogCategory
    {
        General,
        Framework,
        Rendering,
        ECS
    }

    public static class Atlas
    {
        public static string ActiveDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
        public static string DataDirectory = Path.Join(ActiveDirectory, "data" + Path.DirectorySeparatorChar);
        public static string ShaderDirectory = Path.Join(DataDirectory, "shaders" + Path.DirectorySeparatorChar);

        public static string AssetsDirectory = Path.Join(DataDirectory, "assets" + Path.DirectorySeparatorChar);
        public static string AppName = "Atlas";
        public const string Version = "peppermint-tea@1.4";

        public static int TickFrequency = 100;
        public static Timer timer;
        internal static System.Diagnostics.Stopwatch primaryStopwatch { get; private set; }
        static Window? w;
        static bool doTick = true;
        public static void InitializeLogging()
        {
            Debug.StartLogs("General", "Framework", "Rendering", "ECS");
        }
        public static void StartRenderFeatures(string windowTitle)
        {
            AppName = windowTitle;
            primaryStopwatch = System.Diagnostics.Stopwatch.StartNew();
            Debug.Log(LogCategory.Framework, "Atlas/" + Version + " starting up...");
#if DEBUG
            if (Directory.Exists("./data/shaders"))
            {
                // Were running in from the development path. Lets grab our shaders from there instead!
                ShaderDirectory = "./data/shaders";
                Debug.Log(LogCategory.Framework, "It looks like Atlas is running from a development environment. Loading shaders from dev environment instead.");
            }
#endif


            Debug.Log(LogCategory.Framework, "Core framework functionalities started after " + primaryStopwatch.ElapsedMilliseconds + "ms");
            w = new Window(windowTitle);
            Debug.Log(LogCategory.Framework, "Window created after " + primaryStopwatch.ElapsedMilliseconds + "ms");
            EntityComponentSystem.window = w;
            if (timer != null)
                timer.Stop();
        }

        public static void Start(Scene defaultScene)
        {
            if (w == null)
            {
                throw new NullReferenceException("Window hasn't been created yet!");
            }
            SceneManager.LoadScene(defaultScene);
            Debug.Log(LogCategory.Rendering, "Rendering first frame after " + primaryStopwatch.ElapsedMilliseconds + "ms");
            Audio.AudioManager.InitializeAudio();
            try
            {
                w.StartRenderLoop();
            }
            catch (Exception ex)
            {
                Debug.Error(ex.ToString());
            }
            doTick = false;
            EntityComponentSystem.Dispose();
            primaryStopwatch.Stop();
            Debug.Log(LogCategory.Framework, "Atlas shutting down after " + (Math.Round(primaryStopwatch.ElapsedMilliseconds / 100f) / 10) + "s...");
            Debug.Stop();
        }

        public static void StartTickLoop()
        {
            Thread t = new Thread(StartTickLoopTimer);
            t.Name = "Primary ECS Tick Thread";
            t.Start();
        }
        private static void StartTickLoopTimer()
        {
            Debug.Log(LogCategory.Framework, "Starting tick loop with a frequency of " + TickFrequency);
            System.Diagnostics.Stopwatch updateDuration = new System.Diagnostics.Stopwatch();
            while (doTick)
            {
                updateDuration.Restart();
                RunTick();
                updateDuration.Stop();
                long delay = (1000 / TickFrequency - updateDuration.ElapsedMilliseconds);
                if (delay > 0)
                {
                    Thread.Sleep((int)delay);
                }
            }
        }
        private static long lastWarning = 0; // The tick that the last performance warning was printed at
        private static long curTick = 0; // The current tick
        private static int ticksThisSecond = 0;
        public static int TicksPerSecond { get; private set; }
        /// <summary>
        /// Time elapsed between ticks, in seconds.
        /// </summary>

        private static System.Diagnostics.Stopwatch tickCounterStopwatch = new System.Diagnostics.Stopwatch();
        private static System.Diagnostics.Stopwatch tickDeltaStopwatch = new System.Diagnostics.Stopwatch();
        static void RunTick()
        {
            tickDeltaStopwatch.Stop();
            Time.tickDeltaTime = tickDeltaStopwatch.Elapsed.TotalSeconds;
            Time.tickTime = primaryStopwatch.Elapsed.TotalSeconds;
            if (!tickCounterStopwatch.IsRunning)
            {
                tickCounterStopwatch.Start();
            }
            if (tickCounterStopwatch.ElapsedMilliseconds >= 1000)
            {
                TicksPerSecond = ticksThisSecond;
                ticksThisSecond = 0;
                tickCounterStopwatch.Restart();
            }
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            ticksThisSecond++;
            tickDeltaStopwatch.Restart();
            Task t = new Task(EntityComponentSystem.Tick);
            TickScheduler.RequestTick(t);
            t.Wait();
            TickScheduler.FreeThreads();
            sw.Stop();
            if (sw.Elapsed.TotalMilliseconds > 1000f / TickFrequency && curTick - lastWarning > (TickFrequency * 10))
            {
                lastWarning = curTick;
                Debug.Warning(LogCategory.ECS, "Atlas is unable to keep up with current tick frequency of " + TickFrequency + ". Tick took " + sw.Elapsed.TotalMilliseconds + "ms");
            }
            curTick++;
        }

        public static float GetUptime()
        {
            if (primaryStopwatch == null)
            {
                return 0f;
            }
            return (float)primaryStopwatch.Elapsed.TotalSeconds;
        }
    }
}