using System.Numerics;
using SolidCode.Atlas.ECS;
using SolidCode.Atlas.Input;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.SPIRV;
using Veldrid.StartupUtilities;
using static Veldrid.Sdl2.Sdl2Native;
namespace SolidCode.Atlas.Rendering
{
    public class Window
    {
        public static GraphicsDevice _graphicsDevice;
        private static CommandList _commandList;
        private static List<Shader> _shaders = new List<Shader>();
        private static List<Drawable> _drawables = new List<Drawable>();
        public static Sdl2Window window { get; protected set; }
        Matrix4x4 WindowScalingMatrix = new Matrix4x4();
        public const int TargetFramerate = 72;
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
        PostProcess[] postProcess;
        /// <summary>
        /// Time elapsed between frames, in seconds.
        /// </summary>
        public static float frameDeltaTime = 0f;
        /// <summary>
        /// What framerate the previous 60 frames were rendered in
        /// </summary>
        public static float AverageFramerate = 0f;

        private int frames = 0;
        private float frameTimes = 0f;
        public static bool reloadShaders = false;
        /// <summary>
        /// Creates a new window with a title. Also initializes rendering
        /// </summary>
        public Window(string title = "Atlas " + Atlas.Version)
        {
            WindowCreateInfo windowCI = new WindowCreateInfo()
            {
                X = 20,
                Y = 50,
                WindowWidth = 800,
                WindowHeight = 500,
                WindowTitle = title,
                WindowInitialState = WindowState.Hidden,
            };

            window = VeldridStartup.CreateWindow(ref windowCI);
            // Setup graphics device
            GraphicsDeviceOptions options = new GraphicsDeviceOptions
            {
                PreferStandardClipSpaceYDirection = true,
                SyncToVerticalBlank = TargetFramerate != 0,
                ResourceBindingModel = ResourceBindingModel.Improved,
                SwapchainSrgbFormat = false,
                SwapchainDepthFormat = null
            };
            WindowScalingMatrix = GetScalingMatrix(window.Width, window.Height);
            _graphicsDevice = VeldridStartup.CreateGraphicsDevice(window, options);
            Debug.Log(LogCategory.Rendering, "Current graphics backend: " + _graphicsDevice.BackendType.ToString());
            window.Resized += () =>
            {
                _graphicsDevice.ResizeMainWindow((uint)window.Width, (uint)window.Height);
                WindowScalingMatrix = GetScalingMatrix(window.Width, window.Height);
                CreateResources();
            };
            CreateResources();

            Debug.Log(LogCategory.Rendering, "Resources created!");
        }

        public static void SetWindowState(WindowState state)
        {
            window.WindowState = state;
        }

        public static void SetWindowTitle(string title)
        {
            window.Title = title;
        }


        public static void AddDrawables(List<Drawable> drawables)
        {
            _drawables.AddRange(drawables);
        }
        public static void RemoveDrawable(Drawable drawable)
        {
            _drawables.Remove(drawable);
        }

        public void StartRenderLoop()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            int frame = 0;
            while (window.Exists)
            {
                if (TargetFramerate == 0 || watch.ElapsedMilliseconds > 1000.0 / TargetFramerate)
                {
                    if (frame == 1)
                    {
                        window.Visible = true;
                        window.WindowState = WindowState.Normal;
                        Atlas.StartFixedUpdateLoop();
                    }
                    InputSnapshot inputSnapshot = window.PumpEvents();
                    if (reloadShaders)
                    {
                        reloadShaders = false;
                        ReloadAllShaders();
                    }
                    InputManager.ClearInputs();
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
                    Profiler.StartTimer(Profiler.FrameTimeType.Scripting);
#endif
                    EntityComponentSystem.Update();
                    frameDeltaTime = watch.ElapsedMilliseconds / 1000f;
                    frameTimes += frameDeltaTime;
                    if (frames >= 60)
                    {
                        frames = 0;
                        AverageFramerate = 1f / (frameTimes / 60f);
                        frameTimes = 0f;
                    }
                    frames++;

                    float frameRenderTime = Math.Clamp(frameDeltaTime * 1000f, 0f, 1000f / TargetFramerate);
                    watch = System.Diagnostics.Stopwatch.StartNew();
#if DEBUG
                    Profiler.EndTimer();
#endif

                    Draw();
                    if (TargetFramerate != 0)
                        Thread.Sleep((int)Math.Round(1000f / TargetFramerate - frameRenderTime));
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
            _commandList.SetFramebuffer(DuplicatorFramebuffer);

            // At the beginning of every frame, we clear the screen to black. In a static scene, this is not really necessary, but I will do it anyway for demonstration.
            _commandList.ClearColorTarget(0, ClearColor);

            // First we have to sort our drawables in order to perform the back-to-front render pass
            Drawable[] sortedDrawbles = _drawables.ToArray();
            // TODO(amos): vvv - this could be improved a lot! by sorting only when z leves change or a drawable is added
            // although for now cpu performance isn't really a problem
            Array.Sort(sortedDrawbles, Compare);
            foreach (Drawable drawable in sortedDrawbles)
            {
                if (drawable == null)
                {
                    continue;
                }
                drawable.SetGlobalMatrix(_graphicsDevice, WindowScalingMatrix);
                drawable.SetScreenSize(_graphicsDevice, new Vector2(window.Width, window.Height));
                drawable.Draw(_commandList);
            }

            _commandList.ResolveTexture(MainColorTexture, MainSceneResolvedColorTexture);

            _commandList.SetFramebuffer(_graphicsDevice.MainSwapchain.Framebuffer);
            for (int i = 0; i < postProcess.Length; i++)
            {
                postProcess[i].Draw(_commandList);
            }
            _commandList.End();

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
                PixelFormat.R16_G16_B16_A16_Float,
                TextureUsage.RenderTarget | TextureUsage.Sampled,
            TextureSampleCount.Count2);

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

            if (postProcess != null)
            {
                for (int i = 0; i < postProcess.Length; i++)
                {
                    postProcess[i].Dispose();
                }
            }
            postProcess = new PostProcess[4];

            postProcess[0] = new PostProcess(_graphicsDevice, new[] { MainSceneResolvedColorView }, "post/bright/shader", framebuffers[0]);
            postProcess[1] = new PostProcess(_graphicsDevice, new[] { ColorViews[0] }, "post/blur_horizontal/shader", framebuffers[1]);
            postProcess[2] = new PostProcess(_graphicsDevice, new[] { ColorViews[1] }, "post/blur_vertical/shader", framebuffers[2]);
            postProcess[3] = new PostProcess(_graphicsDevice, new[] { MainSceneResolvedColorView, ColorViews[2] }, "post/combine/shader");


        }

        private void DisposeResources()
        {
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

        private static int Compare(Drawable x, Drawable y)
        {
            if (x == null)
            {
                return 0;
            }
            if (y == null)
            {
                return 0;
            }
            if (x.transform == null)
            {
                return 0;
            }
            if (y.transform == null)
            {
                return 0;
            }
            return x.transform.globalZ.CompareTo(y.transform.globalZ);
        }

    }
}
