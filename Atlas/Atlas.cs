using System.Diagnostics;
#if Windows
using SolidCode.Atlas.Rendering.Windows;
#endif
namespace SolidCode.Atlas
{
    using System.Timers;
    using Rendering;
    using ECS;
    using Veldrid.Sdl2;
    using AssetManagement;

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
        public static string ActiveDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly()?.Location) ?? "";
        public static string DataDirectory = Path.Join(ActiveDirectory, "data" + Path.DirectorySeparatorChar);
        public static string ShaderDirectory = Path.Join(DataDirectory, "shaders" + Path.DirectorySeparatorChar);

        public static string AssetsDirectory = Path.Join(DataDirectory, "assets" + Path.DirectorySeparatorChar);
        public static string AssetPackDirectory = Path.Join(ActiveDirectory, "assets" + Path.DirectorySeparatorChar);
        public const string Version = "iced-coffee@1.0-pre.5";
        private static Timer? _timer;
        internal static Stopwatch? PrimaryStopwatch { get; private set; }
        internal static Stopwatch? ECSStopwatch { get; private set; }

        static Window? _w;
        static bool _doTick = true;
        private static DebuggingMode _mode = DebuggingMode.Auto;
        private static FrameworkConfiguration? _config;
        public static FrameworkConfiguration Configuration => _config ?? new FrameworkConfiguration();

        public static void DisableMultiProcessDebugging()
        {
            if (Debug.LogsInitialized && _mode == DebuggingMode.Auto)
                throw new NotSupportedException("Multi-Process Debugging can only be disabled before any logs have been printed.");
            _mode = DebuggingMode.Disabled;
        }
        internal static void InitializeLogging()
        {
            if(_mode != DebuggingMode.Disabled)
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

        public static void StartCoreFeatures(string windowTitle, FrameworkConfiguration? configuration = null, SDL_WindowFlags flags = 0)
        {
            _config = configuration;

            Debug.CheckLog();
            #if Windows
            ForceHighPerformance.InitializeDedicatedGraphics();
            #endif
            PrimaryStopwatch = Stopwatch.StartNew();
            ECSStopwatch = new Stopwatch();
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


            Debug.Log(LogCategory.Framework, "Core framework functionalities started after " + PrimaryStopwatch.ElapsedMilliseconds + "ms");
            _w = new Window(windowTitle, flags);
            Debug.Log(LogCategory.Framework, "Window created after " + PrimaryStopwatch.ElapsedMilliseconds + "ms");
            if (_timer != null)
                _timer.Stop();
        }

        public static void Start()
        {

            if (_w == null)
            {
                throw new NullReferenceException("Window hasn't been created yet!");
            }
            Debug.Log(LogCategory.Rendering, "Rendering first frame after " + PrimaryStopwatch?.ElapsedMilliseconds + "ms");
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
            PrimaryStopwatch?.Stop();
            Debug.Log(LogCategory.Framework, "Atlas shutting down after " + (Math.Round((PrimaryStopwatch?.ElapsedMilliseconds ?? 0) / 100f) / 10) + "s...");
            Telescope.Debug.Dispose();
            
        }

        public static float GetTotalUptime()
        {
            if (PrimaryStopwatch == null)
            {
                return 0f;
            }
            return (float)PrimaryStopwatch.Elapsed.TotalSeconds;
        }
    }
}