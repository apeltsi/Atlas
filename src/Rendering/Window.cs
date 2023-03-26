using System.Numerics;
using SolidCode.Atlas.ECS;
using SolidCode.Atlas.Input;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using System.Collections.Concurrent;
using SolidCode.Atlas.Mathematics;
using SolidCode.Atlas.AssetManagement;

namespace SolidCode.Atlas.Rendering
{

    public class Window
    {

        public static GraphicsDevice _graphicsDevice;
        private static CommandList _commandList;
        private static List<Shader> _shaders = new List<Shader>();
        private static List<Drawable> _drawables = new List<Drawable>();
        protected static Sdl2Window window;
        Matrix4x4 WindowScalingMatrix = new Matrix4x4();
        public static int MaxFramerate = 0;
        public static Framebuffer DuplicatorFramebuffer { get; protected set; }
        Veldrid.Texture MainColorTexture;
        Veldrid.Texture MainDepthTexture;
        Veldrid.Texture MainSceneResolvedColorTexture;
        public static Veldrid.TextureView MainSceneResolvedColorView { get; protected set; }
        Veldrid.Texture[] ColorTextures = new Veldrid.Texture[0];
        public static Veldrid.TextureView[] ColorViews = new Veldrid.TextureView[0];
        Veldrid.Framebuffer[] framebuffers = new Veldrid.Framebuffer[0];
        public static Vector2 MousePosition = Vector2.Zero;
        public static RgbaFloat ClearColor = RgbaFloat.Black;
        public static bool DoPostProcess = true;
        PostProcess[] postProcess;
        /// <summary>
        /// What framerate the previous 60 frames were rendered in
        /// </summary>
        public static float AverageFramerate = 0f;

        private int frames = 0;
        private float frameTimes = 0f;
        public static bool reloadShaders = false;
        static ConcurrentBag<Drawable> drawablesToAdd = new ConcurrentBag<Drawable>();
        static ConcurrentBag<Drawable> drawablesToRemove = new ConcurrentBag<Drawable>();

        public static string Title
        {
            get
            {
                if (window == null)
                {
                    return "";
                }
                return window.Title;
            }
            set
            {
                if (window != null)
                    window.Title = value + " | Atlas/" + Atlas.Version;
            }
        }

        public static WindowState State
        {
            get
            {
                if (window == null)
                {
                    return WindowState.Hidden;
                }
                return window.WindowState;
            }
            set
            {
                if (window != null)
                    window.WindowState = value;
            }
        }

        public static bool Focused
        {
            get
            {
                if (window == null)
                {
                    return false;
                }
                return window.Focused;
            }
        }

        public static bool Resizable
        {
            get
            {
                if (window == null)
                {
                    return false;
                }
                return window.Resizable;
            }
            set
            {
                if (window != null)
                {
                    window.Resizable = value;
                }
            }
        }
        private static bool positionDirty = false;
        private static Vector2 _position = new Vector2(50, 50);
        public static Vector2 Position
        {
            get
            {
                if (window == null)
                {
                    return Vector2.Zero;
                }
                unsafe
                {
                    int x = 0;
                    int y = 0;
                    Sdl2Native.SDL_GetWindowPosition(window.SdlWindowHandle, &x, &y);
                    return new Vector2(x, y);
                }
            }
            set
            {
                _position = value;
                positionDirty = true;
            }
        }
        private static bool sizeDirty = false;
        private static Vector2 _size = new Vector2(800, 500);
        public static Vector2 Size
        {
            get
            {
                if (window == null)
                {
                    return Vector2.Zero;
                }

                return _size;
            }
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
                return window.CursorVisible;
            }
            set
            {
                window.CursorVisible = value;
            }
        }

