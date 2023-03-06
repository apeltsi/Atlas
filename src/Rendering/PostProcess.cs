namespace SolidCode.Atlas.Rendering
{
    using System.Numerics;
    using System.Runtime.InteropServices;
    using SolidCode.Atlas.Components;
    using Veldrid;

    public class PostProcess : Drawable
    {
        public Transform transform;
        private string _shader;
        private Mesh<VertexPositionUV> _mesh;
        private Veldrid.TextureView[] texViews;
        private Veldrid.Framebuffer? buffer = null;
        struct VertexPositionUV
        {
            Vector4 Position;
            Vector4 UV;

            public VertexPositionUV(Vector2 position, Vector4 uV)
            {
                Position = new Vector4(position.X, position.Y, 0, 0);
                UV = uV;
            }

        }

        public PostProcess(GraphicsDevice _graphicsDevice, Veldrid.TextureView[] textures, string path, Veldrid.Framebuffer? buffer = null)
        {
            this._shader = path;
            this.texViews = textures;
            VertexPositionUV[] positions = {
                new VertexPositionUV(new Vector2(-1f, 1f), new Vector4(0, 0,0,0)),
                new VertexPositionUV(new Vector2(1f, 1f), new Vector4(1, 0,0,0)),
                new VertexPositionUV(new Vector2(-1f, -1f), new Vector4(0, 1,0,0)),
                new VertexPositionUV(new Vector2(1f, -1f), new Vector4(1, 1,0,0))
            };
            ushort[] quadIndices = { 0, 1, 2, 3 };
            var layout = new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4),
                new VertexElementDescription("UV", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4));

            this._mesh = new Mesh<VertexPositionUV>(positions, quadIndices, layout);
            this.buffer = buffer;

            CreateResources(_graphicsDevice, textures);
        }
        void CreateResources(GraphicsDevice _graphicsDevice, TextureView[] textures)
        {
            Shader shader = ShaderManager.GetShader(_shader);
            ResourceFactory factory = _graphicsDevice.ResourceFactory;


            vertexBuffer = factory.CreateBuffer(new BufferDescription((uint)_mesh.Vertices.Length * (uint)Marshal.SizeOf<VertexPositionUV>(), BufferUsage.VertexBuffer));
            indexBuffer = factory.CreateBuffer(new BufferDescription((uint)_mesh.Indicies.Length * sizeof(ushort), BufferUsage.IndexBuffer));


            _graphicsDevice.UpdateBuffer(vertexBuffer, 0, _mesh.Vertices);
            _graphicsDevice.UpdateBuffer(indexBuffer, 0, _mesh.Indicies);



            VertexLayoutDescription vertexLayout = _mesh.VertexLayout;

            _shaders = shader.shaders;

            GraphicsPipelineDescription pipelineDescription = new GraphicsPipelineDescription();
            pipelineDescription.BlendState = BlendStateDescription.SingleOverrideBlend;
            ResourceLayoutElementDescription[] elementDescriptions = new ResourceLayoutElementDescription[textures.Length + 1];
            for (int i = 0; i < textures.Length; i++)
            {
                elementDescriptions[i] = new ResourceLayoutElementDescription("Texture" + i, ResourceKind.TextureReadOnly, ShaderStages.Fragment);
            }
            elementDescriptions[textures.Length] = new ResourceLayoutElementDescription("Sampler", ResourceKind.Sampler, ShaderStages.Fragment);
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
                depthClipEnabled: true,
                scissorTestEnabled: false);

            pipelineDescription.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
            pipelineDescription.ResourceLayouts = System.Array.Empty<ResourceLayout>();
            pipelineDescription.ShaderSet = new ShaderSetDescription(
                vertexLayouts: new VertexLayoutDescription[] { vertexLayout },
                shaders: _shaders);
            pipelineDescription.ResourceLayouts = new[] { uniformResourceLayout };
            if (buffer == null)
            {
                pipelineDescription.Outputs = _graphicsDevice.MainSwapchain.Framebuffer.OutputDescription;
                buffer = _graphicsDevice.MainSwapchain.Framebuffer;
            }
            else
            {
                pipelineDescription.Outputs = buffer.OutputDescription;
            }
            pipeline = factory.CreateGraphicsPipeline(pipelineDescription);
            BindableResource[] buffers = new BindableResource[textures.Length + 1];
            for (int i = 0; i < textures.Length; i++)
            {
                buffers[i] = textures[i];
            }
            SamplerDescription sdesc = new SamplerDescription(SamplerAddressMode.Clamp, SamplerAddressMode.Clamp, SamplerAddressMode.Clamp, SamplerFilter.Anisotropic, null, 4, 0, uint.MaxValue, 0, SamplerBorderColor.TransparentBlack);
            Sampler s = factory.CreateSampler(sdesc);
            buffers[textures.Length] = s;

            _transformSet = factory.CreateResourceSet(new ResourceSetDescription(
                uniformResourceLayout,
                buffers));

        }

        public override void Draw(CommandList cl)
        {
            if (buffer != null)
            {
                cl.SetFramebuffer(buffer);
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

        public override void Dispose()
        {
            pipeline.Dispose();
            vertexBuffer.Dispose();
            indexBuffer.Dispose();
            _transformSet.Dispose();
        }

    }
}