
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
        public const string Version = "grape-juice@1.3";

        public static int updateFrequency = 100;
        public static Timer timer;
        internal static System.Diagnostics.Stopwatch primaryStopwatch { get; private set; }
        static Window? w;
        static bool doFixedUpdate = true;
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
                Debug.Log(LogCategory.Framework, "It looks like Atlas is running from a development enviroment. Loading shaders from dev enviroment instead.");
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
            doFixedUpdate = false;
            EntityComponentSystem.Dispose();
            primaryStopwatch.Stop();
            Debug.Log(LogCategory.Framework, "Atlas shutting down after " + (Math.Round(primaryStopwatch.ElapsedMilliseconds / 100f) / 10) + "s...");
            Debug.Stop();
        }

        public static void StartFixedUpdateLoop()
        {
            Thread t = new Thread(StartFixedUpdateLoopTimer);
            t.Name = "Primary ECS FixedUpdate Thread";
            t.Start();
        }
        private static void StartFixedUpdateLoopTimer()
        {
            Debug.Log(LogCategory.Framework, "Starting fixed update loop with a frequency of " + updateFrequency);
            System.Diagnostics.Stopwatch updateDuration = new System.Diagnostics.Stopwatch();
            while (doFixedUpdate)
            {
                updateDuration.Restart();
                RunFixedUpdate();
                updateDuration.Stop();
                long delay = (1000 / updateFrequency - updateDuration.ElapsedMilliseconds);
                if (delay > 0)
                {
                    Thread.Sleep((int)delay);
                }
            }
        }
        private static long lastWarning = 0; // The tick that the last performance warning was printed at
        private static long curTick = 0; // The current tick
        private static int fixedUpdatesThisSecond = 0;
        public static int FixedUpdatesPerSecond { get; private set; }
        /// <summary>
        /// Time elapsed between fixedupdates, in seconds.
        /// </summary>

        private static System.Diagnostics.Stopwatch fixedUpdateCounterStopwatch = new System.Diagnostics.Stopwatch();
        private static System.Diagnostics.Stopwatch fixedUpdateDeltaStopwatch = new System.Diagnostics.Stopwatch();
        static void RunFixedUpdate()
        {
            fixedUpdateDeltaStopwatch.Stop();
            Time.fixedDeltaTime = fixedUpdateDeltaStopwatch.Elapsed.TotalSeconds;
            Time.fixedTime = primaryStopwatch.Elapsed.TotalSeconds;
            if (!fixedUpdateCounterStopwatch.IsRunning)
            {
                fixedUpdateCounterStopwatch.Start();
            }
            if (fixedUpdateCounterStopwatch.ElapsedMilliseconds >= 1000)
            {
                FixedUpdatesPerSecond = fixedUpdatesThisSecond;
                fixedUpdatesThisSecond = 0;
                fixedUpdateCounterStopwatch.Restart();
            }
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            fixedUpdatesThisSecond++;
            EntityComponentSystem.FixedUpdate();
            sw.Stop();
            if (sw.Elapsed.TotalMilliseconds > 1000f / updateFrequency && curTick - lastWarning > (updateFrequency * 10))
            {
                lastWarning = curTick;
                Debug.Warning(LogCategory.ECS, "Atlas is unable to keep up with current update frequency of " + updateFrequency + ". FixedUpdate took " + sw.Elapsed.TotalMilliseconds + "ms");
            }
            curTick++;
            fixedUpdateDeltaStopwatch.Restart();
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