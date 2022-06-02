using System.Numerics;
using System.Runtime.InteropServices;
using SolidCode.Caerus.Components;
using Veldrid;

namespace SolidCode.Caerus.Rendering
{
    public class PostProcess : Drawable
    {
        public Transform transform;
        private string _shader;
        private Mesh<VertexPositionUV> _mesh;
        private Veldrid.TextureView texView;
        private Dictionary<string, TextureView> _textures = new Dictionary<string, TextureView>();
        struct VertexPositionUV
        {
            Vector4 Position;
            Vector4 UV;

            public VertexPositionUV(Vector4 position, Vector4 uV)
            {
                Position = position;
                UV = uV;
            }
        }

        public PostProcess(GraphicsDevice _graphicsDevice, Veldrid.TextureView texture)
        {
            this._shader = "post";
            this.texView = texture;
            VertexPositionUV[] positions = {
                new VertexPositionUV(new Vector4(-1f, 1f, 0,0), new Vector4(0, 0,0,0)),
                new VertexPositionUV(new Vector4(1f, 1f, 0,0), new Vector4(1, 0,0,0)),
                new VertexPositionUV(new Vector4(-1f, -1f, 0,0), new Vector4(0, 1,0,0)),
                new VertexPositionUV(new Vector4(1f, -1f, 0,0), new Vector4(1, 1,0,0))
            };
            ushort[] quadIndices = { 0, 1, 2, 3 };
            var layout = new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4),
                new VertexElementDescription("UV", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4));

            this._mesh = new Mesh<VertexPositionUV>(positions, quadIndices, layout);


            CreateResources(_graphicsDevice, _mesh, texture);
            indexCount = (uint)_mesh.Indicies.Length;
        }
        public override void CreateResources(GraphicsDevice _graphicsDevice)
        {
            CreateResources(_graphicsDevice, _mesh, this.texView);
        }
        void CreateResources(GraphicsDevice _graphicsDevice, Mesh<VertexPositionUV> mesh, TextureView texture)
        {
            Shader shader = ShaderManager.GetShader(_shader);
            ResourceFactory factory = _graphicsDevice.ResourceFactory;


            vertexBuffer = factory.CreateBuffer(new BufferDescription((uint)mesh.Vertices.Length * (uint)Marshal.SizeOf<VertexPositionUV>(), BufferUsage.VertexBuffer));
            indexBuffer = factory.CreateBuffer(new BufferDescription((uint)mesh.Vertices.Length * sizeof(ushort), BufferUsage.IndexBuffer));
            transformBuffer = factory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf<TransformStruct>(), BufferUsage.UniformBuffer));


            _graphicsDevice.UpdateBuffer(vertexBuffer, 0, mesh.Vertices);
            _graphicsDevice.UpdateBuffer(indexBuffer, 0, mesh.Indicies);



            VertexLayoutDescription vertexLayout = mesh.VertexLayout;

            _shaders = shader.shaders;

            GraphicsPipelineDescription pipelineDescription = new GraphicsPipelineDescription();
            pipelineDescription.BlendState = BlendStateDescription.SingleOverrideBlend;
            ResourceLayoutElementDescription[] elementDescriptions = new ResourceLayoutElementDescription[2];
            elementDescriptions[0] = new ResourceLayoutElementDescription("Texture", ResourceKind.TextureReadOnly, ShaderStages.Fragment);
            elementDescriptions[1] = new ResourceLayoutElementDescription("Sampler", ResourceKind.Sampler, ShaderStages.Fragment);
            ResourceLayout uniformResourceLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(elementDescriptions));
            pipelineDescription.DepthStencilState = new DepthStencilStateDescription(
                depthTestEnabled: true,
                depthWriteEnabled: true,
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

            pipelineDescription.Outputs = _graphicsDevice.MainSwapchain.Framebuffer.OutputDescription;
            pipeline = factory.CreateGraphicsPipeline(pipelineDescription);
            BindableResource[] buffers = new BindableResource[2];
            buffers[0] = texView;
            buffers[1] = _graphicsDevice.Aniso4xSampler;

            _transformSet = factory.CreateResourceSet(new ResourceSetDescription(
                uniformResourceLayout,
                buffers));

        }

        public override void Draw(CommandList cl)
        {
            cl.SetVertexBuffer(0, vertexBuffer);
            cl.SetIndexBuffer(indexBuffer, IndexFormat.UInt16);
            cl.SetPipeline(pipeline);
            cl.SetGraphicsResourceSet(0, _transformSet);
            cl.DrawIndexed(
                indexCount: indexCount,
                instanceCount: 1,
                indexStart: 0,
                vertexOffset: 0,
                instanceStart: 0);
        }

        public override void Dispose()
        {
            pipeline.Dispose();
            foreach (Veldrid.Shader shader in _shaders)
            {
                shader.Dispose();
            }
            vertexBuffer.Dispose();
            indexBuffer.Dispose();
            transformBuffer.Dispose();
            this.texView.Dispose();
        }

    }
}