using System.Diagnostics;
using System.Numerics;
using SolidCode.Atlas.AssetManagement;
using SolidCode.Atlas.ECS;
using SolidCode.Atlas.Mathematics;
using SolidCode.Atlas.Telescope;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace SolidCode.Atlas.Rendering;

public class Window
{
    private static Sdl2Window? _window;

    /// <summary>
    /// The background color of the window.
    /// </summary>
    public static RgbaFloat ClearColor = RgbaFloat.Black;

    /// <summary>
    /// What framerate the previous 60 frames were rendered in
    /// </summary>
    public static float AverageFramerate;

    private static bool _reloadShaders;
    private static string _title = "";

    private static bool _positionDirty;
    private static Vector2 _position = new(50, 50);

    private static bool _sizeDirty;
    private static Vector2 _size = new(800, 500);

    internal static Task? BuiltinAssetsTask;


    private readonly Stopwatch _renderTimeStopwatch = new();

    private int _frames;
    private float _frameTimes;

    /// <summary>
    /// Creates a new window with a title. Also initializes rendering
    /// </summary>
    internal Window(string title = "Atlas", SDL_WindowFlags flags = 0)
    {
        _title = title;
        var modifiedTitle = GetAdjustedWindowTitle(_title);

        var windowCI = new WindowCreateInfo
        {
            X = AMath.RoundToInt(_position.X),
            Y = AMath.RoundToInt(_position.Y),
            WindowWidth = AMath.RoundToInt(_size.X),
            WindowHeight = AMath.RoundToInt(_size.Y),
            WindowTitle = modifiedTitle,
            WindowInitialState = WindowState.Hidden
        };

        _window = CreateWindow.CreateWindowWithFlags(ref windowCI, flags);

        // Setup graphics device
        var options = new GraphicsDeviceOptions
        {
            PreferStandardClipSpaceYDirection = true,
            SyncToVerticalBlank = true,
            ResourceBindingModel = ResourceBindingModel.Improved,
            SwapchainSrgbFormat = false,
            SwapchainDepthFormat = null,
            Debug = false
        };
#if DEBUG
        options.Debug = true;
#endif
        GraphicsBackend? preferred = null;
        if (Atlas.StartupArgumentExists("--use-dx"))
            preferred = GraphicsBackend.Direct3D11;
        if (Atlas.StartupArgumentExists("--use-vk"))
            preferred = GraphicsBackend.Vulkan;
        if (preferred != null)
            Renderer.GraphicsDevice = VeldridStartup.CreateGraphicsDevice(_window, options, preferred!.Value);
        else
            Renderer.GraphicsDevice = VeldridStartup.CreateGraphicsDevice(_window, options);

        Debug.Log(LogCategory.Rendering,
            "Current graphics backend: " + Renderer.GraphicsDevice.BackendType);
#if DEBUG
        _window.Title = GetAdjustedWindowTitle(_title);
#endif
        // We have to load our builtin shaders now
        var coreAssets = new AssetPack("%ASSEMBLY%/core");
        BuiltinAssetsTask = new AssetPack("%ASSEMBLY%/atlas").LoadAsync();

        Renderer.UpdateGetScalingMatrix(new Vector2(_window.Width, _window.Height));
        var t = coreAssets.LoadAsync();
        _window.Resized += () =>
        {
            PreResize?.Invoke();
            Renderer.GraphicsDevice.ResizeMainWindow((uint)_window.Width, (uint)_window.Height);
            Renderer.UpdateGetScalingMatrix(new Vector2(_window.Width, _window.Height));
            Renderer.CreateResources(t);
            OnResize?.Invoke();
        };

        Renderer.CreateResources(t);
    }

    /// <summary>
    /// The maximum framerate the window will render at. Set to 0 to render at VSYNC.
    /// </summary>
    public static int MaxFramerate { get; set; }

    /// <summary>
    /// Returns or sets the window Title.
    /// <para />
    /// Note that debug builds will include extra information in the title.
    /// </summary>
    public static string Title
    {
        get => _title;
        set
        {
            _title = value;
            if (_window != null)
                _window.Title = GetAdjustedWindowTitle(value);
        }
    }

    /// <summary>
    /// Returns or sets the current <c>WindowState</c>.
    /// </summary>
    public static WindowState State
    {
        get
        {
            if (_window == null) return WindowState.Hidden;

            return _window.WindowState;
        }
        set
        {
            if (_window != null)
                _window.WindowState = value;
        }
    }

    /// <summary>
    /// Is the window currently focused.
    /// </summary>
    public static bool Focused
    {
        get
        {
            if (_window == null) return false;

            return _window.Focused;
        }
    }

    /// <summary>
    /// Toggles the ability for the user to resize the window.
    /// </summary>
    public static bool Resizable
    {
        get
        {
            if (_window == null) return false;

            return _window.Resizable;
        }
        set
        {
            if (_window != null) _window.Resizable = value;
        }
    }

    /// <summary>
    /// The position of the window relative to the upper left corner of the screen.
    /// </summary>
    public static Vector2 Position
    {
        get
        {
            if (_window == null) return Vector2.Zero;

            unsafe
            {
                var x = 0;
                var y = 0;
                Sdl2Native.SDL_GetWindowPosition(_window.SdlWindowHandle, &x, &y);
                return new Vector2(x, y);
            }
        }
        set
        {
            _position = value;
            _positionDirty = true;
        }
    }

