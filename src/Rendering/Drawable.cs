using System.Numerics;
using System.Runtime.InteropServices;
using SolidCode.Caerus.Components;
using Veldrid;

namespace SolidCode.Caerus.Rendering
{
    /// <summary>
    /// A representation of something that will be drawn onscreen
    /// </summary>
    public abstract class Drawable
    {
        public Pipeline pipeline;
        public Veldrid.Shader[] _shaders;
        public DeviceBuffer vertexBuffer;
        public DeviceBuffer indexBuffer;
        public DeviceBuffer uniformBuffer;
        public DeviceBuffer screenSizeUniformBuffer;
        public ResourceSet _transformSet;
        public uint indexCount = 0;

        public virtual void CreateResources(GraphicsDevice _graphicsDevice)
        {
        }

        public virtual void Dispose()
        {
            Debug.Log(LogCategories.Rendering, "Something went wrong! You shouldn't be disposing an abstract class!");
        }

        public virtual void SetGlobalMatrix(GraphicsDevice _graphicsDevice, Matrix4x4 matrix)
        {
            _graphicsDevice.UpdateBuffer(uniformBuffer, 0, matrix);
        }

        public virtual void Draw(CommandList cl)
        {

        }

        public virtual void SetScreenSize(GraphicsDevice _graphicsDevice, Vector2 size)
        {
        }
    }
    public struct TransformStruct
    {
        Matrix4x4 Screen;
        Matrix4x4 Transform;
        Matrix4x4 Camera;

        public TransformStruct(Matrix4x4 matrix, Matrix4x4 transform, Matrix4x4 camera)
        {
            Screen = matrix;
            Transform = transform;
            Camera = camera;
        }
    }

    public struct ScreenSizeStruct
    {
        Vector4 WindowSize;

        public ScreenSizeStruct(Vector2 size)
        {
            this.WindowSize = new Vector4(size.X, size.Y, 0, 0);
        }
    }

    public class Drawable<T> : Drawable where T : struct
    {
        public Transform transform;
        private string _shader;
        private Mesh<T> _mesh;

        public Drawable(GraphicsDevice _graphicsDevice, string shaderPath, Mesh<T> mesh, Transform t)
        {
            this._shader = shaderPath;
            this._mesh = mesh;
            this.transform = t;
            CreateResources(_graphicsDevice, mesh, shaderPath);
            indexCount = (uint)mesh.Indicies.Length;
            Debug.Log(LogCategories.Rendering, "Drawable resources created");
        }
        public override void CreateResources(GraphicsDevice _graphicsDevice)
        {
            CreateResources(_graphicsDevice, _mesh, _shader);
        }
        void CreateResources(GraphicsDevice _graphicsDevice, Mesh<T> mesh, string shaderPath)
        {
            Shader shader = ShaderManager.GetShader(shaderPath);
            ResourceFactory factory = _graphicsDevice.ResourceFactory;



            vertexBuffer = factory.CreateBuffer(new BufferDescription((uint)mesh.Vertices.Length * (uint)Marshal.SizeOf<T>(), BufferUsage.VertexBuffer));
            indexBuffer = factory.CreateBuffer(new BufferDescription((uint)mesh.Vertices.Length * sizeof(ushort), BufferUsage.IndexBuffer));
            uniformBuffer = factory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf<TransformStruct>(), BufferUsage.UniformBuffer));
            screenSizeUniformBuffer = factory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf<ScreenSizeStruct>(), BufferUsage.UniformBuffer));

            _graphicsDevice.UpdateBuffer(vertexBuffer, 0, mesh.Vertices);
            _graphicsDevice.UpdateBuffer(indexBuffer, 0, mesh.Indicies);
            _graphicsDevice.UpdateBuffer(uniformBuffer, 0, new TransformStruct(Matrix4x4.Identity, Matrix4x4.Identity, Camera.GetTransformMatrix()));
            _graphicsDevice.UpdateBuffer(screenSizeUniformBuffer, 0, new ScreenSizeStruct(Vector2.One));

            VertexLayoutDescription vertexLayout = mesh.VertexLayout;

            _shaders = shader.shaders;

            GraphicsPipelineDescription pipelineDescription = new GraphicsPipelineDescription();
            pipelineDescription.BlendState = BlendStateDescription.SingleOverrideBlend;
            ResourceLayout uniformResourceLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("TransformMatrices", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("ScreenSize", ResourceKind.UniformBuffer, ShaderStages.Fragment)));
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
            _transformSet = factory.CreateResourceSet(new ResourceSetDescription(
                uniformResourceLayout,
                uniformBuffer, screenSizeUniformBuffer));

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

        public override void SetScreenSize(GraphicsDevice _graphicsDevice, Vector2 size)
        {
            _graphicsDevice.UpdateBuffer(screenSizeUniformBuffer, 0, new ScreenSizeStruct(size));
        }

        public override void SetGlobalMatrix(GraphicsDevice _graphicsDevice, Matrix4x4 matrix)
        {
            _graphicsDevice.UpdateBuffer(uniformBuffer, 0, new TransformStruct(matrix, transform.GetTransformationMatrix(), Camera.GetTransformMatrix()));
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
            uniformBuffer.Dispose();
            screenSizeUniformBuffer.Dispose();
        }

    }
}