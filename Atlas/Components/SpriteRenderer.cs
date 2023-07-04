﻿namespace SolidCode.Atlas.Components;

using System.Numerics;
using AssetManagement;
using ECS;
using Rendering;
using Veldrid;

public class SpriteRenderer : RenderComponent
{
    Mesh<VertexPositionUV> mesh;
    protected Rendering.Texture _sprite = AssetManager.GetAsset<Rendering.Texture>("error");
    protected Sampler? sampler;
    public Rendering.Texture Sprite
    {
        get => _sprite;
        set
        {
            _sprite = value;
            if (drawable != null)
            {
                drawable.UpdateTexture(value, 0);
            }
        }
    }
    private Vector4 _color = Vector4.One;
    protected Drawable drawable;
    public Vector4 Color
    {
        get
        {
            return _color;
        }
        set
        {
            if (_color == value)
                return;
            _color = value;
            if (drawable != null)
            {
                drawable.SetUniformBufferValue(Renderer.GraphicsDevice, new ColorUniform(value));
            }
        }
    }
    public override Drawable[] StartRender(GraphicsDevice _graphicsDevice)
    {
        VertexPositionUV[] quadVertices =
               {
                new VertexPositionUV(new Vector2(-1f, 1f), new Vector2(0, 0)),
                new VertexPositionUV(new Vector2(1f, 1f), new Vector2(1, 0)),
                new VertexPositionUV(new Vector2(-1f, -1f), new Vector2(0, 1)),
                new VertexPositionUV(new Vector2(1f, -1f), new Vector2(1, 1))
        };
        ushort[] quadIndices = { 0, 1, 2, 3 };
        var layout = new VertexLayoutDescription(
                        new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                        new VertexElementDescription("UV", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2));
        mesh = new Mesh<VertexPositionUV>(quadVertices, quadIndices, layout);
        List<SolidCode.Atlas.Rendering.Texture> textures = new List<SolidCode.Atlas.Rendering.Texture>();
        textures.Add(Sprite);
        DrawableOptions<VertexPositionUV, ColorUniform> options = new ()
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
        List<Drawable> drawables = new List<Drawable>();
        drawables.Add(drawable);
        return drawables.ToArray();
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