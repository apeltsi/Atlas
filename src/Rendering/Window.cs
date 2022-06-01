using System.Numerics;
using SolidCode.Caerus.ECS;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.SPIRV;
using Veldrid.StartupUtilities;

namespace SolidCode.Caerus.Rendering
{
    class Window
    {
        public static GraphicsDevice _graphicsDevice;
        private static CommandList _commandList;
        private static List<Shader> _shaders = new List<Shader>();
        private static List<Drawable> _drawables = new List<Drawable>();
        Sdl2Window window;
        Matrix4x4 WindowScalingMatrix = new Matrix4x4();
        public const int TargetFramerate = 80;
        /// <summary>
        /// Creates a new window with a title. Also initializes rendering
        /// </summary>
        public Window(string title = "Caerus " + Caerus.Version)
        {
            // TODO(amos): allow to open windows that arent borderless fullscreen, also allow changing window type at runtime
            WindowCreateInfo windowCI = new WindowCreateInfo()
            {
                X = 100,
                Y = 100,
                WindowWidth = 960,
                WindowHeight = 540,
                WindowTitle = title,
                WindowInitialState = WindowState.Hidden,
            };

            window = VeldridStartup.CreateWindow(ref windowCI);
            // Setup graphics device
            GraphicsDeviceOptions options = new GraphicsDeviceOptions
            {
                PreferStandardClipSpaceYDirection = true,
                PreferDepthRangeZeroToOne = true,
            };
            WindowScalingMatrix = GetScalingMatrix(window.Width, window.Height);
            _graphicsDevice = VeldridStartup.CreateGraphicsDevice(window, options);
            Debug.Log(LogCategories.Rendering, "Current graphics backend: " + _graphicsDevice.BackendType.ToString());
            window.Resized += () => { WindowScalingMatrix = GetScalingMatrix(window.Width, window.Height); };
            CreateResources();

            Debug.Log(LogCategories.Rendering, "Resources created!");
        }

        public void AddDrawables(List<Drawable> drawables)
        {
            _drawables.AddRange(drawables);
            Debug.Log("Added " + drawables.Count + " drawables");
        }

        public bool StartRenderLoop(EntityComponentSystem ecs)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            int frame = 0;
            bool reopen = false;
            while (window.Exists)
            {
                // TODO(amos): Fix this nasty code
                if (watch.ElapsedMilliseconds > 1000.0 / TargetFramerate)
                {
                    if (frame == 1)
                    {
                        window.Visible = true;
                        window.WindowState = WindowState.Normal;
                    }
                    InputSnapshot inputSnapshot = window.PumpEvents();
                    for (int i = 0; i < inputSnapshot.KeyEvents.Count; i++)
                    {
                        if (inputSnapshot.KeyEvents[i].Key == Key.F5 && inputSnapshot.KeyEvents[i].Down == true)
                        {
                            ReloadAllDrawables();
                        }
                    }
                    frame++;
                    ecs.Update();
                    Draw();
                    watch = System.Diagnostics.Stopwatch.StartNew();
                    Thread.Sleep((int)Math.Round(1000f / TargetFramerate));
                }
            }

            DisposeResources();
            return reopen;
        }


        public void ReloadAllDrawables()
        {
            Debug.Log(LogCategories.Rendering, "RELOADING ALL DRAWABLES...");
            // First, lets recompile all our shaders
            ShaderManager.ClearAllShaders();
            // Next, lets dispose all drawables
            foreach (Drawable drawable in _drawables)
            {
                drawable.Dispose();
                drawable.CreateResources(_graphicsDevice);
            }
            Debug.Log(LogCategories.Rendering, "All drawables have been reloaded...");
        }


        public void Draw()
        {
            // The first thing we need to do is call Begin() on our CommandList. Before commands can be recorded into a CommandList, this method must be called.
            _commandList.Begin();
            // Before we can issue a Draw command, we need to set a Framebuffer.
            _commandList.SetFramebuffer(_graphicsDevice.SwapchainFramebuffer);

            // At the beginning of every frame, we clear the screen to black. In a static scene, this is not really necessary, but I will do it anyway for demonstration.
            _commandList.ClearColorTarget(0, RgbaFloat.Black);
            foreach (Drawable drawable in _drawables)
            {
                drawable.SetGlobalMatrix(_graphicsDevice, WindowScalingMatrix);
                drawable.SetScreenSize(_graphicsDevice, new Vector2(window.Width, window.Height));
                drawable.Draw(_commandList);
            }
            _commandList.End();

            // Now that we have done that, we need to bind the resources that we created in the last section, and issue a draw call.
            _graphicsDevice.SubmitCommands(_commandList);
            _graphicsDevice.SwapBuffers();
        }


        void CreateResources()
        {
            ResourceFactory factory = _graphicsDevice.ResourceFactory;
            _commandList = factory.CreateCommandList();
        }

        private void DisposeResources()
        {
            foreach (Drawable drawable in _drawables)
            {
                drawable.Dispose();
            }
            _commandList.Dispose();
            _graphicsDevice.Dispose();
            Debug.Log(LogCategories.Rendering, "Disposed all resources");
        }

        public static Matrix4x4 GetScalingMatrix(float Width, float Height)
        {
            float max = Math.Max(Width, Height);
            return new Matrix4x4(
                Height / max, 0, 0, 0,
                0, Width / max, 0, 0,
                0, 0, 0, 0,
                0, 0, 0, 1);
        }

    }
}
