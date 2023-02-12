
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
        public const string Version = "0.1.0a";

        public static int updateFrequency = 100;
        public static Timer timer;
        public static System.Diagnostics.Stopwatch watch { get; private set; }
        static Window? w;
        public static void InitializeLogging()
        {
            Debug.StartLogs("General", "Framework", "Rendering", "ECS");
        }
        public static void StartRenderFeatures(string windowTitle)
        {
            AppName = windowTitle;
            watch = System.Diagnostics.Stopwatch.StartNew();
            Debug.Log(LogCategory.Framework, "Atlas " + Version + " starting up...");
#if DEBUG
            if (Directory.Exists("./data/shaders"))
            {
                // Were running in from the development path. Lets grab our shaders from there instead!
                ShaderDirectory = "./data/shaders";
                Debug.Log(LogCategory.Framework, "It looks like Atlas is running from a development enviroment. Loading shaders from dev enviroment instead.");
            }
#endif


            Debug.Log(LogCategory.Framework, "Core framework functionalities started after " + watch.ElapsedMilliseconds + "ms");
            w = new Window(windowTitle);
            Debug.Log(LogCategory.Framework, "Window created after " + watch.ElapsedMilliseconds + "ms");
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
            Debug.Log(LogCategory.Rendering, "Rendering first frame after " + watch.ElapsedMilliseconds + "ms");
            Audio.AudioManager.InitializeAudio();
            try
            {
                w.StartRenderLoop();
            }
            catch (Exception ex)
            {
                Debug.Error(ex.ToString());
            }
            EntityComponentSystem.Dispose();
            watch.Stop();
            Debug.Log(LogCategory.Framework, "Atlas shutting down after " + (Math.Round(watch.ElapsedMilliseconds / 100f) / 10) + "s...");
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
            timer = new System.Timers.Timer(1000f / updateFrequency);
            timer.Elapsed += new System.Timers.ElapsedEventHandler(FixedUpdate);
            timer.AutoReset = true;
            timer.Start();
        }
        private static int lastWarning = 0; // The tick that the last performance warning was printed at
        private static int curTick = 0;
        private static bool ongoingFixedUpdate = false;
        private static int fixedUpdatesThisSecond = 0;
        public static int FixedUpdatesPerSecond { get; private set; }
        /// <summary>
        /// Time elapsed between fixedupdates, in seconds.
        /// </summary>

        public static double FixedUpdateDeltaTime = 0f;
        private static System.Diagnostics.Stopwatch fixedUpdateCounterStopwatch = new System.Diagnostics.Stopwatch();
        private static System.Diagnostics.Stopwatch fixedUpdateDeltaStopwatch = new System.Diagnostics.Stopwatch();
        private static int queuedUpdates = 0;
        public static void FixedUpdate(object? sender, ElapsedEventArgs e)
        {
            if (ongoingFixedUpdate)
            {
                Interlocked.Increment(ref queuedUpdates);
                if (queuedUpdates > 1)
                {
                    return;
                }
                while (ongoingFixedUpdate)
                {
                    Thread.Sleep(1);
                }
                Interlocked.Decrement(ref queuedUpdates);
            }
            if (ongoingFixedUpdate)
                return;
            ongoingFixedUpdate = true;
            fixedUpdateDeltaStopwatch.Stop();
            FixedUpdateDeltaTime = fixedUpdateDeltaStopwatch.ElapsedMilliseconds / 1000.0;
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
            ongoingFixedUpdate = false;
        }

        public static float GetUptime()
        {
            return (watch.ElapsedMilliseconds) / 1000f;
        }
    }
}