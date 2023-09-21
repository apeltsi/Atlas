using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using FontStashSharp;
using FontStashSharp.Interfaces;
using SolidCode.Atlas.AssetManagement;
using SolidCode.Atlas.Components;
using SolidCode.Atlas.ECS;
using SolidCode.Atlas.UI;
using Veldrid;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;

namespace SolidCode.Atlas.Rendering;

public class TextDrawable : Drawable
{
    private Vector4 _color;
    private bool _dirty;
    private FontSet _fontSet;
    private TextAlignment _horizontalAlignment;
    private Matrix4x4 _lastMatrix;
    private Vector2 _lastScaleEval = Vector2.Zero;
    private FontRenderer _renderer;
    private float _resolutionFactor;
    private float _size;
    private string _text;
    private bool _transformDirty;
    private TextVerticalAlignment _verticalAlignment;

    public TextDrawable(string text, FontSet fonts, Vector4 color, TextAlignment hAlignment,
        TextVerticalAlignment vAlignment, float size, float _resolutionScale, Transform transform)
    {
        _text = text;
        this.transform = transform;
        _fontSet = fonts;
        _size = size;
        _horizontalAlignment = hAlignment;
        _verticalAlignment = vAlignment;
        Color = color;
        _resolutionFactor = _resolutionScale;
        CreateResources();
    }

    public Vector4 Color
    {
        get => _color;
        set
        {
            _color = value;
            if (_renderer != null)
            {
                _renderer.Color = value;
                _renderer.UpdateColor(value);
            }
        }
    }

    public void UpdateAlignment(TextAlignment hAlignment, TextVerticalAlignment vAlignment)
    {
        if (hAlignment == _horizontalAlignment && vAlignment == _verticalAlignment)
            return; // Lets not waste our precious time updating text that is already up to date
        _horizontalAlignment = hAlignment;
        _verticalAlignment = vAlignment;
        _dirty = true;
    }

    public void UpdateText(string text, float size, float resolutionScale)
    {
        if (text == _text && size == _size)
            return; // Lets not waste our precious time updating text that is already up to date
        _size = size;
        _text = text;
        _dirty = true;
        _resolutionFactor = resolutionScale;
    }

    public override void CreateResources()
    {
        _renderer = new FontRenderer(new DrawableOptions<VertexPositionColorTexture, TextUniform>
        {
            Shader = AssetManager.GetShader("text")!,
            Mesh = null!, // This value is ignored
            Transform = transform,
            Uniform = new TextUniform(),
            UniformShaderStages = ShaderStages.Vertex | ShaderStages.Fragment
        }, Color, _fontSet.TextureManager);
        DrawText();
    }

    public override void Draw(CommandList cl)
    {
        if (transform.GetType() == typeof(RectTransform))
        {
            // Check if the scale has changed
            var eval = ((RectTransform)transform).Scale.Evaluate();
            if (eval != _lastScaleEval)
            {
                _transformDirty = true;
                _lastScaleEval = eval;
            }
        }
        else
        {
            // Check if the scale has changed
            var scale = transform.GlobalScale;
            if (scale != _lastScaleEval)
            {
                _transformDirty = true;
                _lastScaleEval = scale;
            }
        }

        if (_dirty || _transformDirty)
        {
            // Update text
            _renderer.ClearAllQuads();
            DrawText();
            _dirty = false;
            _transformDirty = false;
        }

        // Draw the text
        _renderer.Draw(cl);
    }

