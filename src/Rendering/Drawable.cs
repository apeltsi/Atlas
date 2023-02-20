using System.Numerics;
using System.Runtime.InteropServices;
using SolidCode.Atlas.Components;
using Veldrid;

namespace SolidCode.Atlas.Rendering
{
    /// <summary>
    /// A representation of something that will be drawn onscreen
    /// </summary>
    public abstract class Drawable : IComparable<Drawable>
    {
        public Pipeline pipeline;
        public Veldrid.Shader[] _shaders;
        public DeviceBuffer vertexBuffer;
        public DeviceBuffer indexBuffer;
        public DeviceBuffer transformBuffer;
        public ResourceSet _transformSet;
        public Transform transform;

        public virtual void CreateResources(GraphicsDevice _graphicsDevice)
        {
        }

        public virtual void Dispose()
        {
            Debug.Log(LogCategory.Rendering, "Something went wrong! You shouldn't be disposing an abstract class!");
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
        public virtual void UpdateMeshBuffers()
        {

        }
        public virtual int CompareTo(Drawable x)
        {
            if (this == null)
            {
                return 0;
            }
            if (x == null)
            {
                return 0;
            }
            if (this.transform == null)
            {
                return 0;
            }
            if (x.transform == null)
            {
                return 0;
            }
            return this.transform.globalZ.CompareTo(x.transform.globalZ);
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
        protected string _shader;
        protected Mesh<T> _mesh;
        protected Uniform uniform;
        protected List<string> _texturePrototypes;
        protected Dictionary<string, DeviceBuffer> _uniformBuffers = new Dictionary<string, DeviceBuffer>();
        protected Dictionary<string, TextureView> _textures = new Dictionary<string, TextureView>();

        protected ShaderStages uniformShaderStages;
        protected ShaderStages transformShaderStages;
        protected Sampler sampler;
        public Drawable(GraphicsDevice _graphicsDevice, string shaderPath, Mesh<T> mesh, Transform t, Uniform uniform, ShaderStages uniformShaderStages, List<string>? textures = null, ShaderStages transformShaderStages = ShaderStages.Vertex, Sampler? sampler = null)
        {
            this._shader = shaderPath;
            if (mesh != null)
                this._mesh = mesh;
            if (t == null)
            {
                Debug.Error(LogCategory.Rendering, "Drawable is missing a transform. Drawable can not be properly sorted!");
            }
            this.transform = t;
            this.uniform = uniform;
            this.uniformShaderStages = uniformShaderStages;
            this.transformShaderStages = transformShaderStages;
            if (textures == null)
            {
                textures = new List<string>();
            }
            if (sampler == null)
            {
                sampler = _graphicsDevice.Aniso4xSampler;
            }
            this.sampler = sampler;
            this._texturePrototypes = textures;
            if (mesh != null)
                CreateResources(_graphicsDevice, shaderPath);
        }
        public override void CreateResources(GraphicsDevice _graphicsDevice)
        {
            CreateResources(_graphicsDevice, _shader);
        }

        protected void CreateResources(GraphicsDevice _graphicsDevice, string shaderPath)
        {
            // Make sure our transform knows us
            if(this.transform != null)
                this.transform.RegisterDrawable(this);

            Shader shader = ShaderManager.GetShader(shaderPath);
            ResourceFactory factory = _graphicsDevice.ResourceFactory;
            List<Texture> _loadedTextures = new List<Texture>();
            foreach (string texPath in _texturePrototypes)
            {
                _loadedTextures.Add(new Texture(texPath + ".ktx", texPath, _graphicsDevice, factory));
            }
            vertexBuffer = factory.CreateBuffer(new BufferDescription((uint)_mesh.Vertices.Length * (uint)Marshal.SizeOf<T>(), BufferUsage.VertexBuffer));
            indexBuffer = factory.CreateBuffer(new BufferDescription((uint)_mesh.Indicies.Length * sizeof(ushort), BufferUsage.IndexBuffer));
            transformBuffer = factory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf<TransformStruct>(), BufferUsage.UniformBuffer));

            // Uniform
            _uniformBuffers.Add("Default Uniform", factory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf(uniform), BufferUsage.UniformBuffer)));

