using System.Diagnostics;
using System.Reflection;
using SolidCode.Atlas.AssetManagement;
using SolidCode.Atlas.ECS;
using SolidCode.Atlas.Rendering;
using Veldrid.Sdl2;
#if Windows
using SolidCode.Atlas.Rendering.Windows;
#endif

namespace SolidCode.Atlas;

/// <summary>
/// Labels logs as part of a category
/// </summary>
public enum LogCategory
{
    /// <summary>
    /// A general log
    /// </summary>
    General,

    /// <summary>
    /// A framework related log
    /// </summary>
    Framework,

    /// <summary>
    /// A rendering related log
    /// </summary>
    Rendering,

    /// <summary>
    /// A ECS related log
    /// </summary>
    ECS
}

/// <summary>
/// Defines how the debugging system should be initialized
/// </summary>
public enum DebuggingMode
{
    /// <summary>
    /// Multi-process debugging will be enabled unless the --disable-multi-process-debugging argument was passed to the app
    /// </summary>
    Auto,

    /// <summary>
    /// Multi-process debugging will be disabled
    /// </summary>
    Disabled
}

/// <summary>
/// The main class of Atlas. This is where you start the engine, and access some of the main features.
/// </summary>
public static class Atlas
{
    /// <summary>
    /// The path of the entry assembly directory
    /// </summary>
    public static string AppDirectory =
        Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ?? "";

    /// <summary>
    /// The path of the assets directory
    /// </summary>
    public static string AssetsDirectory = Path.Join(AppDirectory, "assets" + Path.DirectorySeparatorChar);

    /// <summary>
    /// The path of the shader directory
    /// </summary>
    public static string ShaderDirectory = Path.Join(AssetsDirectory, "shaders" + Path.DirectorySeparatorChar);

    /// <summary>
    /// The path of the assetpacks directory
    /// </summary>
    public static string AssetPackDirectory = Path.Join(AppDirectory, "assetpacks" + Path.DirectorySeparatorChar);

    private static Window? _w;
    private static DebuggingMode _mode = DebuggingMode.Auto;
    private static FrameworkConfiguration? _config;
    internal static bool AudioEnabled = true;

    /// <summary>
    /// Returns the active version of Atlas
    /// </summary>
    public static string Version =>
        FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion ??
        "Unknown Version";

    internal static Stopwatch? PrimaryStopwatch { get; private set; }
    internal static Stopwatch? ECSStopwatch { get; private set; }

    /// <summary>
    /// The configuration of the framework
    /// </summary>
    public static FrameworkConfiguration Configuration => _config ?? new FrameworkConfiguration();

    /// <summary>
    /// Note: This should be one of the first things called in your app!
    /// </summary>
    /// <exception cref="NotSupportedException">This method may throw if the logging system has already started</exception>
    public static void DisableMultiProcessDebugging()
    {
        if (Debug.LogsInitialized && _mode == DebuggingMode.Auto)
            throw new NotSupportedException(
                "Multi-Process Debugging can only be disabled before any logs have been printed.");
        _mode = DebuggingMode.Disabled;
    }

    internal static void InitializeLogging()
    {
        if (_mode != DebuggingMode.Disabled)
            Telescope.Debug.UseMultiProcessDebugging(Version);
        Telescope.Debug.StartLogs(new[] { "General", "Framework", "Rendering", "ECS" });
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
    /// Tells Atlas to initialize the logging system
    /// <para />
    /// Technically optional, but not using this could impact app startup performance or lead to unexpected issues.
    /// Especially if you're using multi-process debugging.
    /// <para />
    /// Multi-process debugging works by using two separate processes. One for the debugger, one for the app. The
    /// debug(first) process will halt normal code execution when the Atlas logging system is enabled.
    /// It will then in turn start the actual process and some extra debugging features. Not calling UseLogging() directly
    /// after the app starts could lead to unnecessary code being executed twice, on both processes.
    /// </summary>
    public static void UseLogging()
    {
        Debug.CheckLog();
    }

    /// <summary>
    /// Checks if a startup argument exists
    /// </summary>
    /// <param name="argument">The startup argument to check for</param>
    /// <returns>A boolean indicating if the provided startup argument exists</returns>
    public static bool StartupArgumentExists(string argument)
    {
        var args = Environment.GetCommandLineArgs();
        foreach (var arg in args)
            if (arg.ToLower() == argument.ToLower())
                return true;

        return false;
    }


    /// <summary>
    /// Starts the main features in Atlas, without displaying a window. This lets you load assets, and do any preparations
    /// before the window is opened.
    /// </summary>
    /// <param name="windowTitle">The window title</param>
    /// <param name="configuration">Framework configuration</param>
    /// <param name="flags">Any flags for SDL</param>
    public static void StartCoreFeatures(string windowTitle = "Atlas", FrameworkConfiguration? configuration = null,
        SDL_WindowFlags flags = 0)
    {
        if (StartupArgumentExists("--no-audio"))
            AudioEnabled = false;
        _config = configuration;

        Debug.CheckLog();
#if Windows
        ForceHighPerformance.InitializeDedicatedGraphics();
#endif
        PrimaryStopwatch = Stopwatch.StartNew();
        ECSStopwatch = new Stopwatch();
        Debug.Log(LogCategory.Framework, "Atlas/" + Version + " starting up...");
        if (AudioEnabled)
            Audio.Audio.InitializeAudio();
        AssetManager.LoadAssetMap();

        Debug.Log(LogCategory.Framework,
            "Core framework functionalities started after " + PrimaryStopwatch.ElapsedMilliseconds + "ms");
        _w = new Window(windowTitle, flags);
        Debug.Log(LogCategory.Framework, "Window created after " + PrimaryStopwatch.ElapsedMilliseconds + "ms");
    }

    /// <summary>
    /// Actually starts the engine, and displays the window. It's recommended to call StartCoreFeatures() first, giving you
    /// more time to load assets and prepare.
    /// </summary>
    /// <exception cref="NullReferenceException"></exception>
    public static void Start()
    {
        if (_w == null) StartCoreFeatures();

        Debug.Log(LogCategory.Rendering,
            "Rendering first frame after " + PrimaryStopwatch?.ElapsedMilliseconds + "ms");
        try
        {
            _w?.StartRenderLoop();
        }
        catch (Exception ex)
        {
            Debug.Error(LogCategory.Framework, ex.ToString());
        }

        if (AudioEnabled)
            Audio.Audio.DisposeAllSources();
        EntityComponentSystem.Dispose();
        Renderer.Dispose();
        Input.Input.Dispose();
        if (AudioEnabled)
            Audio.Audio.Dispose();
        PrimaryStopwatch?.Stop();
        Debug.Log(LogCategory.Framework,
            "Atlas shutting down after " + Math.Round((PrimaryStopwatch?.ElapsedMilliseconds ?? 0) / 100f) / 10 +
            "s...");
        Telescope.Debug.Dispose();
    }

    /// <summary>
    /// Returns the total time the app has been running, in seconds
    /// </summary>
    public static float GetTotalUptime()
    {
        if (PrimaryStopwatch == null) return 0f;

        return (float)PrimaryStopwatch.Elapsed.TotalSeconds;
    }
}