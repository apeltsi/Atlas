using System.Numerics;
using SolidCode.Atlas.ECS;
using SolidCode.Atlas.Rendering;
using Veldrid;

namespace SolidCode.Atlas.Components;

public class InstancedSpriteRenderer : SpriteRenderer
{
    public struct InstanceData
    {
        public Vector2 InstancePosition;
        public float InstanceRotation;
        public Vector2 InstanceScale;
        public Vector4 InstanceColor;

        public InstanceData(Vector2 instancePosition, float instanceRotation, Vector2 instanceScale, Vector4 instanceColor)
        {
            InstancePosition = instancePosition;
            InstanceRotation = instanceRotation;
            InstanceScale = instanceScale;
            InstanceColor = instanceColor;
        }
    }

    private InstanceData[] _data = new[]
    {
        new InstanceData(Vector2.Zero, 0f, Vector2.One, Vector4.One),
        new InstanceData(new Vector2(0, -1f), 3.14f, Vector2.One, Vector4.One)
    };
    public List<InstanceData> Instances
    {
        get
        {
            return _data.ToList();
        }
        set
        {
            _data = value.ToArray();
            if (drawable != null)
            {
                ((InstancedDrawable<VertexPositionUV, ColorUniform, InstanceData>)drawable).UpdateInstanceData(_data);
            }
        }
    }
    
    public override Drawable[] StartRender(GraphicsDevice _graphicsDevice)
    {
        
        VertexPositionUV[] quadVertices =
        {
            new VertexPositionUV(new Vector2(-1f, 1f), new Vector4(0, 0,0,0)),
            new VertexPositionUV(new Vector2(1f, 1f), new Vector4(1, 0,0,0)),
            new VertexPositionUV(new Vector2(-1f, -1f), new Vector4(0, 1,0,0)),
            new VertexPositionUV(new Vector2(1f, -1f), new Vector4(1, 1,0,0))
        };
        ushort[] quadIndices = { 0, 1, 2, 3 };
        var layout = new VertexLayoutDescription(
            new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
            new VertexElementDescription("UV", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4));
        Mesh<VertexPositionUV> mesh = new Mesh<VertexPositionUV>(quadVertices, quadIndices, layout);
        
        
        var desc = new VertexLayoutDescription(
            new VertexElementDescription("InstancePosition", VertexElementSemantic.TextureCoordinate,
                VertexElementFormat.Float2),
            new VertexElementDescription("InstanceRotation", VertexElementSemantic.TextureCoordinate,
                VertexElementFormat.Float1),
            new VertexElementDescription("InstanceScale", VertexElementSemantic.TextureCoordinate,
                VertexElementFormat.Float2),
            new VertexElementDescription("InstanceColor", VertexElementSemantic.TextureCoordinate,
                VertexElementFormat.Float4));
        List<Rendering.Texture> textures = new List<Rendering.Texture>();
        textures.Add(this.Sprite);
        Drawable[] ret = new Drawable[1];
        drawable = new InstancedDrawable<VertexPositionUV, ColorUniform, InstanceData>("instanced-sprite/shader", mesh, entity.GetComponent<Transform>(), _data, desc, new ColorUniform(Color), ShaderStages.Fragment, textures);
        ret[0] = drawable;
        return ret;
    }
}