            _graphicsDevice.UpdateBuffer(_uniformBuffers["Default Uniform"], 0, uniform);



            _graphicsDevice.UpdateBuffer(vertexBuffer, 0, _mesh.Vertices);
            _graphicsDevice.UpdateBuffer(indexBuffer, 0, _mesh.Indicies);
            _graphicsDevice.UpdateBuffer(transformBuffer, 0, new TransformStruct(Matrix4x4.Identity, Matrix4x4.Identity, Camera.GetTransformMatrix()));

            // Next lets load textures to the gpu
            foreach (Texture texture in _loadedTextures)
            {
                _textures.Add(texture.name, factory.CreateTextureView(texture.texture));
            }


            VertexLayoutDescription vertexLayout = _mesh.VertexLayout;

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
                depthTestEnabled: false,
                depthWriteEnabled: false,
                comparisonKind: ComparisonKind.LessEqual);

            pipelineDescription.RasterizerState = new RasterizerStateDescription(
                cullMode: FaceCullMode.Back,
                fillMode: PolygonFillMode.Solid,
                frontFace: FrontFace.Clockwise,
                depthClipEnabled: false,
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
                buffers[i] = this.sampler;
                i++;
            }
            _transformSet = factory.CreateResourceSet(new ResourceSetDescription(
                uniformResourceLayout,
                buffers));

        }

        public override void UpdateMeshBuffers()
        {
            if (vertexBuffer.SizeInBytes != (uint)_mesh.Vertices.Length * (uint)Marshal.SizeOf<T>())
            {
                vertexBuffer.Dispose();
                vertexBuffer = Window._graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription((uint)_mesh.Vertices.Length * (uint)Marshal.SizeOf<T>(), BufferUsage.VertexBuffer));
            }
            if (indexBuffer.SizeInBytes != (uint)_mesh.Indicies.Length * sizeof(ushort))
            {
                indexBuffer.Dispose();
                indexBuffer = Window._graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription((uint)_mesh.Indicies.Length * sizeof(ushort), BufferUsage.IndexBuffer));
            }
            Window._graphicsDevice.UpdateBuffer(vertexBuffer, 0, _mesh.Vertices);
            Window._graphicsDevice.UpdateBuffer(indexBuffer, 0, _mesh.Indicies);
        }


        // TODO: Add a way to update the textures

        public override void Draw(CommandList cl)
        {
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

        public override void SetUniformBufferValue<TBufferType>(GraphicsDevice _graphicsDevice, TBufferType value) where TBufferType : struct
        {
            _graphicsDevice.UpdateBuffer(_uniformBuffers["Default Uniform"], 0, value);
        }

        public override void SetGlobalMatrix(GraphicsDevice _graphicsDevice, Matrix4x4 matrix)
        {
            Matrix4x4 tmat = transform.GetTransformationMatrix();
            Matrix4x4 cmat = Camera.GetTransformMatrix();
            if (transformBuffer != null && !transformBuffer.IsDisposed)
            {
                // FIXME: This occasionally fails, most likely due to a race-condition of some kind. (try catch did not help!)
                // Could the transformbuffer be updated during updatebuffer?
                _graphicsDevice.UpdateBuffer(transformBuffer, 0, new TransformStruct(matrix, tmat, cmat));
            }
        }

        public override void Dispose()
        {
            // Mby this will help with our problem above
            Window._graphicsDevice.WaitForIdle();
            pipeline.Dispose();

            vertexBuffer.Dispose();
            indexBuffer.Dispose();
            transformBuffer.Dispose();
            this.transform.UnregisterDrawable(this);
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