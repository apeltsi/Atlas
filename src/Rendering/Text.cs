
namespace SolidCode.Caerus.Rendering
{
    using System.Collections.Generic;
    using System.Drawing;
    using System.Numerics;
    using System.Runtime.InteropServices;
    using FontStashSharp;
    using FontStashSharp.Interfaces;
    using SolidCode.Caerus.Components;
    using Veldrid;

    public class TextDrawable : Drawable
    {
        public Transform transform;
        string text;
        FontSystem font;
        string[] fontPaths;
        FontRenderer renderer;
        public TextDrawable(string text, string[] fontPaths, int size, Transform transform)
        {
            this.text = text;
            this.transform = transform;
            this.fontPaths = fontPaths;
            CreateResources(Window._graphicsDevice);
            // TODO
            // #1 Get the texture to the gpu
            // #2 only update vertices on text update
            renderer = new FontRenderer(Window._graphicsDevice, transform, new Uniform(), ShaderStages.Vertex | ShaderStages.Fragment);
            this.font.GetFont(size).DrawText(renderer, text, System.Numerics.Vector2.Zero, Color.White);
        }

        public void UpdateText(string text, int size)
        {
            renderer.ClearAllQuads();
            this.font.GetFont(size).DrawText(renderer, text, System.Numerics.Vector2.Zero, Color.White, origin: new Vector2(0.5f, 0.5f));
        }

        public override void CreateResources(GraphicsDevice _graphicsDevice)
        {
            this.font = new FontSystem();
            for (int i = 0; i < this.fontPaths.Length; i++)
            {
                FontManager.AddFont(this.font, this.fontPaths[i]);
            }
        }
        public override void Draw(CommandList cl)
        {
            renderer.Draw(cl);
        }

        public override void Dispose()
        {
            this.font.Dispose();
        }

        public override void SetGlobalMatrix(GraphicsDevice _graphicsDevice, Matrix4x4 matrix)
        {
            renderer.SetGlobalMatrix(_graphicsDevice, matrix);
        }


    }

    struct Uniform {
        Vector4 vector;
    }

    unsafe class FontTextureManager : ITexture2DManager
    {
        
