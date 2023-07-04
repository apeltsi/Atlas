using System.Numerics;
using SolidCode.Atlas.AssetManagement;
using SolidCode.Atlas.Components;
using SolidCode.Atlas.ECS;
using SolidCode.Atlas.Rendering;
using Veldrid;
using Texture = SolidCode.Atlas.Rendering.Texture;

namespace SolidCode.Atlas
{
    public static class Debug
    {
        internal static bool LogsInitialized { get; private set; } = false;
        internal static void CheckLog()
        {
            if (!LogsInitialized)
            {
                LogsInitialized = true;
                Atlas.InitializeLogging();
            }
        }
        public static void Log(params string[] log)
        {
            Log(LogCategory.General, log);
        }
        public static void Log<T>(T category, params string[] log) where T : IComparable, IFormattable, IConvertible
        {
            CheckLog();
            Telescope.Debug.Log<T>(category, log);
        }
        public static void Warning(params string[] log)
        {
            Warning(LogCategory.General, log);
        }
        public static void Warning<T>(T category, params string[] log) where T : IComparable, IFormattable, IConvertible
        {
            CheckLog();
            Telescope.Debug.Warning<T>(category, log);
        }
        public static void Error(params string[] log)
        {
            Error(LogCategory.General, log);
        }
        public static void Error<T>(T category, params string[] log) where T : IComparable, IFormattable, IConvertible
        {
            CheckLog();
            Telescope.Debug.Error<T>(category, log);
        }

        private static List<DebugMarker> _markers = new ();
        /// <summary>
        /// Places a debug marker at the specified position
        /// (NOT VISIBLE IN A RELEASE BUILD)
        /// </summary>
        /// <param name="position">Position of marker</param>
        /// <param name="color">Color of marker</param>
        public static void Marker(Vector2 position, Vector4? color = null)
        {
#if DEBUG
            if(color == null)
                color = new Vector4(0, 0, 1, 1);
            _markers.Add(new DebugMarker(position, color.Value));
#endif
        }

        internal static void RenderMarkers(CommandList cl, Matrix4x4 scalingMatrix)
        {
            foreach (var marker in _markers)
            {
                Transform tr = new Transform();
                tr.Position = marker.Position;
                tr.Layer = 0;
                tr.Scale = Renderer.PixelScale * 4f / Camera.GetScaling();
                var layout = new VertexLayoutDescription(
                    new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                    new VertexElementDescription("UV", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2));

                DrawableOptions<VertexPositionUV, ColorUniform> options = new()
                {
                    Shader = AssetManager.GetShader("color/shader")!,
                    Mesh = new Mesh<VertexPositionUV>(new []
                    {
                        new VertexPositionUV(new Vector2(-1f, 1f), new Vector2(0, 0)),
                        new VertexPositionUV(new Vector2(1f, 1f), new Vector2(1, 0)),
                        new VertexPositionUV(new Vector2(-1f, -1f), new Vector2(0, 1)),
                        new VertexPositionUV(new Vector2(1f, -1f), new Vector2(1, 1))
                    }, new ushort[] { 0, 1, 2, 3 }, layout),
                    Transform = tr,
                    Uniform = new ColorUniform(marker.Color),
                    UniformShaderStages = ShaderStages.Fragment,
                    Textures = new List<Texture>(),
                    TransformShaderStages = ShaderStages.Vertex
                };
                Drawable d = new Drawable<VertexPositionUV, ColorUniform>(options);
                d.SetGlobalMatrix(Renderer.GraphicsDevice!, scalingMatrix);
                d.SetScreenSize(Renderer.GraphicsDevice!, Renderer.RenderResolution);
                d.Draw(cl);
            }
            _markers.Clear();
        }
        
        private struct ColorUniform
        {
            public Vector4 Color;

            public ColorUniform(Vector4 color)
            {
                Color = color;
            }
        }


        private struct VertexPositionUV
        {
            public Vector2 Position; // This is the position, in normalized device coordinates.
            public Vector2 UV; // This is the color of the vertex.
            public VertexPositionUV(Vector2 position, Vector2 uv)
            {
                Position = position;
                UV = uv;
            }
        }


        internal class DebugMarker
        {
            public Vector2 Position;
            public Vector4 Color;

            public DebugMarker(Vector2 position, Vector4 color)
            {
                Position = position;
                Color = color;
            }
        }

    }
}