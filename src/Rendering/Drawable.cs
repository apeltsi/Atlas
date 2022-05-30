using System.Numerics;
using System.Runtime.InteropServices;
using SolidCode.Caerus.Components;
using Veldrid;

namespace SolidCode.Caerus.Rendering
{
    public abstract class Drawable
    {
        public Pipeline pipeline;
        public Veldrid.Shader[] _shaders;
        public DeviceBuffer vertexBuffer;
        public DeviceBuffer indexBuffer;
        public DeviceBuffer uniformBuffer;

        public void Dispose()
        {
            Debug.Log(LogCategories.Rendering, "Something went wrong! You shouldn't be disposing an abstract class!");
        }

        public void SetGlobalMatrix(GraphicsDevice _graphicsDevice, Matrix4x4 matrix)
        {
            _graphicsDevice.UpdateBuffer(uniformBuffer, 0, matrix);
        }

    }
    /// <summary>
    /// A representation of something that will be drawn onscreen
    /// </summary>
    public struct TransformStruct
    {
        Matrix4x4 Matrix;

        public TransformStruct(Matrix4x4 matrix)
        {
            Matrix = matrix;
        }
    }
    public class Drawable<T> : Drawable where T : struct
    {
        public Transform transform;
        public Drawable(GraphicsDevice _graphicsDevice, Shader shader, Mesh<T> mesh, Transform t)
        {
            this.transform = t;
            CreateResources(_graphicsDevice, mesh, shader);
            Debug.Log(LogCategories.Rendering, "Drawable resources created");
        }
        void CreateResources(GraphicsDevice _graphicsDevice, Mesh<T> mesh, Shader shader)
        {
            ResourceFactory factory = _graphicsDevice.ResourceFactory;



            vertexBuffer = factory.CreateBuffer(new BufferDescription((uint)mesh.Vertices.Length * (uint)Marshal.SizeOf<T>(), BufferUsage.VertexBuffer));
            indexBuffer = factory.CreateBuffer(new BufferDescription((uint)mesh.Vertices.Length * sizeof(ushort), BufferUsage.IndexBuffer));
            uniformBuffer = factory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf<TransformStruct>(), BufferUsage.UniformBuffer));
            var uniformLayout = new ResourceLayoutElementDescription("TransformMatrices", ResourceKind.UniformBuffer, ShaderStages.Vertex);
            ResourceLayout uniformResourceLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(uniformLayout));
            _graphicsDevice.UpdateBuffer(vertexBuffer, 0, mesh.Vertices);
            _graphicsDevice.UpdateBuffer(indexBuffer, 0, mesh.Indicies);
            _graphicsDevice.UpdateBuffer(uniformBuffer, 0, new TransformStruct(Matrix4x4.Identity));


            VertexLayoutDescription vertexLayout = mesh.VertexLayout;

            _shaders = shader.shaders;

            GraphicsPipelineDescription pipelineDescription = new GraphicsPipelineDescription();
            pipelineDescription.BlendState = BlendStateDescription.SingleOverrideBlend;

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

            pipelineDescription.Outputs = _graphicsDevice.SwapchainFramebuffer.OutputDescription;
            pipeline = factory.CreateGraphicsPipeline(pipelineDescription);
        }

        public void SetGlobalMatrix(GraphicsDevice _graphicsDevice, Matrix4x4 matrix)
        {
            Debug.Log("setting global matrix");
            _graphicsDevice.UpdateBuffer(uniformBuffer, 0, new TransformStruct(matrix));
        }

        public void Dispose()
        {
            pipeline.Dispose();
            foreach (Veldrid.Shader shader in _shaders)
            {
                shader.Dispose();
            }
            vertexBuffer.Dispose();
            indexBuffer.Dispose();
        }

    }
}