    private void DrawText()
    {
        if (_text == "" || _text == null) return;

        var xScale = 0f;
        var yScale = 0f;
        if (transform is RectTransform rectTransform)
        {
            var eval = rectTransform.Scale.Evaluate();
            xScale = eval.X;
            yScale = eval.Y;
        }
        else
        {
            var eval = transform.GlobalScale;
            xScale = eval.X;
            yScale = eval.Y;
        }

        var splits = SplitSections(_text);
        var heightSoFar = 0f;
        var lineDimensions = new Vector2[splits.Length];
        var totalHeight = 0f;
        // first lets measure our lines
        if (_horizontalAlignment != TextAlignment.Left ||
            _verticalAlignment !=
            TextVerticalAlignment.Top) // LTR and TTB don't need any extra calculations, so we can skip them
            for (var i = 0; i < splits.Length; i++)
            {
                lineDimensions[i] = _fontSet.System.GetFont(_size * _resolutionFactor).MeasureString(splits[i]) *
                    _size / _resolutionFactor;
                totalHeight += lineDimensions[i].Y;
            }

        var yOffset = 0f;
        switch (_verticalAlignment)
        {
            case TextVerticalAlignment.Top:
                yOffset = 0f;
                break;
            case TextVerticalAlignment.Center:
                yOffset = 20000f * yScale - totalHeight / 2f;
                break;
            case TextVerticalAlignment.Bottom:
                yOffset = 40000f * yScale - totalHeight;
                break;
        }

        for (var i = 0; i < splits.Length; i++)
        {
            // Next part is kinda nasty, but it works :)
            switch (_horizontalAlignment)
            {
                case TextAlignment.Center:
                    _fontSet.System.GetFont(_size * _resolutionFactor).DrawText(_renderer,
                        new StringBuilder(splits[i]),
                        new Vector2(-lineDimensions[i].X / 2f, heightSoFar - 20000f * yScale + yOffset),
                        FSColor.White, new Vector2(_size / _resolutionFactor, _size / _resolutionFactor));
                    break;

                case TextAlignment.Left:
                    _fontSet.System.GetFont(_size * _resolutionFactor).DrawText(_renderer,
                        new StringBuilder(splits[i]),
                        new Vector2(-20000f * xScale, heightSoFar - 20000f * yScale + yOffset),
                        FSColor.White, new Vector2(_size / _resolutionFactor, _size / _resolutionFactor));
                    break;

                case TextAlignment.Right:
                    _fontSet.System.GetFont(_size * _resolutionFactor).DrawText(_renderer,
                        new StringBuilder(splits[i]),
                        new Vector2(20000f * xScale - lineDimensions[i].X, heightSoFar - 20000f * yScale + yOffset),
                        FSColor.White, new Vector2(_size / _resolutionFactor, _size / _resolutionFactor));

                    break;
            }

            heightSoFar += _fontSet.System.GetFont(_size * _resolutionFactor).MeasureString(splits[i]).Y * _size /
                           _resolutionFactor;
        }
    }

    private string[] SplitSections(string text)
    {
        if (text == null)
            text = "";
        var sections = new List<string>();
        var lastSplit = 0;

        // Assuming transform is defined elsewhere in your class.
        float maxWidth;

        if (transform is RectTransform rectTransform)
            maxWidth = rectTransform.Scale.Evaluate().X;
        else
            maxWidth = transform.GlobalScale.X;

        maxWidth *= 800f;

        for (var i = 0; i < text.Length; i++)
        {
            // Check for newline
            if (text[i] == '\n')
            {
                sections.Add(text.Substring(lastSplit, i - lastSplit));
                lastSplit = i + 1;
                continue;
            }

            // Now lets check if we need to split the text
            // First we'll get the text so far
            var textSoFar =
                text.Substring(lastSplit, i - lastSplit + 1); // +1 to include current character in the measure
            // Now lets get the width of the text so far
            var width = _fontSet.System.GetFont(_size).MeasureString(textSoFar).X;

            if (width > maxWidth)
            {
                // Our text is overflowing, lets see if we can nicely split it up at the last word
                var lastSpace = textSoFar.LastIndexOf(' ');
                if (lastSpace == -1)
                {
                    // There is no space, lets just split it up
                    sections.Add(text.Substring(lastSplit, i - lastSplit));
                    lastSplit = i;
                }
                else
                {
                    // There is a space, lets split it up there
                    sections.Add(text.Substring(lastSplit, lastSpace));
                    lastSplit = lastSplit + lastSpace + 1;
                }
            }
        }

        if (lastSplit < text.Length) // Ensure we don't miss out any remaining text
            sections.Add(text.Substring(lastSplit));

        return sections.ToArray();
    }

    public void UpdateFontSet(FontSet set)
    {
        Dispose();
        _fontSet = set;
        CreateResources();
    }

    public override void Dispose()
    {
        _renderer.Dispose();
    }

    public override void SetGlobalMatrix(GraphicsDevice graphicsDevice, Matrix4x4 matrix)
    {
        _renderer.SetGlobalMatrix(graphicsDevice, matrix);
        _lastMatrix = matrix;
    }
}

internal struct TextUniform
{
    private Vector4 vector;

    public TextUniform(Vector4 vector)
    {
        this.vector = vector;
    }
}

public struct TextTransformStruct
{
    private Matrix4x4 Screen;
    private Matrix4x4 Transform;
    private Matrix4x4 Camera;

    public TextTransformStruct(Matrix4x4 matrix, Matrix4x4 transform, Matrix4x4 camera)
    {
        Screen = matrix;
        Transform = transform;
        Camera = camera;
    }
}

internal unsafe class FontTextureManager : ITexture2DManager
{
    private readonly List<Veldrid.Texture> _textures = new();

