
namespace SolidCode.Caerus
{
    using System.Timers;
    using SolidCode.Caerus.Rendering;
    using SolidCode.Caerus.ECS;
    using SolidCode.Caerus.Components;
    using SolidCode.Caerus.ECS.SceneManagement;

    public enum LogCategories
    {
        General,
        Framework,
        Rendering,
        ECS
    }

    public static class Caerus
    {
        public static string ActiveDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
        public static string DataDirectory = Path.Join(ActiveDirectory, "data" + Path.DirectorySeparatorChar);
        public static string ShaderDirectory = Path.Join(DataDirectory, "shaders" + Path.DirectorySeparatorChar);

        public static string AssetsDirectory = Path.Join(DataDirectory, "assets" + Path.DirectorySeparatorChar);
        public static string AppName = "Caerus";
        public const string Version = "0.1.0a";

        public static int updateFrequency = 100;
        public static Timer timer;
        public static EntityComponentSystem? ecs;
        public static System.Diagnostics.Stopwatch watch { get; private set; }
        public static void InitializeLogging()
        {
            Debug.StartLogs("General", "Framework", "Rendering", "ECS");
        }
        public static void Start(Scene defaultScene, string windowTitle)
        {
            AppName = windowTitle;
            watch = System.Diagnostics.Stopwatch.StartNew();
            Debug.Log(LogCategories.Framework, "Caerus " + Version + " starting up...");
#if DEBUG
            if (Directory.Exists("./data/shaders"))
            {
                // Were running in from the development path. Lets grab our shaders from there instead!
                ShaderDirectory = "./data/shaders";
                Debug.Log(LogCategories.Framework, "It looks like Caerus is running from a development enviroment. Loading shaders from dev enviroment instead.");
            }
#endif

            ecs = new EntityComponentSystem();

            Debug.Log(LogCategories.Framework, "Core framework functionalities started after " + watch.ElapsedMilliseconds + "ms");
            Window w = new Window(windowTitle);
            Debug.Log(LogCategories.Framework, "Window created after " + watch.ElapsedMilliseconds + "ms");
            ecs.window = w;
            SceneManager.LoadScene(defaultScene);
            Debug.Log(LogCategories.Rendering, "Rendering first frame after " + watch.ElapsedMilliseconds + "ms");
            Audio.AudioManager.InitializeAudio();
            try
            {
                w.StartRenderLoop(ecs);
            }
            catch (Exception ex)
            {
                Debug.Error(ex.ToString());
            }
            if (timer != null)
                timer.Stop();
            ecs.Dispose();
            watch.Stop();
            Debug.Log(LogCategories.Framework, "Caerus shutting down after " + (Math.Round(watch.ElapsedMilliseconds / 100f) / 10) + "s...");
            Debug.Stop();
        }

        public static void StartFixedUpdateLoop(EntityComponentSystem ecs)
        {
            Debug.Log(LogCategories.Framework, "Starting fixed update loop with a frequency of " + updateFrequency);
            timer = new System.Timers.Timer(1000f / updateFrequency);
            timer.Elapsed += new System.Timers.ElapsedEventHandler(FixedUpdate);
            timer.AutoReset = true;
            timer.Start();
        }

        public static void FixedUpdate(object? sender, ElapsedEventArgs e)
        {
            if (ecs != null)
            {
                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                sw.Start();
                ecs.FixedUpdate();
                sw.Stop();
                if (sw.Elapsed.TotalMilliseconds > 1000f / updateFrequency)
                {
                    Debug.Warning("Caerus is unable to keep up with current update frequency of " + updateFrequency + ". FixedUpdate took " + sw.Elapsed.TotalMilliseconds + "ms");
                }
            }
        }

        public static float GetUptime()
        {
            return (watch.ElapsedMilliseconds) / 1000f;
        }
    }
}