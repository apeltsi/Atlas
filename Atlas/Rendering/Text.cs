
using System.Text;
using SolidCode.Atlas.UI;

namespace SolidCode.Atlas.Rendering
{
    using System.Collections.Generic;
    using System.Drawing;
    using System.Numerics;
    using System.Runtime.InteropServices;
    using FontStashSharp;
    using FontStashSharp.Interfaces;
    using SolidCode.Atlas.Components;
    using Veldrid;
    using SolidCode.Atlas.ECS;

    public class TextDrawable : Drawable
    {
        bool dirty = false;
        string _text;
        float _size;
        FontSet _fontSet;
        FontRenderer _renderer;
        Matrix4x4 _lastMatrix;
        bool _centered = true;
        private Vector4 _color;
        public Vector4 Color
        {
            get
            {
                return _color;
            }
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
        public TextDrawable(string text, FontSet fonts, Vector4 color, bool centered, float size, Transform transform)
        {
            this._text = text;
            this.transform = transform;
            this._fontSet = fonts;
            this._size = size;
            this._centered = centered;
            this.Color = color;
            CreateResources();
        }

        public void UpdateText(string text, float size)
        {
            if (text == this._text && size == this._size)
                return; // Lets not waste our precious time updating text that is already up to date
            _renderer.ClearAllQuads();
            this._size = size;
            this._text = text;
            dirty = true;
        }

        public override void CreateResources()
        {
            _renderer = new FontRenderer(Renderer.GraphicsDevice, this.Color, transform, new TextUniform(),  ShaderStages.Vertex | ShaderStages.Fragment, this._fontSet.TextureManager);
            if (_centered)
                _renderer.SetHorizontalOffset(this._fontSet.System.GetFont(_size).MeasureString(_text).X / 2f);
            this._fontSet.System.GetFont(_size).DrawText(_renderer, new StringBuilder(_text), Vector2.Zero,FSColor.White, new Vector2(_size,_size));
        }
        
        public override void Draw(CommandList cl)
        {
            if (dirty)
            {
                if (_centered)
                    _renderer.SetHorizontalOffset(this._fontSet.System.GetFont(_size).MeasureString(_text).X / 2f);
                this._fontSet.System.GetFont(_size).DrawText(_renderer, new StringBuilder(_text), Vector2.Zero,FSColor.White);
                SetGlobalMatrix(Renderer.GraphicsDevice, _lastMatrix);
                dirty = false;
            }
            _renderer.Draw(cl);
        }

        public void UpdateFontSet(FontSet set)
        {
            Dispose();
            this._fontSet = set;
            CreateResources();
        }

        public override void Dispose()
        {
            this._renderer.Dispose();
        }

        public override void SetGlobalMatrix(GraphicsDevice _graphicsDevice, Matrix4x4 matrix)
        {
            _renderer.SetGlobalMatrix(_graphicsDevice, matrix);
            _lastMatrix = matrix;
        }


    }

    struct TextUniform
    {
        Vector4 vector;

        public TextUniform(Vector4 vector)
        {
            this.vector = vector;
        }
    }

    public struct TextTransformStruct
    {
        Matrix4x4 Screen;
        Matrix4x4 Transform;
        Matrix4x4 Camera;
        Vector4 Offsets;

        public TextTransformStruct(Matrix4x4 matrix, Matrix4x4 transform, Matrix4x4 camera, float horizontalOffset)
        {
            Screen = matrix;
            Transform = transform;
            Camera = camera;
            Offsets = new Vector4(horizontalOffset, 0, 0, 0);
        }
    }


    internal unsafe class FontTextureManager : ITexture2DManager
    {
        private List<Veldrid.Texture> _textures = new List<Veldrid.Texture>();  
        public object CreateTexture(int width, int height)
        {
            ResourceFactory factory = Renderer.GraphicsDevice.ResourceFactory;
            Veldrid.Texture t = factory.CreateTexture(new TextureDescription((uint)width, (uint)height, 1, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled, TextureType.Texture2D));
            _textures.Add(t);
            return t;
        }

        public System.Drawing.Point GetTextureSize(object texture)
        {
            return new System.Drawing.Point((int)((Veldrid.Texture)texture).Width, (int)((Veldrid.Texture)texture).Height);
        }

        public void SetTextureData(object texture, System.Drawing.Rectangle bounds, byte[] data)
        {
            Veldrid.Texture t = (Veldrid.Texture)texture;
            fixed (byte* ptr = data)
            {
                Renderer.GraphicsDevice.UpdateTexture(t, new IntPtr(ptr), (uint)data.Length, (uint)bounds.X, (uint)bounds.Y, 0, (uint)bounds.Width, (uint)bounds.Height, 1, 0, 0);
            }
        }

