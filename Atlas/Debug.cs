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
        private static List<DebugLine> _lines = new();
        /// <summary>
        /// Places a debug marker at the specified position
        /// (NOT VISIBLE IN A RELEASE BUILD)
        /// </summary>
        /// <param name="position">Position of marker</param>
        /// <param name="color">Color of marker</param>
        public static void Marker(Vector2 position, Vector4? color = null, float scale = 6f)
        {
#if DEBUG
            if(color == null)
                color = new Vector4(0, 0, 1, 1);
            _markers.Add(new DebugMarker(position, color.Value, scale));
#endif
        }

        /// <summary>
        /// Draws a debug line from start to end.
        /// (NOT VISIBLE IN A RELEASE BUILD)
        /// </summary>
        /// <param name="start">Starting point</param>
        /// <param name="end">Ending Point</param>
        /// <param name="color">Color</param>
        /// <param name="width">Width of the line</param>
        public static void Line(Vector2 start, Vector2 end, Vector4? color = null, float width = 4f)
        {
#if DEBUG
            if(color == null)
                color = new Vector4(0, 0, 1, 1);
            _lines.Add(new DebugLine(start, end, color.Value, width));
#endif
        }

        internal static void RenderMarkers(CommandList cl, Matrix4x4 scalingMatrix)
        {
            var layout = new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2));
            Vector2 cameraScaling = Camera.GetScaling();
            foreach (var marker in _markers)
            {
                Transform tr = new Transform();
                tr.Position = marker.Position;
                tr.Layer = 0;
                tr.Scale = Renderer.PixelScale * marker.Scale / cameraScaling;

                DrawableOptions<VertexPosition, ColorUniform> options = new()
                {
                    Shader = AssetManager.GetShader("color/shader")!,
                    Mesh = new Mesh<VertexPosition>(new []
                    {
                        new VertexPosition(new Vector2(-1f, 1f)),
                        new VertexPosition(new Vector2(1f, 1f)),
                        new VertexPosition(new Vector2(-1f, -1f)),
                        new VertexPosition(new Vector2(1f, -1f))
                    }, new ushort[] { 0, 1, 2, 3 }, layout),
                    Transform = tr,
                    Uniform = new ColorUniform(marker.Color),
                    UniformShaderStages = ShaderStages.Fragment,
                    Textures = new List<Texture>(),
                    TransformShaderStages = ShaderStages.Vertex
                };
                Drawable d = new Drawable<VertexPosition, ColorUniform>(options);
                // Lets remove the offset from our matrix
                d.SetGlobalMatrix(Renderer.GraphicsDevice!, scalingMatrix);
                d.SetScreenSize(Renderer.GraphicsDevice!, Renderer.RenderResolution);
                d.Draw(cl);
            }
            _markers.Clear();
            
            // Next lets render our lines
            foreach (var line in _lines)
            {
                Transform tr = new Transform();
                tr.Position = line.start;
                tr.Layer = 0;

                // Lets calculate a vector from start to end
                Vector2 diff = line.end - line.start;
                // To render our line we want to have a vector that is pointing 90 degrees to the right of our vector from our starting point
                // To do this we can use the cross product of our vector and the vector pointing up (0, 1)
                Vector2 norm = Vector2.Normalize(diff);
                Vector2 scaling = cameraScaling;
                Vector2 right = new Vector2(-norm.Y, norm.X) * line.Width * Renderer.ScalingIndex / scaling / 1000f;
                Vector2 left = new Vector2(norm.Y, -norm.X) * line.Width * Renderer.ScalingIndex / scaling / 1000f;
                

                DrawableOptions<VertexPosition, ColorUniform> options = new()
                {
                    Shader = AssetManager.GetShader("color/shader")!,
                    Mesh = new Mesh<VertexPosition>(new []
                    {
                        new VertexPosition(left),
                        new VertexPosition(right),
                        new VertexPosition(diff + left),
                        new VertexPosition(diff + right),
                    }, new ushort[] { 0, 1, 2, 3 }, layout),
                    Transform = tr,
                    Uniform = new ColorUniform(line.Color),
                    UniformShaderStages = ShaderStages.Fragment,
                    Textures = new List<Texture>(),
                    TransformShaderStages = ShaderStages.Vertex
                };
                Drawable d = new Drawable<VertexPosition, ColorUniform>(options);
                d.SetGlobalMatrix(Renderer.GraphicsDevice!, scalingMatrix);
                d.SetScreenSize(Renderer.GraphicsDevice!, Renderer.RenderResolution);
                d.Draw(cl);
            }
            _lines.Clear();
        }
        
        private struct ColorUniform
        {
            public Vector4 Color;

            public ColorUniform(Vector4 color)
            {
                Color = color;
            }
        }


        private struct VertexPosition
        {
            public Vector2 Position;
            public VertexPosition(Vector2 position)
            {
                Position = position;
            }
        }


        internal class DebugMarker
        {
            public Vector2 Position;
            public Vector4 Color;
            public float Scale;

            public DebugMarker(Vector2 position, Vector4 color, float scale)
            {
                Position = position;
                Color = color;
                Scale = scale;
            }
        }

        internal class DebugLine
        {
            public Vector2 start;
            public Vector2 end;
            public Vector4 Color;
            public float Width;
            
            public DebugLine(Vector2 start, Vector2 end, Vector4 color, float width)
            {
                this.start = start;
                this.end = end;
                Color = color;
                Width = width;
            }
        }

    }
}