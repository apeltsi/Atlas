using System.Numerics;
using SolidCode.Atlas.AssetManagement;
using SolidCode.Atlas.ECS;
using SolidCode.Atlas.Rendering;
using Veldrid;
using Texture = SolidCode.Atlas.Rendering.Texture;

namespace SolidCode.Atlas.Components;

public class SpriteRenderer : RenderComponent
{
    private Color _color = Color.White;
    protected Texture _sprite = AssetManager.GetAsset<Texture>("error");
    protected Drawable drawable;
    private Mesh<VertexPositionUV> mesh;
    protected Sampler? sampler;

    public Texture Sprite
    {
        get => _sprite;
        set
        {
            _sprite = value;
            if (drawable != null) drawable.UpdateTexture(value, 0);
        }
    }

    public Color Color
    {
        get => _color;
        set
        {
            if (_color == value)
                return;
            _color = value;
            if (drawable != null) drawable.SetUniformBufferValue(Renderer.GraphicsDevice!, new ColorUniform(value));
        }
    }

    /// <summary>
    /// THIS METHOD SHOULD ONLY BE CALLED BY THE RENDERER UNLESS YOU KNOW WHAT YOU'RE DOING
    /// </summary>
    /// <param name="graphicsDevice">The graphics device</param>
    /// <returns>A drawable array</returns>
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
        mesh = new Mesh<VertexPositionUV>(quadVertices, quadIndices, layout);
        var textures = new List<Texture>();
        textures.Add(Sprite);
        DrawableOptions<VertexPositionUV, ColorUniform> options = new()
        {
            Shader = AssetManager.GetShader("sprite/shader"),
            Sampler = sampler,
            Textures = textures,
            Mesh = mesh,
            Transform = Entity.GetComponent<Transform>(true),
            Uniform = new ColorUniform(Color),
            UniformShaderStages = ShaderStages.Fragment
        };
        drawable = new Drawable<VertexPositionUV, ColorUniform>(options);
        var drawables = new Drawable[1];
        drawables[0] = drawable;
        return drawables;
    }

    protected struct ColorUniform
    {
        public Vector4 Color;

        public ColorUniform(Vector4 color)
        {
            Color = color;
        }
    }


    protected struct VertexPositionUV
    {
        public Vector2 Position; // This is the position, in normalized device coordinates.
        public Vector2 UV; // This is the color of the vertex.

        public VertexPositionUV(Vector2 position, Vector2 uv)
        {
            Position = position;
            UV = uv;
        }
    }
}