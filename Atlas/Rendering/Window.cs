using System.Numerics;
using SolidCode.Atlas.ECS;
using SolidCode.Atlas.Input;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using System.Collections.Concurrent;
using SolidCode.Atlas.Mathematics;
using SolidCode.Atlas.AssetManagement;
using SolidCode.Atlas.Rendering.PostProcess;
using SolidCode.Atlas.Telescope;

namespace SolidCode.Atlas.Rendering
{

    public class Window
    {

        public static GraphicsDevice? GraphicsDevice;
        private static CommandList _commandList = null!;
        private static List<Drawable> _drawables = new();
        private static Sdl2Window? _window;
        private Matrix4x4 _windowScalingMatrix;
        public static int MaxFramerate { get; set; }
        public static Framebuffer PrimaryFramebuffer { get; protected set; }
        private Veldrid.Texture _mainColorTexture;
        private static TextureView? _mainColorView;
        private static ShaderPass? _resolvePass;
        private static TextureView? _finalTextureView;

        public static Vector2 MousePosition = Vector2.Zero;
        public static RgbaFloat ClearColor = RgbaFloat.Black;
        
        // Post Process
        public static bool DoPostProcess = true;
        private static readonly TextureSampleCount SampleCount = TextureSampleCount.Count1;
        public static List<PostProcessEffect> PostProcessEffects { get; protected set; } = new();
        
        public static TextureDescription MainTextureDescription { get; protected set; }

        
        /// <summary>
        /// Describes the scale of one unit. A scaling index of 1 = 1000px. A scaling index of 2 = 2000px etc etc.  
        /// </summary>
        public static float ScalingIndex { get; protected set; } = 1;
        
        /// <summary>
        /// What framerate the previous 60 frames were rendered in
        /// </summary>
        public static float AverageFramerate = 0f;

        private int _frames = 0;
        private float _frameTimes = 0f;
        private static bool _reloadShaders = false;
        private static ConcurrentBag<Drawable> _drawablesToAdd = new();
        private static ConcurrentBag<Drawable> _drawablesToRemove = new();
        private static TextureView? _downSampledTextureView;
        private static bool _resourcesDirty = false;
        private static object _resourcesLock = new();

        public static string Title
        {
            get
            {
                if (_window == null)
                {
                    return "";
                }
                return _window.Title;
            }
            set
            {
                string modifiedTitle = value;
#if DEBUG
                modifiedTitle += " | Atlas/" + Atlas.Version + " | Telescope Active";
#endif

                if (_window != null)
                    _window.Title = modifiedTitle;
            }
        }

        public static WindowState State
        {
            get
            {
                if (_window == null)
                {
                    return WindowState.Hidden;
                }
                return _window.WindowState;
            }
            set
            {
                if (_window != null)
                    _window.WindowState = value;
            }
        }

        public static bool Focused
        {
            get
            {
                if (_window == null)
                {
                    return false;
                }
                return _window.Focused;
            }
        }