        public object CreateTexture(int width, int height)
        {
            ResourceFactory factory = Window._graphicsDevice.ResourceFactory;
            Veldrid.Texture t = factory.CreateTexture(new TextureDescription((uint)width, (uint)height, 1, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled, TextureType.Texture2D));
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
                Window._graphicsDevice.UpdateTexture(t, new IntPtr(ptr), (uint)data.Length, (uint)bounds.X, (uint)bounds.Y, 0, (uint)bounds.Width, (uint)bounds.Height, 1, 0, 0);
            }
        }
    }

    class FontRenderer : Drawable<VertexPositionColorTexture, Uniform>, IFontStashRenderer2 
    {
        bool resourcesCreated = false;
        Mesh<VertexPositionColorTexture> virtualMesh; // This is needed when the mesh is updated during rendering
        public FontRenderer(GraphicsDevice _graphicsDevice, Transform t, Uniform uniform, ShaderStages uniformShaderStages, ShaderStages transformShaderStages = ShaderStages.Vertex) : base(_graphicsDevice, "text", null, t, uniform, uniformShaderStages, new List<string>(), transformShaderStages)
        {
            var layout = new VertexLayoutDescription(
                        new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                        new VertexElementDescription("Color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.UInt1),
                        new VertexElementDescription("UV", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2));

            this._mesh = new Mesh<VertexPositionColorTexture>(new VertexPositionColorTexture[0], new ushort[0], layout);
            this.virtualMesh = new Mesh<VertexPositionColorTexture>(new VertexPositionColorTexture[0], new ushort[0], layout);
            this.transform = t;
            this.uniform = uniform;
            this.uniformShaderStages = uniformShaderStages;
            this.transformShaderStages = transformShaderStages;
            texManager = new FontTextureManager();
        }

        TextureView texView;
        private FontTextureManager texManager;
        Veldrid.Texture texture;
        ITexture2DManager IFontStashRenderer2.TextureManager {
            get { return texManager; }

        }
        bool buffersDirty = false;

        public void DrawQuad(object texture, ref VertexPositionColorTexture topLeft, ref VertexPositionColorTexture topRight, ref VertexPositionColorTexture bottomLeft, ref VertexPositionColorTexture bottomRight)
        {
            int c = this.virtualMesh.Vertices.Length;
            this.virtualMesh.AddIndicies(new ushort[6] {(ushort)(c), (ushort)(c + 1), (ushort)(c + 2), (ushort)(c + 2),  (ushort)(c + 1),  (ushort)(c + 3)});
            this.virtualMesh.AddVertices(new VertexPositionColorTexture[4] {topLeft, topRight, bottomLeft, bottomRight});
            this.texture = (Veldrid.Texture)texture;
            buffersDirty = true;
        }

        public void ClearAllQuads() {
            this.virtualMesh.ClearIndicies();
            this.virtualMesh.ClearVertices();
            buffersDirty = true;
        }

        protected void CreateResources(GraphicsDevice _graphicsDevice, Veldrid.Texture texture)
        {
            Shader shader = ShaderManager.GetShader("text");
            ResourceFactory factory = _graphicsDevice.ResourceFactory;
            
            vertexBuffer = factory.CreateBuffer(new BufferDescription((uint)_mesh.Vertices.Length * (uint)Marshal.SizeOf<VertexPositionColorTexture>(), BufferUsage.VertexBuffer));
            indexBuffer = factory.CreateBuffer(new BufferDescription((uint)_mesh.Indicies.Length * sizeof(ushort), BufferUsage.IndexBuffer));
            transformBuffer = factory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf<TransformStruct>(), BufferUsage.UniformBuffer));





            _graphicsDevice.UpdateBuffer(vertexBuffer, 0, _mesh.Vertices);
            _graphicsDevice.UpdateBuffer(indexBuffer, 0, _mesh.Indicies);
            _graphicsDevice.UpdateBuffer(transformBuffer, 0, new TransformStruct(Matrix4x4.Identity, Matrix4x4.Identity, Camera.GetTransformMatrix()));

            // Next lest load textures to the gpu
            
            TextureView texView = factory.CreateTextureView(texture);
            


            VertexLayoutDescription vertexLayout = _mesh.VertexLayout;

            _shaders = shader.shaders;

            GraphicsPipelineDescription pipelineDescription = new GraphicsPipelineDescription();
            pipelineDescription.BlendState = BlendStateDescription.SingleAlphaBlend;
            ResourceLayoutElementDescription[] elementDescriptions = new ResourceLayoutElementDescription[3];
            elementDescriptions[0] = new ResourceLayoutElementDescription("TransformMatrices", ResourceKind.UniformBuffer, transformShaderStages);

            
            elementDescriptions[1] = new ResourceLayoutElementDescription("Texture", ResourceKind.TextureReadOnly, ShaderStages.Fragment);
            elementDescriptions[2] = new ResourceLayoutElementDescription("TextureSampler", ResourceKind.Sampler, ShaderStages.Fragment);
            
            ResourceLayout uniformResourceLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(elementDescriptions));
            pipelineDescription.DepthStencilState = new DepthStencilStateDescription(
                depthTestEnabled: false,
                depthWriteEnabled: false,
                comparisonKind: ComparisonKind.LessEqual);

            pipelineDescription.RasterizerState = new RasterizerStateDescription(
                cullMode: FaceCullMode.Back,
                fillMode: PolygonFillMode.Solid,
                frontFace: FrontFace.Clockwise,
                depthClipEnabled: false,
                scissorTestEnabled: false);

            pipelineDescription.PrimitiveTopology = PrimitiveTopology.TriangleList;
            pipelineDescription.ResourceLayouts = System.Array.Empty<ResourceLayout>();
            pipelineDescription.ShaderSet = new ShaderSetDescription(
                vertexLayouts: new VertexLayoutDescription[] { vertexLayout },
                shaders: _shaders);
            pipelineDescription.ResourceLayouts = new[] { uniformResourceLayout };

            pipelineDescription.Outputs = Window.DuplicatorFramebuffer.OutputDescription;
            pipeline = factory.CreateGraphicsPipeline(pipelineDescription);
            BindableResource[] buffers = new BindableResource[3];
            buffers[0] = transformBuffer;
            buffers[1] = texView;
            buffers[2] = _graphicsDevice.Aniso4xSampler;
            
            _transformSet = factory.CreateResourceSet(new ResourceSetDescription(
                uniformResourceLayout,
                buffers));

        }

        public override void Draw(CommandList cl) {
            if(!resourcesCreated && buffersDirty && texture != null) {
                _mesh = new Mesh<VertexPositionColorTexture>(virtualMesh);
                CreateResources(Window._graphicsDevice, this.texture);
                resourcesCreated = true;
            }

            if(!resourcesCreated) {
                return;
            }
            if(buffersDirty && virtualMesh.Vertices.Length == 0) {
                return;
            }
            if(buffersDirty) {
                buffersDirty = false;
                _mesh = new Mesh<VertexPositionColorTexture>(virtualMesh);
                UpdateMeshBuffers();
            }
            if(_mesh.Vertices.Length == 0) {
                return;
            }

            cl.SetVertexBuffer(0, vertexBuffer);
            cl.SetIndexBuffer(indexBuffer, IndexFormat.UInt16);
            cl.SetPipeline(pipeline);
            cl.SetGraphicsResourceSet(0, _transformSet);
            cl.DrawIndexed(
                indexCount: (uint)_mesh.Indicies.Length,
                instanceCount: 1,
                indexStart: 0,
                vertexOffset: 0,
                instanceStart: 0);
        }
    }
}