
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
        public static void InitializeLogging()
        {
            Debug.StartLogs("General", "Framework", "Rendering", "ECS");
        }
        public static void Start(Scene defaultScene, string windowTitle)
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
            Window w = new Window(windowTitle);
            Debug.Log(LogCategory.Framework, "Window created after " + watch.ElapsedMilliseconds + "ms");
            EntityComponentSystem.window = w;
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
            if (timer != null)
                timer.Stop();
            EntityComponentSystem.Dispose();
            watch.Stop();
            Debug.Log(LogCategory.Framework, "Atlas shutting down after " + (Math.Round(watch.ElapsedMilliseconds / 100f) / 10) + "s...");
            Debug.Stop();
        }

        public static void StartFixedUpdateLoop()
        {
            Debug.Log(LogCategory.Framework, "Starting fixed update loop with a frequency of " + updateFrequency);
            timer = new System.Timers.Timer(1000f / updateFrequency);
            timer.Elapsed += new System.Timers.ElapsedEventHandler(FixedUpdate);
            timer.AutoReset = true;
            timer.Start();
        }

        public static void FixedUpdate(object? sender, ElapsedEventArgs e)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            EntityComponentSystem.FixedUpdate();
            sw.Stop();
            if (sw.Elapsed.TotalMilliseconds > 1000f / updateFrequency)
            {
                Debug.Warning("Atlas is unable to keep up with current update frequency of " + updateFrequency + ". FixedUpdate took " + sw.Elapsed.TotalMilliseconds + "ms");
            }

        }

        public static float GetUptime()
        {
            return (watch.ElapsedMilliseconds) / 1000f;
        }
    }
}