        public void Dispose()
        {
            foreach (Veldrid.Texture t in _textures)
            {
                t.Dispose();
            }
        }
    }

    class FontRenderer : Drawable<VertexPositionColorTexture, TextUniform>, IFontStashRenderer2
    {
        bool resourcesCreated = false;
        Mesh<VertexPositionColorTexture> virtualMesh; // This is needed when the mesh is updated during rendering
        float HorizontalOffset = 0f;
        DeviceBuffer colorBuffer;
        public Vector4 Color = new Vector4(1, 1, 1, 1f);
        public FontRenderer(GraphicsDevice _graphicsDevice, Vector4 Color, Transform t, TextUniform drawableUniform, ShaderStages uniformShaderStages, FontTextureManager textureManager, ShaderStages transformShaderStages = ShaderStages.Vertex) : base(_graphicsDevice, "text", null, t, drawableUniform, uniformShaderStages, new List<Texture>(), transformShaderStages)
        {
            var layout = new VertexLayoutDescription(
                        new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                        new VertexElementDescription("Color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.UInt1),
                        new VertexElementDescription("UV", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2));
            
            this._mesh = new Mesh<VertexPositionColorTexture>(new VertexPositionColorTexture[0], new ushort[0], layout);
            this.virtualMesh = new Mesh<VertexPositionColorTexture>(new VertexPositionColorTexture[0], new ushort[0], layout);
            this.transform = t;
            this.drawableUniform = drawableUniform;
            this.uniformShaderStages = uniformShaderStages;
            this.transformShaderStages = transformShaderStages;
            this.Color = Color;
            texManager = textureManager;
        }

        TextureView texView;
        private FontTextureManager texManager;
        Veldrid.Texture texture;
        ITexture2DManager IFontStashRenderer2.TextureManager
        {
            get { return texManager; }

        }
        bool buffersDirty = false;
        private ResourceLayout _textResourceLayout;

        public void DrawQuad(object texture, ref VertexPositionColorTexture topLeft, ref VertexPositionColorTexture topRight, ref VertexPositionColorTexture bottomLeft, ref VertexPositionColorTexture bottomRight)
        {
            int c = this.virtualMesh.Vertices.Length;
            this.virtualMesh.AddIndices(new ushort[6] { (ushort)(c), (ushort)(c + 1), (ushort)(c + 2), (ushort)(c + 2), (ushort)(c + 1), (ushort)(c + 3) });
            this.virtualMesh.AddVertices(new VertexPositionColorTexture[4] { topLeft, topRight, bottomLeft, bottomRight });
            this.texture = (Veldrid.Texture)texture;
            buffersDirty = true;
        }

        public void ClearAllQuads()
        {
            this.virtualMesh.ClearIndices();
            this.virtualMesh.ClearVertices();
            buffersDirty = true;
        }

        protected void CreateResources(GraphicsDevice _graphicsDevice, Veldrid.Texture texture)
        {
            Shader shader = AssetManagement.AssetManager.GetAsset<Shader>("text");
            ResourceFactory factory = _graphicsDevice.ResourceFactory;
            vertexBuffer = factory.CreateBuffer(new BufferDescription((uint)_mesh.Vertices.Length * (uint)Marshal.SizeOf<VertexPositionColorTexture>(), BufferUsage.VertexBuffer));
            indexBuffer = factory.CreateBuffer(new BufferDescription((uint)_mesh.Indices.Length * sizeof(ushort), BufferUsage.IndexBuffer));
            transformBuffer = factory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf<TextTransformStruct>(), BufferUsage.UniformBuffer));
            colorBuffer = factory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf<TextUniform>(), BufferUsage.UniformBuffer));




            _graphicsDevice.UpdateBuffer(vertexBuffer, 0, _mesh.Vertices);
            _graphicsDevice.UpdateBuffer(indexBuffer, 0, _mesh.Indices);
            _graphicsDevice.UpdateBuffer(transformBuffer, 0, new TextTransformStruct(new Matrix4x4(), new Matrix4x4(), new Matrix4x4(), HorizontalOffset)); // By having zeroed out matrices the text wont "jitter" if a frame is rendered before the matrix has been properly updated
            _graphicsDevice.UpdateBuffer(colorBuffer, 0, new TextUniform(this.Color));
            // Next lets load textures to the gpu

            texView = factory.CreateTextureView(texture);



            VertexLayoutDescription vertexLayout = _mesh.VertexLayout;
            
            _shaders = shader.Shaders;

