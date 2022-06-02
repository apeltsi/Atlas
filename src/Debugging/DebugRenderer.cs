using System.Numerics;
using SolidCode.Caerus;
using SolidCode.Caerus.Components;
using SolidCode.Caerus.ECS;
using SolidCode.Caerus.Rendering;
using Veldrid;

public class DebugRenderer : RenderComponent
{
    int fixedUpdates = 0;
    Mesh<VertexPositionUV> mesh;
    public override Drawable[] StartRender(GraphicsDevice _graphicsDevice)
    {
        VertexPositionUV[] quadVertices =
               {
                new VertexPositionUV(new Vector2(-0.75f, 0.75f), new Vector4(0, 0,0,0)),
                new VertexPositionUV(new Vector2(0.75f, 0.75f), new Vector4(1, 0,0,0)),
                new VertexPositionUV(new Vector2(-0.75f, -0.75f), new Vector4(0, 1,0,0)),
                new VertexPositionUV(new Vector2(0.75f, -0.75f), new Vector4(1, 1,0,0))
        };
        ushort[] quadIndices = { 0, 1, 2, 3 };
        var layout = new VertexLayoutDescription(
                        new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                        new VertexElementDescription("Color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4));
        mesh = new Mesh<VertexPositionUV>(quadVertices, quadIndices, layout);
        List<string> textures = new List<string>();
        textures.Add("infile");
        var drawable = new Drawable<VertexPositionUV>(_graphicsDevice, "bg/shader", mesh, entity.GetComponent<Transform>(), null, textures);
        List<Drawable> drawables = new List<Drawable>();
        drawables.Add(drawable);
        return drawables.ToArray();
    }

    public override void Update()
    {
    }
    public override void FixedUpdate()
    {

    }

    struct VertexPositionUV
    {
        public Vector2 Position; // This is the position, in normalized device coordinates.
        public Vector4 UV; // This is the color of the vertex.
        public VertexPositionUV(Vector2 position, Vector4 uv)
        {
            Position = position;
            UV = uv;
        }
        public const uint SizeInBytes = 24;
    }

}