using System.Numerics;
using System.Runtime.InteropServices;
using SolidCode.Atlas.AssetManagement;
using SolidCode.Atlas.Components;
using SolidCode.Atlas.ECS;
using Veldrid;

namespace SolidCode.Atlas.Rendering;

public class InstancedDrawable<T, TUniform, TInstanceData> : Drawable
    where T : unmanaged
    where TUniform : unmanaged
    where TInstanceData : unmanaged
{
    protected string _shader;
    protected Mesh<T> _mesh;
    protected TUniform textUniform;
    protected Dictionary<string, DeviceBuffer> _uniformBuffers = new ();
    protected Dictionary<string, TextureView> _textures = new ();
    protected List<Texture> _textureAssets = new ();
    protected ShaderStages uniformShaderStages;
    protected ShaderStages transformShaderStages;
    protected Sampler sampler;
    protected uint _instanceCount = 100u;
    protected VertexLayoutDescription instanceLayoutDescription;
    protected DeviceBuffer _instanceVB;
    protected TInstanceData[] _instanceData;

    public InstancedDrawable(string shaderPath, Mesh<T> mesh, Transform t, TInstanceData[] instanceData,VertexLayoutDescription instanceLayoutDesc,
        TUniform textUniform, ShaderStages uniformShaderStages, List<Texture>? textures = null,
        ShaderStages transformShaderStages = ShaderStages.Vertex, Sampler? sampler = null)
    {
        this._shader = shaderPath;
        this.instanceLayoutDescription = instanceLayoutDesc;
        this._instanceData = instanceData;
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
            sampler = Renderer.GraphicsDevice.LinearSampler;
        }

        this._textureAssets = textures;
        this.sampler = sampler;
        this._instanceCount = (uint)instanceData.Length;
        if (mesh != null)
            CreateResources(Renderer.GraphicsDevice, shaderPath);

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

        Shader shader = AssetManager.GetAsset<Shader>(shaderPath);
        ResourceFactory factory = _graphicsDevice.ResourceFactory;
        vertexBuffer =
            factory.CreateBuffer(new BufferDescription((uint)_mesh.Vertices.Length * (uint)Marshal.SizeOf<T>(),
                BufferUsage.VertexBuffer));
        indexBuffer =
            factory.CreateBuffer(new BufferDescription((uint)_mesh.Indicies.Length * sizeof(ushort),
                BufferUsage.IndexBuffer));
        transformBuffer =
            factory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf<TransformStruct>(),
                BufferUsage.UniformBuffer));
        
        // Uniform
        _uniformBuffers.Add("Default Uniform",
            factory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf(textUniform), BufferUsage.UniformBuffer)));

        _graphicsDevice.UpdateBuffer<TUniform>(_uniformBuffers["Default Uniform"], 0, textUniform);


        _graphicsDevice.UpdateBuffer(vertexBuffer, 0, _mesh.Vertices);
        _graphicsDevice.UpdateBuffer(indexBuffer, 0, _mesh.Indicies);
        _graphicsDevice.UpdateBuffer(transformBuffer, 0,
            new TransformStruct(Matrix4x4.Identity, Matrix4x4.Identity, Camera.GetTransformMatrix()));

        // Next lets load textures to the gpu
        foreach (Texture texture in _textureAssets)
        {
            _textures.Add(texture.name, factory.CreateTextureView(texture.texture));
        }


        VertexLayoutDescription vertexLayout = _mesh.VertexLayout;

        _shaders = shader.shaders;

        GraphicsPipelineDescription pipelineDescription = new GraphicsPipelineDescription();
        pipelineDescription.BlendState = BlendStateDescription.SingleAlphaBlend;
        ResourceLayoutElementDescription[] elementDescriptions =
            new ResourceLayoutElementDescription[2 + _textures.Count];
        elementDescriptions[0] =
            new ResourceLayoutElementDescription("TransformMatrices", ResourceKind.UniformBuffer,
                transformShaderStages);
        int i = 1;


        foreach (Texture texture in _textureAssets)
        {
            elementDescriptions[i] =
                new ResourceLayoutElementDescription(texture.name, ResourceKind.TextureReadOnly, ShaderStages.Fragment);
            i++;
            i++;
        }

        elementDescriptions[^1] =
            new ResourceLayoutElementDescription("Sampler", ResourceKind.Sampler, ShaderStages.Fragment);

        ResourceLayout transformTextureResourceLayout = factory.CreateResourceLayout(
            new ResourceLayoutDescription(elementDescriptions));

        // Next up we have to create the layout for our uniform
        
        ResourceLayoutElementDescription[] uniformElementDescriptions =
            new ResourceLayoutElementDescription[_uniformBuffers.Count];
        uniformElementDescriptions[0] = new ResourceLayoutElementDescription(_uniformBuffers["Default Uniform"].Name,
            ResourceKind.UniformBuffer, this.uniformShaderStages);
        ResourceLayout uniformResourceLayout = factory.CreateResourceLayout(
            new ResourceLayoutDescription(uniformElementDescriptions));
        
        //  -- Instancing STUFF --
        
        
        _instanceVB = factory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf<TInstanceData>() * _instanceCount, BufferUsage.VertexBuffer));
        Renderer.GraphicsDevice.UpdateBuffer(_instanceVB, 0, _instanceData);
        instanceLayoutDescription.InstanceStepRate = 1;
        
        // End of instancing stuff :( (it was fun while it lasted)
        
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
            vertexLayouts: new VertexLayoutDescription[] { vertexLayout, instanceLayoutDescription },
            shaders: _shaders);
        pipelineDescription.ResourceLayouts = new[] { transformTextureResourceLayout, uniformResourceLayout };


        pipelineDescription.Outputs = Renderer.PrimaryFramebuffer.OutputDescription;
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
            vertexBuffer = Renderer.GraphicsDevice.ResourceFactory.CreateBuffer(
                new BufferDescription((uint)_mesh.Vertices.Length * (uint)Marshal.SizeOf<T>(),
                    BufferUsage.VertexBuffer));
        }

        if (indexBuffer.SizeInBytes != (uint)_mesh.Indicies.Length * sizeof(ushort))
        {
            indexBuffer.Dispose();
            indexBuffer = Renderer.GraphicsDevice.ResourceFactory.CreateBuffer(
                new BufferDescription((uint)_mesh.Indicies.Length * sizeof(ushort), BufferUsage.IndexBuffer));
        }

        Renderer.GraphicsDevice.UpdateBuffer(vertexBuffer, 0, _mesh.Vertices);
        Renderer.GraphicsDevice.UpdateBuffer(indexBuffer, 0, _mesh.Indicies);
    }


    public override void UpdateTexture(Texture texture, int index)
    {
        _textureAssets[index] = texture;
        SoftDispose();
        CreateResources(Renderer.GraphicsDevice);
    }

    private bool _instancesDirty = false;

    public override void Draw(CommandList cl)
    {
        if (_instancesDirty)
        {
            if (_instanceData.Length != _instanceCount)
            {
                _instanceCount = (uint)_instanceData.Length;
                _instanceVB.Dispose();
                _instanceVB = Renderer.GraphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf<TInstanceData>() * _instanceCount, BufferUsage.VertexBuffer));
            }
            Renderer.GraphicsDevice.UpdateBuffer(_instanceVB, 0, _instanceData);
            _instancesDirty = false;
        }
        cl.SetPipeline(pipeline);
        cl.SetIndexBuffer(indexBuffer, IndexFormat.UInt16);
        cl.SetVertexBuffer(0, vertexBuffer);
        cl.SetVertexBuffer(1, _instanceVB);

        cl.SetGraphicsResourceSet(0, _transformSet);
        cl.SetGraphicsResourceSet(1, _uniformSet);
        cl.DrawIndexed(
            indexCount: (uint)_mesh.Indicies.Length,
            instanceCount: _instanceCount,
            indexStart: 0,
            vertexOffset: 0,
            instanceStart: 0);
    }

    public override void SetUniformBufferValue<TBufferType>(GraphicsDevice _graphicsDevice, TBufferType value)
        where TBufferType : struct
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

    public void UpdateInstanceData(TInstanceData[] newData)
    {
        _instancesDirty = true;
        this._instanceData = newData;
    }

    public override void Dispose()
    {
        SoftDispose();
        this.transform.UnregisterDrawable(this);
    }

    public void SoftDispose()
    {
        // Mby this will help with our problem above
        Renderer.GraphicsDevice.WaitForIdle();
        pipeline.Dispose();

        vertexBuffer.Dispose();
        indexBuffer.Dispose();
        transformBuffer.Dispose();
        _transformSet.Dispose();
        _instanceVB.Dispose();
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