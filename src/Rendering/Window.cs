using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace SolidCode.Caerus.Rendering
{
    class Window
    {
        public Window(string title = "Caerus")
        {
            // TODO(amos): allow to open windows that arent borderless fullscreen, also allow changing window type at runtime
            var nativeWindowSettings = new NativeWindowSettings()
            {
                Size = new Vector2i(Monitors.GetPrimaryMonitor().HorizontalResolution, Monitors.GetPrimaryMonitor().VerticalResolution),
                Title = title,
                // This is needed to run on macos
                Flags = ContextFlags.ForwardCompatible,
                WindowBorder = WindowBorder.Hidden,
                CurrentMonitor = Monitors.GetPrimaryMonitor().Handle,
            };


            using (var window = new WindowInstance(GameWindowSettings.Default, nativeWindowSettings))
            {
                window.Run();
            }

        }
    }
    class WindowInstance : GameWindow
    {
        float[] vertices = {
        0.5f,  0.5f, 0.0f,  // top right
        0.5f, -0.5f, 0.0f,  // bottom right
        -0.5f, -0.5f, 0.0f,  // bottom left
        -0.5f,  0.5f, 0.0f   // top left
        };
        uint[] indices = {  // note that we start from 0!
            0, 1, 3,   // first triangle
            1, 2, 3    // second triangle
        };
        Shader? shader;
        int VertexBufferObject;
        int VertexArrayObject;
        int ElementBufferObject;
        public WindowInstance(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
        }

        protected override void OnLoad()
        {
            // Compile shaders
            shader = new Shader(Path.Join(Caerus.DataDirectory, "Shaders" + Path.DirectorySeparatorChar + "shader.vert"), Path.Join(Caerus.DataDirectory, "Shaders" + Path.DirectorySeparatorChar + "shader.frag"));

            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
            VertexBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            VertexArrayObject = GL.GenVertexArray();
            GL.BindVertexArray(VertexArrayObject);
            GL.VertexAttribPointer(shader.GetAttribLocation("aPosition"), 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            ElementBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferObject);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);


            shader.Use();
            base.OnLoad();
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            if (shader == null)
            {
                throw new NullReferenceException("Shader is null!");
            }
            GL.Clear(ClearBufferMask.ColorBufferBit);
            shader.Use();
            GL.BindVertexArray(VertexArrayObject);
            GL.DrawElements(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedInt, 0);

            SwapBuffers();
            base.OnRenderFrame(e);
        }

        protected override void OnUnload()
        {
            if (shader == null)
            {
                throw new NullReferenceException("Shader is null!");
            }
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.DeleteBuffer(VertexBufferObject);
            shader.Dispose();
            base.OnUnload();
        }
        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            // When the window gets resized, we have to call GL.Viewport to resize OpenGL's viewport to match the new size.
            // If we don't, the NDC will no longer be correct.
            GL.Viewport(0, 0, Size.X, Size.Y);
        }

        // This function runs on every update frame.
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            // Check if the Escape button is currently being pressed.
            if (KeyboardState.IsKeyDown(Keys.Escape))
            {
                // If it is, close the window.
                Close();
            }

            base.OnUpdateFrame(e);
        }
    }
}