        /// <summary>
        /// Creates a new window with a title. Also initializes rendering
        /// </summary>
        internal Window(string title = "Atlas/" + Atlas.Version, SDL_WindowFlags flags = 0)
        {
            WindowCreateInfo windowCI = new WindowCreateInfo()
            {
                X = AMath.RoundToInt(_position.X),
                Y = AMath.RoundToInt(_position.Y),
                WindowWidth = AMath.RoundToInt(_size.X),
                WindowHeight = AMath.RoundToInt(_size.Y),
                WindowTitle = title + " | Atlas/" + Atlas.Version,
                WindowInitialState = WindowState.Hidden
            };

            window = CreateWindow.CreateWindowWithFlags(ref windowCI, flags);
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

            WindowScalingMatrix = GetScalingMatrix(window.Width, window.Height);
            _graphicsDevice = VeldridStartup.CreateGraphicsDevice(window, options);
            Debug.Log(LogCategory.Rendering, "Current graphics backend: " + _graphicsDevice.BackendType.ToString());

            // We have to load our builtin shaders now
            AssetPack builtinAssets = new AssetPack("atlas");
            builtinAssets.LoadAtlasAssetpack();

            if (_graphicsDevice.BackendType == GraphicsBackend.Vulkan)
            {
                DoPostProcess = false;
                Debug.Warning(LogCategory.Rendering, "Post-Processing support for Vulkan has not been implemented yet.");
            }
            window.Resized += () =>
            {
                _graphicsDevice.ResizeMainWindow((uint)window.Width, (uint)window.Height);
                WindowScalingMatrix = GetScalingMatrix(window.Width, window.Height);
                CreateResources();
            };

            CreateResources();
        }

        public static void Close()
        {
            window.Close();
        }

        public static void AddDrawables(List<Drawable> drawables)
        {
            foreach (Drawable d in drawables)
            {
                drawablesToAdd.Add(d);
            }
        }
        public static void RemoveDrawable(Drawable drawable)
        {
            drawablesToRemove.Add(drawable);
        }

        private System.Diagnostics.Stopwatch renderTimeStopwatch = new System.Diagnostics.Stopwatch();
        internal void StartRenderLoop()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            int frame = 0;

            while (window.Exists)
            {
                renderTimeStopwatch.Restart();
                if (frame == 1)
                {
                    window.Visible = true;
                    window.WindowState = WindowState.Normal;
                    Atlas.StartTickLoop();
                }
                if (frame == 2)
                {
                    Debug.Log(LogCategory.Rendering, "First frame has been rendered. Rendering frame 2");
                }
                InputSnapshot inputSnapshot = window.PumpEvents();

                if (reloadShaders)
                {
                    reloadShaders = false;
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
                if (positionDirty)
                {
                    Sdl2Native.SDL_SetWindowPosition(window.SdlWindowHandle, AMath.RoundToInt(_position.X), AMath.RoundToInt(_position.Y));
                    positionDirty = false;
                }
                if (sizeDirty)
                {
                    window.Width = AMath.RoundToInt(_size.X);
                    window.Height = AMath.RoundToInt(_size.Y);
                    sizeDirty = false;
                }


                EntityComponentSystem.Update();
                // Update time
                Time.deltaTime = watch.Elapsed.TotalSeconds;
                Time.time = Atlas.ecsStopwatch.Elapsed.TotalSeconds;

                frameTimes += (float)Time.deltaTime;
                if (frames >= 60)
                {
                    frames = 0;
                    AverageFramerate = 1f / (frameTimes / 60f);
                    frameTimes = 0f;
                }
                frames++;

                watch = System.Diagnostics.Stopwatch.StartNew();
#if DEBUG
                Profiler.EndTimer();
#endif
                Draw();

                renderTimeStopwatch.Stop();
                if (MaxFramerate != 0)
                {
                    Thread.Sleep(Math.Clamp(AMath.RoundToInt((1000.0 / MaxFramerate) - renderTimeStopwatch.Elapsed.TotalMilliseconds), 0, 1000));
                }
            }

            DisposeResources();
        }

        public static void MoveToFront()
        {
            window.WindowState = WindowState.Minimized;
            Thread.Sleep(100);
            window.WindowState = WindowState.Maximized;

        }