    public object CreateTexture(int width, int height)
    {
        var factory = Renderer.GraphicsDevice.ResourceFactory;
        var t = factory.CreateTexture(new TextureDescription((uint)width, (uint)height, 1, 1, 1,
            PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled, TextureType.Texture2D));
        _textures.Add(t);
        return t;
    }

    public Point GetTextureSize(object texture)
    {
        return new Point((int)((Veldrid.Texture)texture).Width,
            (int)((Veldrid.Texture)texture).Height);
    }

    public void SetTextureData(object texture, Rectangle bounds, byte[] data)
    {
        var t = (Veldrid.Texture)texture;
        fixed (byte* ptr = data)
        {
            Renderer.GraphicsDevice.UpdateTexture(t, new IntPtr(ptr), (uint)data.Length, (uint)bounds.X,
                (uint)bounds.Y, 0, (uint)bounds.Width, (uint)bounds.Height, 1, 0, 0);
        }
    }

    public void Dispose()
    {
        foreach (var t in _textures) t.Dispose();
    }
}

internal class FontRenderer : Drawable<VertexPositionColorTexture, TextUniform>, IFontStashRenderer2
{
    private readonly FontTextureManager texManager;

    private readonly Mesh<VertexPositionColorTexture>
        virtualMesh; // This is needed when the mesh is updated during rendering

    private ResourceLayout _textResourceLayout;

    private bool buffersDirty;
    public Vector4 Color = new(1, 1, 1, 1f);
    private DeviceBuffer colorBuffer;
    private bool resourcesCreated;
    private Veldrid.Texture texture;

    private TextureView texView;

    public FontRenderer(DrawableOptions<VertexPositionColorTexture, TextUniform> options, Vector4 color,
        FontTextureManager textureManager) : base(options)
    {
        var layout = new VertexLayoutDescription(
            new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate,
                VertexElementFormat.Float3),
            new VertexElementDescription("Color", VertexElementSemantic.TextureCoordinate,
                VertexElementFormat.UInt1),
            new VertexElementDescription("UV", VertexElementSemantic.TextureCoordinate,
                VertexElementFormat.Float2));

