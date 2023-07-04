using System.Numerics;
using System.Runtime.InteropServices;
using SolidCode.Atlas.AssetManagement;
using SolidCode.Atlas.Components;
using SolidCode.Atlas.ECS;
using Veldrid;

namespace SolidCode.Atlas.Rendering;

public struct InstancedDrawableOptions<T, TUniform, TInstanceData>
    where T : unmanaged
    where TUniform : unmanaged
{
    public Shader Shader; 
    public Mesh<T> Mesh;
    public Transform Transform; 
    public TUniform Uniform; 
    public ShaderStages? UniformShaderStages;
    public List<Texture>? Textures;
    public ShaderStages? TransformShaderStages;
    public Sampler? Sampler;
    public PrimitiveTopology? Topology;
    public TInstanceData[] InstancedData;
    public VertexLayoutDescription InstanceLayoutDescription;
}

public class InstancedDrawable<T, TUniform, TInstanceData> : Drawable
    where T : unmanaged
    where TUniform : unmanaged
    where TInstanceData : unmanaged
{
    protected string _shader;
    protected Mesh<T> _mesh;
    protected TUniform drawableUniform;
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
    protected ResourceLayout? _uniformResourceLayout;
    protected PrimitiveTopology _topology;

    [Obsolete("This constructor is deprecated and will be removed in a future version of Atlas. Please use the other constructor that takes in InstancedDrawableOptions instead.")]
    public InstancedDrawable(string shaderPath, Mesh<T> mesh, Transform t, TInstanceData[] instanceData,VertexLayoutDescription instanceLayoutDesc,
        TUniform drawableUniform, ShaderStages uniformShaderStages, List<Texture>? textures = null,
        ShaderStages transformShaderStages = ShaderStages.Vertex, Sampler? sampler = null)
    {
        this._shaders = AssetManager.GetShader(shaderPath).Shaders;
        this.instanceLayoutDescription = instanceLayoutDesc;
        this._instanceData = instanceData;
        this._instanceCount = (uint)instanceData.Length;

        if (mesh != null)
            this._mesh = mesh;
        else
            this._mesh = new Mesh<T>(new T[0], new ushort[0], new VertexLayoutDescription());
        if (t == null)
        {
            Debug.Error(LogCategory.Rendering, "Drawable is missing a transform. Drawable can not be properly sorted!");
        }

        this.transform = t;
        this.drawableUniform = drawableUniform;
        this.uniformShaderStages = uniformShaderStages;
        this.transformShaderStages = transformShaderStages;
        if (textures == null)
        {
            textures = new List<Texture>();
        }
        
        this._textureAssets = textures;
        this.sampler = sampler ?? Renderer.GraphicsDevice.LinearSampler;
        if (mesh != null)
            CreateResources();

    }
    
    public InstancedDrawable(InstancedDrawableOptions<T, TUniform, TInstanceData> o)
    {
        this.instanceLayoutDescription = o.InstanceLayoutDescription;
        this._instanceData = o.InstancedData;
        this._instanceCount = (uint)o.InstancedData.Length;

        
        this._shaders = o.Shader.Shaders;
        this._mesh = o.Mesh ?? new Mesh<T>(new T[0], new ushort[0], new VertexLayoutDescription());
        if (o.Transform == null)
        {
            Debug.Error(LogCategory.Rendering, "Drawable is missing a transform. Drawable can not be properly sorted!");
        }

        _topology = o.Topology ?? PrimitiveTopology.TriangleStrip;
        this.transform = o.Transform;
        this.drawableUniform = o.Uniform;
        this.uniformShaderStages = o.UniformShaderStages ?? ShaderStages.Vertex | ShaderStages.Fragment;
        this.transformShaderStages = o.TransformShaderStages ?? ShaderStages.Vertex;
            
            
        this._textureAssets = o.Textures ?? new List<Texture>();
        this.sampler = o.Sampler ?? Renderer.GraphicsDevice.LinearSampler;
        CreateResources();
    }

    
    public void CreateResources()
    {
        GraphicsDevice _graphicsDevice = Renderer.GraphicsDevice;
        // Make sure our transform knows us
        if (this.transform != null)
            this.transform.RegisterDrawable(this);

        ResourceFactory factory = _graphicsDevice.ResourceFactory;
        vertexBuffer =
            factory.CreateBuffer(new BufferDescription((uint)_mesh.Vertices.Length * (uint)Marshal.SizeOf<T>(),
                BufferUsage.VertexBuffer));
        indexBuffer =
            factory.CreateBuffer(new BufferDescription((uint)_mesh.Indices.Length * sizeof(ushort),
                BufferUsage.IndexBuffer));
        transformBuffer =
            factory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf<TransformStruct>(),
                BufferUsage.UniformBuffer));
        
        // Uniform
        _uniformBuffers.Add("Default Uniform",
            factory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf(drawableUniform), BufferUsage.UniformBuffer)));

        _graphicsDevice.UpdateBuffer<TUniform>(_uniformBuffers["Default Uniform"], 0, drawableUniform);


        _graphicsDevice.UpdateBuffer(vertexBuffer, 0, _mesh.Vertices);
        _graphicsDevice.UpdateBuffer(indexBuffer, 0, _mesh.Indices);
        _graphicsDevice.UpdateBuffer(transformBuffer, 0,
            new TransformStruct(Matrix4x4.Identity, Matrix4x4.Identity, Camera.GetTransformMatrix()));

        // Next lets load textures to the gpu
        foreach (Texture texture in _textureAssets)
        {
            _textures.Add(texture.Name, factory.CreateTextureView(texture.TextureData));
        }


        VertexLayoutDescription vertexLayout = _mesh.VertexLayout;


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
                new ResourceLayoutElementDescription(texture.Name, ResourceKind.TextureReadOnly, ShaderStages.Fragment);
            i++;
            i++;
        }

        elementDescriptions[^1] =
            new ResourceLayoutElementDescription("Sampler", ResourceKind.Sampler, ShaderStages.Fragment);

        _transformTextureResourceLayout = factory.CreateResourceLayout(
            new ResourceLayoutDescription(elementDescriptions));

        // Next up we have to create the layout for our uniform
        
        ResourceLayoutElementDescription[] uniformElementDescriptions =
            new ResourceLayoutElementDescription[_uniformBuffers.Count];
        uniformElementDescriptions[0] = new ResourceLayoutElementDescription(_uniformBuffers["Default Uniform"].Name,
            ResourceKind.UniformBuffer, this.uniformShaderStages);
        _uniformResourceLayout = factory.CreateResourceLayout(
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

        pipelineDescription.PrimitiveTopology = _topology;
        pipelineDescription.ResourceLayouts = System.Array.Empty<ResourceLayout>();
        pipelineDescription.ShaderSet = new ShaderSetDescription(
            vertexLayouts: new VertexLayoutDescription[] { vertexLayout, instanceLayoutDescription },
            shaders: _shaders);
        pipelineDescription.ResourceLayouts = new[] { _transformTextureResourceLayout, _uniformResourceLayout };


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
            _transformTextureResourceLayout,
            buffers));
        i = 0;
        buffers = new BindableResource[_uniformBuffers.Count];
        foreach (DeviceBuffer buffer in _uniformBuffers.Values)
        {
            buffers[i] = buffer;
            i++;
        }

        _uniformSet = factory.CreateResourceSet(new ResourceSetDescription(
            _uniformResourceLayout,
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

        if (indexBuffer.SizeInBytes != (uint)_mesh.Indices.Length * sizeof(ushort))
        {
            indexBuffer.Dispose();
            indexBuffer = Renderer.GraphicsDevice.ResourceFactory.CreateBuffer(
                new BufferDescription((uint)_mesh.Indices.Length * sizeof(ushort), BufferUsage.IndexBuffer));
        }

        Renderer.GraphicsDevice.UpdateBuffer(vertexBuffer, 0, _mesh.Vertices);
        Renderer.GraphicsDevice.UpdateBuffer(indexBuffer, 0, _mesh.Indices);
    }


    public override void UpdateTexture(Texture texture, int index)
    {
        _textureAssets[index] = texture;
        SoftDispose();
        CreateResources();
    }

    private bool _instancesDirty = false;
    private ResourceLayout _transformTextureResourceLayout;

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
            indexCount: (uint)_mesh.Indices.Length,
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
        _uniformResourceLayout?.Dispose();
        _transformTextureResourceLayout?.Dispose();
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