using System.Numerics;
using System.Runtime.InteropServices;
using SolidCode.Atlas.Components;
using Veldrid;
using SolidCode.Atlas.ECS;

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
        protected ResourceSet _transformSet;
        protected ResourceSet _uniformSet;
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

        public virtual void SetUniformBufferValue<TBufferType>(GraphicsDevice _graphicsDevice, TBufferType value) where TBufferType : unmanaged
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



    public class Drawable<T, Uniform> : Drawable
    where T : unmanaged
    where Uniform : unmanaged
    {
        protected string _shader;
        protected Mesh<T> _mesh;
        protected Uniform textUniform;
        protected Dictionary<string, DeviceBuffer> _uniformBuffers = new Dictionary<string, DeviceBuffer>();
        protected Dictionary<string, TextureView> _textures = new Dictionary<string, TextureView>();
        protected List<Texture> _textureAssets = new List<Texture>();
        protected ShaderStages uniformShaderStages;
        protected ShaderStages transformShaderStages;
        protected Sampler sampler;
        public Drawable(GraphicsDevice _graphicsDevice, string shaderPath, Mesh<T> mesh, Transform t, Uniform textUniform, ShaderStages uniformShaderStages, List<Texture>? textures = null, ShaderStages transformShaderStages = ShaderStages.Vertex, Sampler? sampler = null)
        {
            this._shader = shaderPath;
            if (mesh != null)
                this._mesh = mesh;
            else
                this._mesh = new Mesh<T>(new T[0], new ushort[0], new VertexLayoutDescription());
            if (t == null)
            {
                Debug.Error(LogCategory.Rendering, "Drawable is missing a transform. Drawable can not be properly sorted!");
            }
            this.transform = t;
            this.textUniform = textUniform;
            this.uniformShaderStages = uniformShaderStages;
            this.transformShaderStages = transformShaderStages;
            if (textures == null)
            {
                textures = new List<Texture>();
            }
            if (sampler == null)
            {
                sampler = _graphicsDevice.LinearSampler;
            }
            this._textureAssets = textures;
            this.sampler = sampler;
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
            if (this.transform != null)
                this.transform.RegisterDrawable(this);

            Shader shader = ShaderManager.GetShader(shaderPath);
            ResourceFactory factory = _graphicsDevice.ResourceFactory;
            vertexBuffer = factory.CreateBuffer(new BufferDescription((uint)_mesh.Vertices.Length * (uint)Marshal.SizeOf<T>(), BufferUsage.VertexBuffer));
            indexBuffer = factory.CreateBuffer(new BufferDescription((uint)_mesh.Indicies.Length * sizeof(ushort), BufferUsage.IndexBuffer));
            transformBuffer = factory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf<TransformStruct>(), BufferUsage.UniformBuffer));

            // Uniform
            _uniformBuffers.Add("Default Uniform", factory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf(textUniform), BufferUsage.UniformBuffer)));

            _graphicsDevice.UpdateBuffer<Uniform>(_uniformBuffers["Default Uniform"], 0, textUniform);


            _graphicsDevice.UpdateBuffer(vertexBuffer, 0, _mesh.Vertices);
            _graphicsDevice.UpdateBuffer(indexBuffer, 0, _mesh.Indicies);
            _graphicsDevice.UpdateBuffer(transformBuffer, 0, new TransformStruct(Matrix4x4.Identity, Matrix4x4.Identity, Camera.GetTransformMatrix()));

            // Next lets load textures to the gpu
            foreach (Texture texture in _textureAssets)
            {
                _textures.Add(texture.name, factory.CreateTextureView(texture.texture));
            }


            VertexLayoutDescription vertexLayout = _mesh.VertexLayout;

            _shaders = shader.shaders;

            GraphicsPipelineDescription pipelineDescription = new GraphicsPipelineDescription();
            pipelineDescription.BlendState = BlendStateDescription.SingleAlphaBlend;
            ResourceLayoutElementDescription[] elementDescriptions = new ResourceLayoutElementDescription[2 + _textures.Count];
            elementDescriptions[0] = new ResourceLayoutElementDescription("TransformMatrices", ResourceKind.UniformBuffer, transformShaderStages);
            int i = 1;


            foreach (Texture texture in _textureAssets)
            {
                elementDescriptions[i] = new ResourceLayoutElementDescription(texture.name, ResourceKind.TextureReadOnly, ShaderStages.Fragment);
                i++;
                i++;
            }
            elementDescriptions[^1] = new ResourceLayoutElementDescription("Sampler", ResourceKind.Sampler, ShaderStages.Fragment);

            ResourceLayout transformTextureResourceLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(elementDescriptions));

            // Next up we have to create the layout for our uniform
            ResourceLayoutElementDescription[] uniformElementDescriptions = new ResourceLayoutElementDescription[_uniformBuffers.Count];
            uniformElementDescriptions[0] = new ResourceLayoutElementDescription(_uniformBuffers["Default Uniform"].Name, ResourceKind.UniformBuffer, this.uniformShaderStages);
            ResourceLayout uniformResourceLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(uniformElementDescriptions));

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
            pipelineDescription.ResourceLayouts = new[] { transformTextureResourceLayout, uniformResourceLayout };

            pipelineDescription.Outputs = Window.PrimaryFramebuffer.OutputDescription;
            pipeline = factory.CreateGraphicsPipeline(pipelineDescription);
            BindableResource[] buffers = new BindableResource[2 + _textures.Count];
            buffers[0] = transformBuffer;
            i = 1;

            foreach (TextureView texView in _textures.Values)
            {
                buffers[i] = texView;
                i++;
            }
            buffers[^1] = this.sampler;
            _transformSet = factory.CreateResourceSet(new ResourceSetDescription(
                transformTextureResourceLayout,
                buffers));
            i = 0;
            buffers = new BindableResource[_uniformBuffers.Count];
            foreach (DeviceBuffer buffer in _uniformBuffers.Values)
            {
                buffers[i] = buffer;
                i++;
            }

            _uniformSet = factory.CreateResourceSet(new ResourceSetDescription(
                uniformResourceLayout,
                buffers));

        }

        public override void UpdateMeshBuffers()
        {
            if (vertexBuffer.SizeInBytes != (uint)_mesh.Vertices.Length * (uint)Marshal.SizeOf<T>())
            {
                vertexBuffer.Dispose();
                vertexBuffer = Window.GraphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription((uint)_mesh.Vertices.Length * (uint)Marshal.SizeOf<T>(), BufferUsage.VertexBuffer));
            }
            if (indexBuffer.SizeInBytes != (uint)_mesh.Indicies.Length * sizeof(ushort))
            {
                indexBuffer.Dispose();
                indexBuffer = Window.GraphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription((uint)_mesh.Indicies.Length * sizeof(ushort), BufferUsage.IndexBuffer));
            }
            Window.GraphicsDevice.UpdateBuffer(vertexBuffer, 0, _mesh.Vertices);
            Window.GraphicsDevice.UpdateBuffer(indexBuffer, 0, _mesh.Indicies);
        }


        public void UpdateTexture(Texture texture, int index)
        {
            _textureAssets[index] = texture;
            SoftDispose();
            CreateResources(Window.GraphicsDevice);
        }

        public override void Draw(CommandList cl)
        {
            cl.SetVertexBuffer(0, vertexBuffer);
            cl.SetIndexBuffer(indexBuffer, IndexFormat.UInt16);
            cl.SetPipeline(pipeline);
            cl.SetGraphicsResourceSet(0, _transformSet);
            cl.SetGraphicsResourceSet(1, _uniformSet);
            cl.DrawIndexed(
                indexCount: (uint)_mesh.Indicies.Length,
                instanceCount: 1,
                indexStart: 0,
                vertexOffset: 0,
                instanceStart: 0);
        }

        public override void SetUniformBufferValue<TBufferType>(GraphicsDevice _graphicsDevice, TBufferType value) where TBufferType : struct
        {
            if (_uniformBuffers.ContainsKey("Default Uniform") && !_uniformBuffers["Default Uniform"].IsDisposed)
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
            SoftDispose();
            this.transform.UnregisterDrawable(this);

        }
        
        public void SoftDispose()
        {
            // Mby this will help with our problem above
            Window.GraphicsDevice.WaitForIdle();
            pipeline.Dispose();

            vertexBuffer.Dispose();
            indexBuffer.Dispose();
            transformBuffer.Dispose();
            _transformSet.Dispose();
            if (_uniformSet != null)
                _uniformSet.Dispose();
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