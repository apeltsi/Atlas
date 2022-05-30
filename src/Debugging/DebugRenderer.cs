using System.Numerics;
using SolidCode.Caerus;
using SolidCode.Caerus.Components;
using SolidCode.Caerus.ECS;
using SolidCode.Caerus.Rendering;
using Veldrid;

public class DebugRenderer : RenderComponent
{
    int fixedUpdates = 0;
    Mesh<VertexPositionColor> mesh;
    public override Drawable[] StartRender(GraphicsDevice _graphicsDevice)
    {
        VertexPositionColor[] quadVertices =
               {
                new VertexPositionColor(new Vector2(-0.75f, 0.75f), RgbaFloat.Red),
                new VertexPositionColor(new Vector2(0.75f, 0.75f), RgbaFloat.Green),
                new VertexPositionColor(new Vector2(-0.75f, -0.75f), RgbaFloat.Blue),
                new VertexPositionColor(new Vector2(0.75f, -0.75f), RgbaFloat.Yellow)
        };
        ushort[] quadIndices = { 0, 1, 2, 3 };
        var layout = new VertexLayoutDescription(
                        new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                        new VertexElementDescription("Color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4));
        mesh = new Mesh<VertexPositionColor>(quadVertices, quadIndices, layout);
        var drawable = new Drawable<VertexPositionColor>(_graphicsDevice, ShaderManager.GetShader("shader"), mesh, entity.GetComponent<Transform>());
        Debug.Log("im still alive");
        List<Drawable> drawables = new List<Drawable>();
        Debug.Log("after all this");
        drawables.Add(drawable);
        Debug.Log("returning drawable");
        return drawables.ToArray();
    }

    public override void Update()
    {
        Debug.Log("Update time!");
    }
    public override void FixedUpdate()
    {

    }

    struct VertexPositionColor
    {
        public Vector2 Position; // This is the position, in normalized device coordinates.
        public RgbaFloat Color; // This is the color of the vertex.
        public VertexPositionColor(Vector2 position, RgbaFloat color)
        {
            Position = position;
            Color = color;
        }
        public const uint SizeInBytes = 24;
    }

}