using System.Numerics;
using SolidCode.Atlas.AssetManagement;
using SolidCode.Atlas.ECS;
using SolidCode.Atlas.Rendering;
using Veldrid;
using Texture = SolidCode.Atlas.Rendering.Texture;

namespace SolidCode.Atlas.Components;

public class InstancedSpriteRenderer : SpriteRenderer
{
    private InstanceData[] _data =
    {
        new(Vector2.Zero, 0f, Vector2.One, Vector4.One)
    };

    public InstanceData[] Instances
    {
        get => _data;
        set
        {
            _data = value;
            UpdateData();
        }
    }

    public void UpdateData()
    {
        if (drawable != null)
            ((InstancedDrawable<VertexPositionUV, ColorUniform, InstanceData>)drawable).UpdateInstanceData(_data);
    }

    /// <summary>
    /// THIS METHOD SHOULD ONLY BE CALLED BY THE RENDERER UNLESS YOU KNOW WHAT YOU'RE DOING
    /// </summary>
    /// <param name="graphicsDevice"> The graphics device </param>
    /// <returns> A drawable array </returns>
    public override Drawable[] StartRender(GraphicsDevice graphicsDevice)
    {
        AssetManager.RequireBuiltinAssets();

        VertexPositionUV[] quadVertices =
        {
            new(new Vector2(-1f, 1f), new Vector2(0, 0)),
            new(new Vector2(1f, 1f), new Vector2(1, 0)),
            new(new Vector2(-1f, -1f), new Vector2(0, 1)),
            new(new Vector2(1f, -1f), new Vector2(1, 1))
        };
        ushort[] quadIndices = { 0, 1, 2, 3 };
        var layout = new VertexLayoutDescription(
            new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate,
                VertexElementFormat.Float2),
            new VertexElementDescription("UV", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2));
        var mesh = new Mesh<VertexPositionUV>(quadVertices, quadIndices, layout);


        var desc = new VertexLayoutDescription(
            new VertexElementDescription("InstancePosition", VertexElementSemantic.TextureCoordinate,
                VertexElementFormat.Float2),
            new VertexElementDescription("InstanceRotation", VertexElementSemantic.TextureCoordinate,
                VertexElementFormat.Float1),
            new VertexElementDescription("InstanceScale", VertexElementSemantic.TextureCoordinate,
                VertexElementFormat.Float2),
            new VertexElementDescription("InstanceColor", VertexElementSemantic.TextureCoordinate,
                VertexElementFormat.Float4));
        var textures = new List<Texture>();
        textures.Add(Sprite);
        var ret = new Drawable[1];
        InstancedDrawableOptions<VertexPositionUV, ColorUniform, InstanceData> options = new()
        {
            Shader = AssetManager.GetShader("instanced-sprite/shader"),
            Sampler = sampler,
            Textures = textures,
            Mesh = mesh,
            Transform = Entity.GetComponent<Transform>(true),
            Uniform = new ColorUniform(Color),
            UniformShaderStages = ShaderStages.Fragment,
            InstancedData = _data,
            InstanceLayoutDescription = desc
        };

        drawable = new InstancedDrawable<VertexPositionUV, ColorUniform, InstanceData>(options);
        ret[0] = drawable;
        return ret;
    }

    public struct InstanceData
    {
        public Vector2 InstancePosition;
        public float InstanceRotation;
        public Vector2 InstanceScale;
        public Color InstanceColor;

        public InstanceData(Vector2 instancePosition, float instanceRotation, Vector2 instanceScale,
            Color instanceColor)
        {
            InstancePosition = instancePosition;
            InstanceRotation = instanceRotation;
            InstanceScale = instanceScale;
            InstanceColor = instanceColor;
        }
    }
}