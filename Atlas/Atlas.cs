﻿using System.Diagnostics;
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
        public static string AppDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly()?.Location) ?? "";
        public static string DataDirectory = Path.Join(AppDirectory, "data" + Path.DirectorySeparatorChar);
        public static string ShaderDirectory = Path.Join(DataDirectory, "shaders" + Path.DirectorySeparatorChar);

        public static string AssetsDirectory = Path.Join(DataDirectory, "assets" + Path.DirectorySeparatorChar);
        public static string AssetPackDirectory = Path.Join(AppDirectory, "assets" + Path.DirectorySeparatorChar);
        public const string Version = "1.0.0-pre.13";
        private static Timer? _timer;
        internal static Stopwatch? PrimaryStopwatch { get; private set; }
        internal static Stopwatch? ECSStopwatch { get; private set; }

        static Window? _w;
        static bool _doTick = true;
        private static DebuggingMode _mode = DebuggingMode.Auto;
        private static FrameworkConfiguration? _config;
        internal static bool AudioEnabled = true;
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
                Telescope.Debug.UseMultiProcessDebugging(Version);
            Telescope.Debug.StartLogs(new string[] { "General", "Framework", "Rendering", "ECS" });
            Telescope.Debug.RegisterTelescopeAction("showwindow", ShowWindow);
            Telescope.Debug.RegisterTelescopeAction("quit", Quit);
        }
        private static void ShowWindow()
        {
            Window.MoveToFront();
        }
        
        /// <summary>
        /// Closes the app
        /// </summary>
        public static void Quit()
        {
            Window.Close();
        }
        /// <summary>
        /// Tells Atlas to initialize the logging system<para/>
        /// Technically optional, but not using this could impact app startup performance or lead to unexpected issues. Especially if you're using multi-process debugging.
        /// <para/>
        /// Multi-process debugging works by using two separate processes. One for the debugger, one for the app. The debug(first) process will halt normal code execution when the Atlas logging system is enabled.
        /// It will then in turn start the actual process and some extra debugging features. Not calling UseLogging() directly after the app starts could lead to unnecessary code being executed twice, on both processes.
        /// </summary>
        public static void UseLogging()
        {
            Debug.CheckLog();
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

       
        /// <summary>
        /// Starts the main features in Atlas, without displaying a window. This lets you load assets, and do any preparations before the window is opened.
        /// </summary>
        /// <param name="windowTitle">The window title</param>
        /// <param name="configuration">Framework configuration</param>
        /// <param name="flags">Any flags for SDL</param>
        public static void StartCoreFeatures(string windowTitle = "Atlas", FrameworkConfiguration? configuration = null, SDL_WindowFlags flags = 0)
        {
            if(StartupArgumentExists("--no-audio"))
                AudioEnabled = false;
            _config = configuration;

            Debug.CheckLog();
            #if Windows
            ForceHighPerformance.InitializeDedicatedGraphics();
            #endif
            PrimaryStopwatch = Stopwatch.StartNew();
            ECSStopwatch = new Stopwatch();
            Debug.Log(LogCategory.Framework, "Atlas/" + Version + " starting up...");
            if(AudioEnabled)
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

        /// <summary>
        /// Actually starts the engine, and displays the window. It's recommended to call StartCoreFeatures() first, giving you more time to load assets and prepare.
        /// </summary>
        /// <exception cref="NullReferenceException"></exception>
        public static void Start()
        {

            if (_w == null)
            {
                StartCoreFeatures();
            }
            Debug.Log(LogCategory.Rendering, "Rendering first frame after " + PrimaryStopwatch?.ElapsedMilliseconds + "ms");
            try
            {
                _w.StartRenderLoop();
            }
            catch (Exception ex)
            {
                Debug.Error(LogCategory.Framework, ex.ToString());
            }
            _doTick = false;
            if(AudioEnabled)
                Audio.Audio.DisposeAllSources();
            EntityComponentSystem.Dispose();
            Renderer.Dispose();
            Input.Input.Dispose();
            if(AudioEnabled)
                Audio.Audio.Dispose();
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