        public void ReloadAllShaders()
        {
            TickScheduler.RequestTick().Wait();
            Debug.Log(LogCategory.Rendering, "RELOADING ALL SHADERS...");
            // First, lets recompile all our shaders
            ShaderManager.ClearAllShaders();
            // Next, lets dispose all drawables
            foreach (Drawable drawable in _drawables)
            {
                drawable.Dispose();
                drawable.CreateResources(_graphicsDevice);
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


        public static CommandList GetCommandList()
        {
            return _commandList;
        }

        public void Draw()
        {
#if DEBUG
            Profiler.StartTimer(Profiler.FrameTimeType.PreRender);
#endif
            // We should begin by removing any stray drawables
            foreach (Drawable d in drawablesToRemove)
            {
                _drawables.Remove(d);
            }
            drawablesToRemove.Clear();

            foreach (Drawable d in drawablesToAdd)
            {
                _drawables.AddSorted<Drawable>(d);
            }
            drawablesToAdd.Clear();

            // The first thing we need to do is call Begin() on our CommandList. Before commands can be recorded into a CommandList, this method must be called.
            ResourceFactory factory = _graphicsDevice.ResourceFactory;
            _commandList.Begin();
            // Lets start by clearing all of our framebuffers
            for (int i = 0; i < framebuffers.Length; i++)
            {
                _commandList.SetFramebuffer(framebuffers[i]);
                _commandList.ClearColorTarget(0, RgbaFloat.Clear);
            }
            // Before we can issue a Draw command, we need to set a Framebuffer.
            if (!DoPostProcess)
                _commandList.SetFramebuffer(_graphicsDevice.MainSwapchain.Framebuffer);
            else
                _commandList.SetFramebuffer(DuplicatorFramebuffer);

            // At the beginning of every frame, we clear the screen to black. In a static scene, this is not really necessary, but I will do it anyway for demonstration.
            _commandList.ClearColorTarget(0, ClearColor);

            // First we have to sort our drawables in order to perform the back-to-front render pass
            Drawable[] sortedDrawbles = _drawables.ToArray();
            foreach (Drawable drawable in sortedDrawbles)
            {
                if (drawable == null || drawable.transform == null)
                {
                    continue;
                }
                drawable.SetGlobalMatrix(_graphicsDevice, WindowScalingMatrix);
                drawable.SetScreenSize(_graphicsDevice, new Vector2(window.Width, window.Height));
                drawable.Draw(_commandList);
            }
            if (DoPostProcess)
            {
                _commandList.ResolveTexture(MainColorTexture, MainSceneResolvedColorTexture);

                _commandList.SetFramebuffer(_graphicsDevice.MainSwapchain.Framebuffer);
                for (int i = 0; i < postProcess.Length; i++)
                {
                    postProcess[i].Draw(_commandList);
                }

            }
            _commandList.End();
            TickScheduler.FreeThreads(); // Everything we need should now be free for use!

            // Now that we have done that, we need to bind the resources that we created in the last section, and issue a draw call.
#if DEBUG
            Profiler.EndTimer();
            Profiler.StartTimer(Profiler.FrameTimeType.Rendering);
#endif

            _graphicsDevice.SubmitCommands(_commandList);
            _graphicsDevice.WaitForIdle();
            _graphicsDevice.SwapBuffers();
#if DEBUG
            Profiler.EndTimer();
            Profiler.SubmitTimes();
#endif
        }


        void CreateResources()
        {
            ResourceFactory factory = _graphicsDevice.ResourceFactory;
            if (_commandList != null && !_commandList.IsDisposed)
            {
                _commandList.Dispose();
            }
            _commandList = factory.CreateCommandList();
            if (MainColorTexture != null && !MainColorTexture.IsDisposed)
            {
                MainColorTexture.Dispose();
            }
            if (MainSceneResolvedColorTexture != null && !MainSceneResolvedColorTexture.IsDisposed)
            {
                MainSceneResolvedColorTexture.Dispose();
            }
            if (DuplicatorFramebuffer != null && !DuplicatorFramebuffer.IsDisposed)
            {
                DuplicatorFramebuffer.Dispose();
            }
            if (MainSceneResolvedColorView != null && !MainSceneResolvedColorView.IsDisposed)
            {
                MainSceneResolvedColorView.Dispose();
            }
            for (int i = 0; i < ColorTextures.Length; i++)
            {
                ColorTextures[i].Dispose();
            }
            for (int i = 0; i < ColorViews.Length; i++)
            {
                ColorViews[i].Dispose();
            }
            for (int i = 0; i < framebuffers.Length; i++)
            {
                framebuffers[i].Dispose();
            }
            TextureDescription mainColorDesc = TextureDescription.Texture2D(
                _graphicsDevice.SwapchainFramebuffer.Width,
                _graphicsDevice.SwapchainFramebuffer.Height,
                1,
                1,
                DoPostProcess ? PixelFormat.R16_G16_B16_A16_Float : _graphicsDevice.MainSwapchain.Framebuffer.ColorTargets[0].Target.Format,
                TextureUsage.RenderTarget | TextureUsage.Sampled,
            DoPostProcess ? TextureSampleCount.Count2 : TextureSampleCount.Count1);

            MainColorTexture = factory.CreateTexture(mainColorDesc);

            mainColorDesc.SampleCount = TextureSampleCount.Count1; // Reset the sample count for the target texture (the texture that is rendered on screen cant be multisampeled)
            MainSceneResolvedColorTexture = factory.CreateTexture(ref mainColorDesc);
            MainSceneResolvedColorView = factory.CreateTextureView(MainSceneResolvedColorTexture);
            FramebufferDescription fbDesc = new FramebufferDescription(null, MainColorTexture);
            DuplicatorFramebuffer = factory.CreateFramebuffer(ref fbDesc);

            // A texture with all the bright pixels
            ColorTextures = new Veldrid.Texture[3];
            ColorViews = new Veldrid.TextureView[3];
            framebuffers = new Veldrid.Framebuffer[3];

            ColorTextures[0] = factory.CreateTexture(mainColorDesc);
            ColorViews[0] = factory.CreateTextureView(ColorTextures[0]);
            FramebufferDescription desc = new FramebufferDescription(null, ColorTextures[0]);

            framebuffers[0] = factory.CreateFramebuffer(ref desc);


            ColorTextures[1] = factory.CreateTexture(mainColorDesc);
            ColorViews[1] = factory.CreateTextureView(ColorTextures[1]);
            FramebufferDescription desc2 = new FramebufferDescription(null, ColorTextures[1]);

            framebuffers[1] = factory.CreateFramebuffer(ref desc2);


            ColorTextures[2] = factory.CreateTexture(mainColorDesc);
            ColorViews[2] = factory.CreateTextureView(ColorTextures[2]);
            FramebufferDescription desc3 = new FramebufferDescription(null, ColorTextures[2]);

            framebuffers[2] = factory.CreateFramebuffer(desc3);

            if (postProcess != null && DoPostProcess)
            {
                for (int i = 0; i < postProcess.Length; i++)
                {
                    postProcess[i].Dispose();
                }
            }
            postProcess = new PostProcess[4];
            if (DoPostProcess)
            {
                postProcess[0] = new PostProcess(_graphicsDevice, new[] { MainSceneResolvedColorView }, "post/bright/shader", framebuffers[0]);
                postProcess[1] = new PostProcess(_graphicsDevice, new[] { ColorViews[0] }, "post/blur_horizontal/shader", framebuffers[1]);
                postProcess[2] = new PostProcess(_graphicsDevice, new[] { ColorViews[1] }, "post/blur_vertical/shader", framebuffers[2]);
                postProcess[3] = new PostProcess(_graphicsDevice, new[] { MainSceneResolvedColorView, ColorViews[2] }, "post/combine/shader");
            }
        }

        private void DisposeResources()
        {
            Debug.Log(LogCategory.Rendering, "Disposing all rendering resources");
            foreach (Drawable drawable in _drawables)
            {
                drawable.Dispose();
            }
            MainColorTexture.Dispose();
            MainSceneResolvedColorTexture.Dispose();
            MainSceneResolvedColorView.Dispose();
            _commandList.Dispose();
            _graphicsDevice.Dispose();
            for (int i = 0; i < postProcess.Length; i++)
            {
                postProcess[i].Dispose();
            }
            for (int i = 0; i < ColorTextures.Length; i++)
            {
                ColorTextures[i].Dispose();
            }
            for (int i = 0; i < ColorViews.Length; i++)
            {
                ColorViews[i].Dispose();
            }
            for (int i = 0; i < framebuffers.Length; i++)
            {
                framebuffers[i].Dispose();
            }

            Debug.Log(LogCategory.Rendering, "Disposed all resources");
        }

        public static Matrix4x4 GetScalingMatrix(float Width, float Height)
        {
            float max = Math.Max(Width, Height);
            return new Matrix4x4(
                Height / max, 0, 0, 0,
                0, Width / max, 0, 0,
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
            if (@this[@this.Count - 1].CompareTo(item) <= 0)
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