        _mesh = new Mesh<VertexPositionColorTexture>(new VertexPositionColorTexture[0], new ushort[0], layout);
        virtualMesh =
            new Mesh<VertexPositionColorTexture>(new VertexPositionColorTexture[0], new ushort[0], layout);
        transform = options.Transform;
        drawableUniform = options.Uniform;
        uniformShaderStages = options.UniformShaderStages ?? ShaderStages.Fragment | ShaderStages.Vertex;
        transformShaderStages = options.TransformShaderStages ?? ShaderStages.Vertex;
        Color = color;
        texManager = textureManager;
    }

    ITexture2DManager IFontStashRenderer2.TextureManager => texManager;

    public void DrawQuad(object texture, ref VertexPositionColorTexture topLeft,
        ref VertexPositionColorTexture topRight, ref VertexPositionColorTexture bottomLeft,
        ref VertexPositionColorTexture bottomRight)
    {
        var c = virtualMesh.Vertices.Length;
        virtualMesh.AddIndices(new ushort[6]
            { (ushort)c, (ushort)(c + 1), (ushort)(c + 2), (ushort)(c + 2), (ushort)(c + 1), (ushort)(c + 3) });
        virtualMesh.AddVertices(new VertexPositionColorTexture[4]
            { topLeft, topRight, bottomLeft, bottomRight });
        this.texture = (Veldrid.Texture)texture;
        buffersDirty = true;
    }

    public void ClearAllQuads()
    {
        virtualMesh.ClearIndices();
        virtualMesh.ClearVertices();
        buffersDirty = true;
    }

    protected void CreateResources(GraphicsDevice graphicsDevice, Veldrid.Texture texture)
    {
        var shader = AssetManager.GetShader("text");
        var factory = graphicsDevice.ResourceFactory;
        vertexBuffer = factory.CreateBuffer(new BufferDescription(
            (uint)_mesh.Vertices.Length * (uint)Marshal.SizeOf<VertexPositionColorTexture>(),
            BufferUsage.VertexBuffer));
        indexBuffer = factory.CreateBuffer(new BufferDescription((uint)_mesh.Indices.Length * sizeof(ushort),
            BufferUsage.IndexBuffer));
        transformBuffer = factory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf<TextTransformStruct>(),
            BufferUsage.UniformBuffer));
        colorBuffer =
            factory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf<TextUniform>(),
                BufferUsage.UniformBuffer));


        graphicsDevice.UpdateBuffer(vertexBuffer, 0, _mesh.Vertices);
        graphicsDevice.UpdateBuffer(indexBuffer, 0, _mesh.Indices);
        graphicsDevice.UpdateBuffer(transformBuffer, 0,
            new TextTransformStruct(new Matrix4x4(), new Matrix4x4(),
                new Matrix4x4())); // By having zeroed out matrices the text wont "jitter" if a frame is rendered before the matrix has been properly updated
        graphicsDevice.UpdateBuffer(colorBuffer, 0, new TextUniform(Color));
        // Next lets load textures to the gpu

        texView = factory.CreateTextureView(texture);


        var vertexLayout = _mesh.VertexLayout;

        _shaders = shader.Shaders;

        var pipelineDescription = new GraphicsPipelineDescription();
        pipelineDescription.BlendState = BlendStateDescription.SingleAlphaBlend;
        var elementDescriptions = new ResourceLayoutElementDescription[4];
        elementDescriptions[0] = new ResourceLayoutElementDescription("TransformMatrices",
            ResourceKind.UniformBuffer, transformShaderStages);


        elementDescriptions[1] =
            new ResourceLayoutElementDescription("Texture", ResourceKind.TextureReadOnly, ShaderStages.Fragment);
        elementDescriptions[2] =
            new ResourceLayoutElementDescription("TextureSampler", ResourceKind.Sampler, ShaderStages.Fragment);
        elementDescriptions[3] =
            new ResourceLayoutElementDescription("Color", ResourceKind.UniformBuffer, ShaderStages.Fragment);

        _textResourceLayout = factory.CreateResourceLayout(
            new ResourceLayoutDescription(elementDescriptions));

        pipelineDescription.DepthStencilState = new DepthStencilStateDescription(
            false,
            false,
            ComparisonKind.LessEqual);

        pipelineDescription.RasterizerState = new RasterizerStateDescription(
            FaceCullMode.None,
            PolygonFillMode.Solid,
            FrontFace.Clockwise,
            false,
            false);

        pipelineDescription.PrimitiveTopology = PrimitiveTopology.TriangleList;
        pipelineDescription.ResourceLayouts = Array.Empty<ResourceLayout>();
        pipelineDescription.ShaderSet = new ShaderSetDescription(
            new[] { vertexLayout },
            _shaders);
        pipelineDescription.ResourceLayouts = new[] { _textResourceLayout };

        pipelineDescription.Outputs = Renderer.PrimaryFramebuffer.OutputDescription;
        pipeline = factory.CreateGraphicsPipeline(pipelineDescription);
        var buffers = new BindableResource[4];
        buffers[0] = transformBuffer;
        buffers[1] = texView;
        buffers[2] = graphicsDevice.LinearSampler;
        buffers[3] = colorBuffer;

        _transformSet = factory.CreateResourceSet(new ResourceSetDescription(
            _textResourceLayout,
            buffers));
    }

    public void UpdateColor(Vector4 color)
    {
        if (colorBuffer != null)
            Renderer.GraphicsDevice.UpdateBuffer(colorBuffer, 0, new TextUniform(color));
    }

    public override void SetGlobalMatrix(GraphicsDevice graphicsDevice, Matrix4x4 matrix)
    {
        var tmat = transform.GetTransformationMatrix();
        var cmat = Matrix4x4.Identity;

        if (transform.GetType() != typeof(RectTransform))
            cmat = Camera.GetTransformMatrix();
        if (transformBuffer != null && !transformBuffer.IsDisposed)
            graphicsDevice.UpdateBuffer(transformBuffer, 0, new TextTransformStruct(matrix, tmat, cmat));
    }


    public override void Draw(CommandList cl)
    {
        if (!resourcesCreated && buffersDirty && texture != null && virtualMesh.Vertices.Length > 0)
        {
            _mesh = new Mesh<VertexPositionColorTexture>(virtualMesh);
            CreateResources(Renderer.GraphicsDevice, texture);
            resourcesCreated = true;
        }

        if (!resourcesCreated) return;

        if (buffersDirty && virtualMesh.Vertices.Length == 0) return;

        if (buffersDirty)
        {
            buffersDirty = false;
            _mesh = new Mesh<VertexPositionColorTexture>(virtualMesh);
            UpdateMeshBuffers();
        }

        if (_mesh.Vertices.Length == 0) return;

        cl.SetPipeline(pipeline);

        cl.SetVertexBuffer(0, vertexBuffer);
        cl.SetIndexBuffer(indexBuffer, IndexFormat.UInt16);
        cl.SetGraphicsResourceSet(0, _transformSet);
        cl.DrawIndexed(
            (uint)_mesh.Indices.Length,
            1,
            0,
            0,
            0);
    }

    public override void Dispose()
    {
        texView?.Dispose();
        colorBuffer?.Dispose();
        _textResourceLayout?.Dispose();
        base.Dispose();
    }
}