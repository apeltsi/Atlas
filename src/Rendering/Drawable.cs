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
        public DeviceBuffer transformBuffer;
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
        }

        public virtual void SetUniformBufferValue<TBufferType>(GraphicsDevice _graphicsDevice, TBufferType value) where TBufferType : struct
        {

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



    public class Drawable<T, Uniform> : Drawable where T : struct where Uniform : struct
    {
        public Transform transform;
        private string _shader;
        private Mesh<T> _mesh;
        private Uniform uniform;
        private List<string> _texturePrototypes;
        private Dictionary<string, DeviceBuffer> _uniformBuffers = new Dictionary<string, DeviceBuffer>();
        private Dictionary<string, TextureView> _textures = new Dictionary<string, TextureView>();

        ShaderStages uniformShaderStages;
        ShaderStages transformShaderStages;
        public Drawable(GraphicsDevice _graphicsDevice, string shaderPath, Mesh<T> mesh, Transform t, Uniform uniform, ShaderStages uniformShaderStages, List<string>? textures = null, ShaderStages transformShaderStages = ShaderStages.Vertex)
        {
            this._shader = shaderPath;
            this._mesh = mesh;
            this.transform = t;
            this.uniform = uniform;
            this.uniformShaderStages = uniformShaderStages;
            this.transformShaderStages = transformShaderStages;
            if (textures == null)
            {
                textures = new List<string>();
            }
            this._texturePrototypes = textures;

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
            List<Texture> _loadedTextures = new List<Texture>();
            foreach (string texPath in _texturePrototypes)
            {
                _loadedTextures.Add(new Texture(texPath + ".ktx", texPath, _graphicsDevice, factory));
            }


            vertexBuffer = factory.CreateBuffer(new BufferDescription((uint)mesh.Vertices.Length * (uint)Marshal.SizeOf<T>(), BufferUsage.VertexBuffer));
            indexBuffer = factory.CreateBuffer(new BufferDescription((uint)mesh.Vertices.Length * sizeof(ushort), BufferUsage.IndexBuffer));
            transformBuffer = factory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf<TransformStruct>(), BufferUsage.UniformBuffer));

            // Uniform
            _uniformBuffers.Add("Default Uniform", factory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf(uniform), BufferUsage.UniformBuffer)));

            _graphicsDevice.UpdateBuffer(_uniformBuffers["Default Uniform"], 0, uniform);



            _graphicsDevice.UpdateBuffer(vertexBuffer, 0, mesh.Vertices);
            _graphicsDevice.UpdateBuffer(indexBuffer, 0, mesh.Indicies);
            _graphicsDevice.UpdateBuffer(transformBuffer, 0, new TransformStruct(Matrix4x4.Identity, Matrix4x4.Identity, Camera.GetTransformMatrix()));

            // Next lest load textures to the gpu
            foreach (Texture texture in _loadedTextures)
            {
                _textures.Add(texture.name, factory.CreateTextureView(texture.texture));
            }


            VertexLayoutDescription vertexLayout = mesh.VertexLayout;

            _shaders = shader.shaders;

            GraphicsPipelineDescription pipelineDescription = new GraphicsPipelineDescription();
            pipelineDescription.BlendState = BlendStateDescription.SingleAlphaBlend;
            ResourceLayoutElementDescription[] elementDescriptions = new ResourceLayoutElementDescription[1 + _uniformBuffers.Count + _textures.Count * 2];
            elementDescriptions[0] = new ResourceLayoutElementDescription("TransformMatrices", ResourceKind.UniformBuffer, transformShaderStages);
            int i = 1;

            elementDescriptions[i] = new ResourceLayoutElementDescription("Default Uniform", ResourceKind.UniformBuffer, uniformShaderStages);
            i++;

            foreach (Texture texture in _loadedTextures)
            {
                elementDescriptions[i] = new ResourceLayoutElementDescription(texture.name, ResourceKind.TextureReadOnly, ShaderStages.Fragment);
                i++;
                elementDescriptions[i] = new ResourceLayoutElementDescription(texture.name + "Sampler", ResourceKind.Sampler, ShaderStages.Fragment);
                i++;
            }
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

            pipelineDescription.Outputs = Window.DuplicatorFramebuffer.OutputDescription;
            pipeline = factory.CreateGraphicsPipeline(pipelineDescription);
            BindableResource[] buffers = new BindableResource[1 + _uniformBuffers.Count + _textures.Count * 2];
            buffers[0] = transformBuffer;
            i = 1;
            foreach (DeviceBuffer buffer in _uniformBuffers.Values)
            {
                buffers[i] = buffer;
                i++;
            }
            foreach (TextureView texView in _textures.Values)
            {
                buffers[i] = texView;
                i++;
                buffers[i] = _graphicsDevice.Aniso4xSampler;
                i++;
            }
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

        public override void SetUniformBufferValue<TBufferType>(GraphicsDevice _graphicsDevice, TBufferType value) where TBufferType : struct
        {
            _graphicsDevice.UpdateBuffer(_uniformBuffers["Default Uniform"], 0, value);
        }

        public override void SetGlobalMatrix(GraphicsDevice _graphicsDevice, Matrix4x4 matrix)
        {
            _graphicsDevice.UpdateBuffer(transformBuffer, 0, new TransformStruct(matrix, transform.GetTransformationMatrix(), Camera.GetTransformMatrix()));
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
            foreach (DeviceBuffer buffer in _uniformBuffers.Values)
            {
                buffer.Dispose();
            }
            _uniformBuffers.Clear();
            foreach (TextureView texView in _textures.Values)
            {
                texView.Dispose();
            }
            _textures.Clear();

        }

    }
}