        public static bool Resizable
        {
            get
            {
                if (_window == null)
                {
                    return false;
                }
                return _window.Resizable;
            }
            set
            {
                if (_window != null)
                {
                    _window.Resizable = value;
                }
            }
        }
        private static bool _positionDirty = false;
        private static Vector2 _position = new Vector2(50, 50);
        public static Vector2 Position
        {
            get
            {
                if (_window == null)
                {
                    return Vector2.Zero;
                }
                unsafe
                {
                    int x = 0;
                    int y = 0;
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
        private static bool sizeDirty = false;
        private static Vector2 _size = new Vector2(800, 500);
        public static Vector2 Size
        {
            get => _window == null ? Vector2.Zero : _size;
            set
            {
                _size = value;
                sizeDirty = true;
            }
        }

        public static bool CursorVisible
        {
            get
            {
                if(_window == null)
                {
                    return false;
                }
                return _window.CursorVisible;
            }
            set
            {
                if(_window == null)
                {
                    return;
                }
                _window.CursorVisible = value;
            }
        }

        /// <summary>
        /// Creates a new window with a title. Also initializes rendering
        /// </summary>
        internal Window(string title = "Atlas/" + Atlas.Version, SDL_WindowFlags flags = 0)
        {
            string modifiedTitle = title;
#if DEBUG
            modifiedTitle += " | Atlas/" + Atlas.Version + " | Telescope Active";
#endif
            WindowCreateInfo windowCI = new WindowCreateInfo()
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
            GraphicsDeviceOptions options = new GraphicsDeviceOptions
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

            _windowScalingMatrix = GetScalingMatrix(_window.Width, _window.Height);
            GraphicsBackend? preferred = null;
            if (Atlas.StartupArgumentExists("--use-dx"))
                preferred = GraphicsBackend.Direct3D11;
            if (Atlas.StartupArgumentExists("--use-vk"))
                preferred = GraphicsBackend.Vulkan;
            if(preferred != null)
                GraphicsDevice = VeldridStartup.CreateGraphicsDevice(_window, options, preferred!.Value);
            else 
                GraphicsDevice = VeldridStartup.CreateGraphicsDevice(_window, options);
            
            Debug.Log(LogCategory.Rendering, "Current graphics backend: " + GraphicsDevice.BackendType.ToString());
            // We have to load our builtin shaders now
            AssetPack builtinAssets = new AssetPack("atlas");
            builtinAssets.LoadAtlasAssetpack();

            _window.Resized += () =>
            {
                GraphicsDevice.ResizeMainWindow((uint)_window.Width, (uint)_window.Height);
                _windowScalingMatrix = GetScalingMatrix(_window.Width, _window.Height);
                CreateResources();
            };

            CreateResources();
        }

        public static void Close()
        {
            _window?.Close();
        }
        /// <summary>
        /// Adds a post process effect to the list of effects to be applied to the scene
        /// </summary>
        /// <param name="effect">The effect to be applied</param>
        public static void AddPostProcessEffect(PostProcessEffect effect)
        {
            PostProcessEffects.Add(effect);
            lock(_resourcesLock)
                _resourcesDirty = true;
        }
        /// <summary>
        /// Removes a post process effect from the list of effects to be applied to the scene
        /// </summary>
        /// <param name="effect">The effect to be removed</param>
        public static void RemovePostProcessEffect(PostProcessEffect effect)
        {
            PostProcessEffects.Remove(effect);
            lock(_resourcesLock)
                _resourcesDirty = true;
        }

        public static void AddDrawables(List<Drawable> drawables)
        {
            foreach (Drawable d in drawables)
            {
                _drawablesToAdd.Add(d);
            }
        }
        public static void RemoveDrawable(Drawable drawable)
        {
            _drawablesToRemove.Add(drawable);
        }

        private System.Diagnostics.Stopwatch _renderTimeStopwatch = new System.Diagnostics.Stopwatch();

        internal void StartRenderLoop()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            int frame = 0;
            if (_window == null) {Debug.Error(LogCategory.Framework, "Window doesn't exist yet! Did you forget to call Start() or StartCoreFeatures()?");return;}
            while (_window.Exists)
            {
                _renderTimeStopwatch.Restart();
                if (frame == 1)
                {
                    _window.Visible = true;
                    _window.WindowState = WindowState.Normal;
                    Atlas.StartTickLoop();
                }
                if (frame == 2)
                {
                    Debug.Log(LogCategory.Rendering, "First frame has been rendered. Rendering frame 2");
                }
                InputSnapshot inputSnapshot = _window.PumpEvents();

                if (_reloadShaders)
                {
                    _reloadShaders = false;
                    ReloadAllShaders();
                }
                InputManager.ClearInputs();
                InputManager.WheelDelta = inputSnapshot.WheelDelta;
                for (int i = 0; i < inputSnapshot.KeyEvents.Count; i++)
                {
                    KeyEvent e = inputSnapshot.KeyEvents[i];

                    if (e.Down == true)
                    {
                        InputManager.KeyPress(e.Key);
                        if (e.Key == Key.F5)
                        {
                            ReloadAllShaders();
                        }
                    }
                    else
                    {
                        InputManager.RemoveKeyPress(e.Key);
                    }
                }
                MousePosition = inputSnapshot.MousePosition;
                frame++;
#if DEBUG
                Profiler.StartTimer(Profiler.FrameTimeType.Waiting);
#endif

                TickScheduler.RequestTick().Wait();
#if DEBUG
                Profiler.EndTimer();
                Profiler.StartTimer(Profiler.FrameTimeType.Scripting);
#endif
                // Update window if needed
                if (_positionDirty)
                {
                    Sdl2Native.SDL_SetWindowPosition(_window.SdlWindowHandle, AMath.RoundToInt(_position.X), AMath.RoundToInt(_position.Y));
                    _positionDirty = false;
                }
                if (sizeDirty)
                {
                    _window.Width = AMath.RoundToInt(_size.X);
                    _window.Height = AMath.RoundToInt(_size.Y);
                    sizeDirty = false;
                }


                EntityComponentSystem.Update();
                // Update time
                Time.deltaTime = watch.Elapsed.TotalSeconds;
                Time.time = Atlas.ecsStopwatch.Elapsed.TotalSeconds;

                _frameTimes += (float)Time.deltaTime;
                if (_frames >= 60)
                {
                    _frames = 0;
                    AverageFramerate = 1f / (_frameTimes / 60f);
                    _frameTimes = 0f;
                }
                _frames++;

                watch = System.Diagnostics.Stopwatch.StartNew();
#if DEBUG
                Profiler.EndTimer();
#endif
                Draw();

                _renderTimeStopwatch.Stop();
                if (MaxFramerate != 0)
                {
                    Thread.Sleep(Math.Clamp(AMath.RoundToInt((1000.0 / MaxFramerate) - _renderTimeStopwatch.Elapsed.TotalMilliseconds), 0, 1000));
                }
            }

            DisposeResources();
        }

        public static void MoveToFront()
        {
            if (_window == null) return;
            _window.WindowState = WindowState.Minimized;
            Thread.Sleep(100);
            _window.WindowState = WindowState.Maximized;

        }


        public void ReloadAllShaders()
        {
            if (GraphicsDevice == null) return;
            TickScheduler.RequestTick().Wait();
            Debug.Log(LogCategory.Rendering, "RELOADING ALL SHADERS...");
            // First, lets recompile all our shaders
            ShaderManager.ClearAllShaders();
            // Next, lets dispose all drawables
            foreach (Drawable drawable in _drawables)
            {
                drawable.Dispose();
                drawable.CreateResources(GraphicsDevice);
            }
            CreateResources();
            Debug.Log(LogCategory.Rendering, "All shaders have been reloaded...");
            TickScheduler.FreeThreads();
        }

        public static void ResortDrawable(Drawable d)
        {
            // Lets just grab out the drawable out of our sorted List and add it back
            Drawable[] curDrawables = _drawables.ToArray();
            foreach (Drawable dr in curDrawables)
            {
                if (dr == d)
                {
                    _drawables.Remove(d);
                }
            }
            _drawables.AddSorted<Drawable>(d);
        }

        public static void RequestResourceCreation()
        {
            lock(_resourcesLock)
                _resourcesDirty = true;
        }
        
        private void Draw()
        {
            
            if (GraphicsDevice == null) return;
#if DEBUG
            Profiler.StartTimer(Profiler.FrameTimeType.PreRender);
#endif
            if (_resourcesDirty)
            {
                CreateResources();
            }
            // We should begin by removing any stray drawables
            foreach (Drawable d in _drawablesToRemove)
            {
                _drawables.Remove(d);
            }
            _drawablesToRemove.Clear();

            foreach (Drawable d in _drawablesToAdd)
            {
                _drawables.AddSorted<Drawable>(d);
            }
            _drawablesToAdd.Clear();

            // The first thing we need to do is call Begin() on our CommandList. Before commands can be recorded into a CommandList, this method must be called.
            ResourceFactory factory = GraphicsDevice.ResourceFactory;
            _commandList.Begin();

            // Before we can issue a Draw command, we need to set a Framebuffer.
            _commandList.SetFramebuffer(PrimaryFramebuffer);

            // At the beginning of every frame, we clear the screen to black. 
            _commandList.ClearColorTarget(0, ClearColor);

            // First we have to sort our drawables in order to perform the back-to-front render pass
            Drawable[] drawables = _drawables.ToArray();
            foreach (Drawable drawable in drawables)
            {
                if (drawable == null || drawable.transform == null)
                {
                    continue;
                }
                drawable.SetGlobalMatrix(GraphicsDevice, _windowScalingMatrix);
                drawable.SetScreenSize(GraphicsDevice, new Vector2(_window?.Width ?? 0, _window?.Height ?? 0));
                drawable.Draw(_commandList);
            }
            _commandList.InsertDebugMarker("Begin Post-Process");
            if (DoPostProcess)
            {
                foreach (var effect in PostProcessEffects)
                {
                    effect.Draw(_commandList);
                }
            }
            _commandList.InsertDebugMarker("Final Resolve shader");
            // If we're using multi-sampling we need to resolve the texture first
            if (SampleCount != TextureSampleCount.Count1)
            {
                _commandList.ResolveTexture(_finalTextureView.Target, _downSampledTextureView.Target);
            }
            _resolvePass.Draw(_commandList);
            _commandList.End();
            TickScheduler.FreeThreads(); // Everything we need should now be free for use!

            // Now that we have done that, we need to bind the resources that we created in the last section, and issue a draw call.
#if DEBUG
            Profiler.EndTimer();
            Profiler.StartTimer(Profiler.FrameTimeType.Rendering);
#endif

            GraphicsDevice.SubmitCommands(_commandList);
            GraphicsDevice.WaitForIdle();
            GraphicsDevice.SwapBuffers();
#if DEBUG
            Profiler.EndTimer();
            Profiler.SubmitTimes();
#endif
        }


        void CreateResources()
        {
            lock (_resourcesLock)
            {


                if (GraphicsDevice == null) return;
                ResourceFactory factory = GraphicsDevice.ResourceFactory;
                if (_commandList is { IsDisposed: false })
                {
                    _commandList.Dispose();
                }

                _commandList = factory.CreateCommandList();
                if (_mainColorTexture is { IsDisposed: false })
                {
                    _mainColorTexture.Dispose();
                }

                if (PrimaryFramebuffer is { IsDisposed: false })
                {
                    PrimaryFramebuffer.Dispose();
                }


                if (_mainColorView is { IsDisposed: false })
                {
                    _mainColorView.Dispose();
                }

                if (_downSampledTextureView != null && _downSampledTextureView.Target is { IsDisposed: false })
                {
                    _downSampledTextureView.Target.Dispose();
                }

                if (_downSampledTextureView is { IsDisposed: false })
                {
                    _downSampledTextureView.Dispose();
                }

                _resolvePass?.Dispose();

                MainTextureDescription = TextureDescription.Texture2D(
                    GraphicsDevice.SwapchainFramebuffer.Width,
                    GraphicsDevice.SwapchainFramebuffer.Height,
                    1,
                    1,
                    PixelFormat.R16_G16_B16_A16_Float,
                    TextureUsage.RenderTarget | TextureUsage.Sampled, SampleCount);

                _mainColorTexture = factory.CreateTexture(MainTextureDescription);
                _mainColorTexture.Name = "Primary Color Texture";
                _mainColorView = factory.CreateTextureView(_mainColorTexture);
                _mainColorView.Name = "Primary Color Texture View";
                FramebufferDescription fbDesc = new FramebufferDescription(null, _mainColorTexture);
                PrimaryFramebuffer = factory.CreateFramebuffer(ref fbDesc);
                PrimaryFramebuffer.Name = "Primary Framebuffer";

                TextureView previousView = _mainColorView;

                if (DoPostProcess)
                {
                    foreach (var effect in PostProcessEffects)
                    {
                        effect.Dispose();
                        previousView = effect.CreateResources(previousView);
                    }
                }

                _finalTextureView = previousView;
                _resolvePass = new ShaderPass<EmptyStruct>("post/resolve/shader", null);

                if (SampleCount != TextureSampleCount.Count1)
                {
                    TextureDescription downSampledTextureDescription = TextureDescription.Texture2D(
                        GraphicsDevice.SwapchainFramebuffer.Width,
                        GraphicsDevice.SwapchainFramebuffer.Height,
                        1,
                        1,
                        GraphicsDevice.SwapchainFramebuffer.ColorTargets[0].Target.Format,
                        TextureUsage.RenderTarget | TextureUsage.Sampled, TextureSampleCount.Count1);

                    _downSampledTextureView =
                        factory.CreateTextureView(factory.CreateTexture(downSampledTextureDescription));
                    _resolvePass.CreateResources(GraphicsDevice.SwapchainFramebuffer,
                        new[] { _downSampledTextureView });
                }
                else
                {
                    _resolvePass.CreateResources(GraphicsDevice.SwapchainFramebuffer, new[] { _finalTextureView });
                }

                if (_resourcesDirty)
                {
                    _resourcesDirty = false;
                }
            }
        }

        struct EmptyStruct
        {
            
        }

        private void DisposeResources()
        {
            if (GraphicsDevice == null) return;
            Debug.Log(LogCategory.Rendering, "Disposing all rendering resources");
            foreach (Drawable drawable in _drawables)
            {
                drawable.Dispose();
            }
            _mainColorTexture.Dispose();
            _mainColorView?.Dispose();
            _commandList.Dispose();
            _resolvePass?.Dispose();
            GraphicsDevice.Dispose();
            _downSampledTextureView?.Target.Dispose();
            _downSampledTextureView?.Dispose();
            foreach (var effect in PostProcessEffects)
            {
                effect.Dispose();
            }


            Debug.Log(LogCategory.Rendering, "Disposed all resources");
        }

        public static Matrix4x4 GetScalingMatrix(float width, float height)
        {
            float max = Math.Max(width, height);
            ScalingIndex = max / 1000f;

            return new Matrix4x4(
                height / max, 0, 0, 0,
                0, width / max, 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1);
        }

    }
    public static class ListExt
    {
        public static void AddSorted<T>(this List<T> @this, T item) where T : IComparable<T>
        {
            if (@this.Count == 0)
            {
                @this.Add(item);
                return;
            }
            if (@this[^1].CompareTo(item) <= 0)
            {
                @this.Add(item);
                return;
            }
            if (@this[0].CompareTo(item) >= 0)
            {
                @this.Insert(0, item);
                return;
            }
            int index = @this.BinarySearch(item);
            if (index < 0)
                index = ~index;
            @this.Insert(index, item);
        }

    }

}