    /// <summary>
    /// The size of the window in pixels.
    /// <para />
    /// Note that this isn't always the resolution everything is rendered at.
    /// See <see cref="Renderer.ResolutionScale" />and <see cref="Renderer.RenderResolution" />.
    /// </summary>
    public static Vector2 Size
    {
        get => _window == null ? _size : new Vector2(_window.Width, _window.Height);
        set
        {
            _size = value;
            _sizeDirty = true;
        }
    }

    /// <summary>
    /// Toggles the visibility of the cursor.
    /// </summary>
    public static bool CursorVisible
    {
        get
        {
            if (_window == null) return false;

            return _window.CursorVisible;
        }
        set
        {
            if (_window == null) return;

            _window.CursorVisible = value;
        }
    }

    /// <summary>
    /// Invoked when the window is resized but before the renderer's resources have been updated.
    /// </summary>
    public static event Action PreResize;

    /// <summary>
    /// Invoked after the window has been resized.
    /// </summary>
    public static event Action OnResize;

    /// <summary>
    /// Closes the window & Atlas
    /// </summary>
    public static void Close()
    {
        _window?.Close();
    }

    internal void StartRenderLoop()
    {
        var watch = Stopwatch.StartNew();
        var frame = 0;
        if (_window == null)
        {
            Debug.Error(LogCategory.Framework,
                "Window doesn't exist yet! Did you forget to call Start() or StartCoreFeatures()?");
            return;
        }

        while (_window.Exists)
        {
            _renderTimeStopwatch.Restart();
            if (frame == 1)
            {
                _window.Visible = true;
                _window.WindowState = WindowState.Normal;
                BuiltinAssetsTask.Wait();
                TickManager.Initialize();
                Input.Input.Initialize();
            }

            if (frame == 2) Debug.Log(LogCategory.Rendering, "First frame has been rendered. Rendering frame 2");

            var inputSnapshot = _window.PumpEvents();

            if (_reloadShaders)
            {
                _reloadShaders = false;
                Renderer.ReloadAllShaders();
            }

            Input.Input.UpdateInputs(inputSnapshot);
            frame++;
#if DEBUG
            Profiler.StartTimer(Profiler.TickType.Update);
#endif

            TickScheduler.RequestTick().Wait();
#if DEBUG
            Profiler.EndTimer(Profiler.TickType.Update, "Wait");
            Profiler.StartTimer(Profiler.TickType.Update);
#endif
            // Update window if needed
            if (_positionDirty)
            {
                Sdl2Native.SDL_SetWindowPosition(_window.SdlWindowHandle, AMath.RoundToInt(_position.X),
                    AMath.RoundToInt(_position.Y));
                _positionDirty = false;
            }

            if (_sizeDirty)
            {
                _window.Width = AMath.RoundToInt(_size.X);
                _window.Height = AMath.RoundToInt(_size.Y);
                _sizeDirty = false;
            }


            EntityComponentSystem.Update();
            // Update time
            Time.deltaTime = frame < 3 ? 0f : watch.Elapsed.TotalSeconds;
            Time.time = Atlas.ECSStopwatch.Elapsed.TotalSeconds;

            _frameTimes += (float)Time.deltaTime;
            _frames++;
            if (_frames >= 60)
            {
                _frames = 0;
                AverageFramerate = 1f / (_frameTimes / 60f);
                _frameTimes = 0f;
            }


            watch = Stopwatch.StartNew();
#if DEBUG
            Profiler.EndTimer(Profiler.TickType.Update, "Update");
#endif
            Renderer.Draw();

            _renderTimeStopwatch.Stop();
            if (MaxFramerate != 0)
                Thread.Sleep(Math.Clamp(
                    AMath.RoundToInt(1000.0 / MaxFramerate - _renderTimeStopwatch.Elapsed.TotalMilliseconds), 0,
                    1000));
        }
    }

    internal static void RequireBuiltinAssets()
    {
        if (BuiltinAssetsTask == null)
            Telescope.Debug.Warning(LogCategory.Framework,
                "Builtin Assets have not started loading yet. Has Atlas been properly initialized?");
        while (BuiltinAssetsTask == null) Thread.Sleep(10);
        BuiltinAssetsTask.Wait();
    }

    /// <summary>
    /// Makes the window fullscreen
    /// </summary>
    public static void Fullscreen()
    {
        State = WindowState.FullScreen;
    }

    /// <summary>
    /// Makes the window borderless fullscreen
    /// </summary>
    public static void BorderlessFullscreen()
    {
        State = WindowState.BorderlessFullScreen;
    }

    /// <summary>
    /// Makes the window windowed
    /// </summary>
    public static void Windowed()
    {
        State = WindowState.Normal;
    }

    /// <summary>
    /// Moves the window to the front of the screen
    /// </summary>
    public static void MoveToFront()
    {
        if (_window == null) return;
        _window.WindowState = WindowState.Minimized;
        Thread.Sleep(100);
        _window.WindowState = WindowState.Maximized;
    }


    private static string GetAdjustedWindowTitle(string preferred)
    {
#if DEBUG
        return preferred + " | Atlas/" + Atlas.Version + " | " + Renderer.GraphicsDevice?.BackendType +
               " on " + Renderer.GraphicsDevice?.DeviceName + " | Telescope Active" +
               (Atlas.StartupArgumentExists("--disable-multi-process-debugging")
                   ? " | Multi-Process Debugging Disabled"
                   : "");
#endif
        return preferred;
    }
}