            GraphicsPipelineDescription pipelineDescription = new GraphicsPipelineDescription();
            pipelineDescription.BlendState = BlendStateDescription.SingleAlphaBlend;
            ResourceLayoutElementDescription[] elementDescriptions = new ResourceLayoutElementDescription[4];
            elementDescriptions[0] = new ResourceLayoutElementDescription("TransformMatrices", ResourceKind.UniformBuffer, transformShaderStages);


            elementDescriptions[1] = new ResourceLayoutElementDescription("Texture", ResourceKind.TextureReadOnly, ShaderStages.Fragment);
            elementDescriptions[2] = new ResourceLayoutElementDescription("TextureSampler", ResourceKind.Sampler, ShaderStages.Fragment);
            elementDescriptions[3] = new ResourceLayoutElementDescription("Color", ResourceKind.UniformBuffer, ShaderStages.Fragment);

            _textResourceLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(elementDescriptions));
            
            pipelineDescription.DepthStencilState = new DepthStencilStateDescription(
                depthTestEnabled: false,
                depthWriteEnabled: false,
                comparisonKind: ComparisonKind.LessEqual);

            pipelineDescription.RasterizerState = new RasterizerStateDescription(
                cullMode: FaceCullMode.None,
                fillMode: PolygonFillMode.Solid,
                frontFace: FrontFace.Clockwise,
                depthClipEnabled: false,
                scissorTestEnabled: false);

            pipelineDescription.PrimitiveTopology = PrimitiveTopology.TriangleList;
            pipelineDescription.ResourceLayouts = System.Array.Empty<ResourceLayout>();
            pipelineDescription.ShaderSet = new ShaderSetDescription(
                vertexLayouts: new VertexLayoutDescription[] { vertexLayout },
                shaders: _shaders);
            pipelineDescription.ResourceLayouts = new[] { _textResourceLayout };

            pipelineDescription.Outputs = Renderer.PrimaryFramebuffer.OutputDescription;
            pipeline = factory.CreateGraphicsPipeline(pipelineDescription);
            BindableResource[] buffers = new BindableResource[4];
            buffers[0] = transformBuffer;
            buffers[1] = texView;
            buffers[2] = _graphicsDevice.Aniso4xSampler;
            buffers[3] = colorBuffer;

            _transformSet = factory.CreateResourceSet(new ResourceSetDescription(
                _textResourceLayout,
                buffers));

        }

        public void SetHorizontalOffset(float offset)
        {
            HorizontalOffset = offset;
        }

        public void UpdateColor(Vector4 color)
        {
            if (colorBuffer != null)
                Renderer.GraphicsDevice.UpdateBuffer(colorBuffer, 0, new TextUniform(color));
        }

        public override void SetGlobalMatrix(GraphicsDevice _graphicsDevice, Matrix4x4 matrix)
        {
            Matrix4x4 cmat = Matrix4x4.Identity;
            
            if(transform.GetType() != typeof(RectTransform)) 
                cmat = Camera.GetTransformMatrix();

            if (transformBuffer != null && _graphicsDevice != null)
            {
                _graphicsDevice.UpdateBuffer(transformBuffer,
                0,
                new TextTransformStruct(matrix,
                                        transform.GetTransformationMatrix(),
                                        cmat,
                                        HorizontalOffset));
            }

        }


        public override void Draw(CommandList cl)
        {
            if (!resourcesCreated && buffersDirty && texture != null && virtualMesh.Vertices.Length > 0)
            {
                _mesh = new Mesh<VertexPositionColorTexture>(virtualMesh);
                CreateResources(Renderer.GraphicsDevice, this.texture);
                resourcesCreated = true;
            }

            if (!resourcesCreated)
            {
                return;
            }
            if (buffersDirty && virtualMesh.Vertices.Length == 0)
            {
                return;
            }
            if (buffersDirty)
            {
                buffersDirty = false;
                _mesh = new Mesh<VertexPositionColorTexture>(virtualMesh);
                UpdateMeshBuffers();
            }
            if (_mesh.Vertices.Length == 0)
            {
                return;
            }
            cl.SetPipeline(pipeline);

            cl.SetVertexBuffer(0, vertexBuffer);
            cl.SetIndexBuffer(indexBuffer, IndexFormat.UInt16);
            cl.SetGraphicsResourceSet(0, _transformSet);
            cl.DrawIndexed(
                indexCount: (uint)_mesh.Indices.Length,
                instanceCount: 1,
                indexStart: 0,
                vertexOffset: 0,
                instanceStart: 0);
        }

        public override void Dispose()
        {
            texView.Dispose();
            colorBuffer.Dispose();
            _textResourceLayout.Dispose();
            base.Dispose();
        }
    }
}