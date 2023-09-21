using System.Numerics;
using System.Runtime.InteropServices;
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
    protected uint _instanceCount = 100u;
    protected TInstanceData[] _instanceData;

    private bool _instancesDirty;
    protected DeviceBuffer _instanceVB;
    protected Mesh<T> _mesh;
    protected string _shader;
    protected List<Texture> _textureAssets = new();
    protected Dictionary<string, TextureView> _textures = new();
    protected PrimitiveTopology _topology;
    private ResourceLayout _transformTextureResourceLayout;
    protected Dictionary<string, DeviceBuffer> _uniformBuffers = new();
    protected ResourceLayout? _uniformResourceLayout;
    protected TUniform drawableUniform;
    protected VertexLayoutDescription instanceLayoutDescription;
    protected Sampler sampler;
    protected ShaderStages transformShaderStages;
    protected ShaderStages uniformShaderStages;

    public InstancedDrawable(InstancedDrawableOptions<T, TUniform, TInstanceData> o)
    {
        instanceLayoutDescription = o.InstanceLayoutDescription;
        _instanceData = o.InstancedData;
        _instanceCount = (uint)o.InstancedData.Length;


        _shaders = o.Shader.Shaders;
        _mesh = o.Mesh ?? new Mesh<T>(new T[0], new ushort[0], new VertexLayoutDescription());
        if (o.Transform == null)
            Debug.Error(LogCategory.Rendering, "Drawable is missing a transform. Drawable can not be properly sorted!");

        _topology = o.Topology ?? PrimitiveTopology.TriangleStrip;
        transform = o.Transform;
        drawableUniform = o.Uniform;
        uniformShaderStages = o.UniformShaderStages ?? ShaderStages.Vertex | ShaderStages.Fragment;
        transformShaderStages = o.TransformShaderStages ?? ShaderStages.Vertex;


        _textureAssets = o.Textures ?? new List<Texture>();
        sampler = o.Sampler ?? Renderer.GraphicsDevice.LinearSampler;
        CreateResources();
    }


    public void CreateResources()
    {
        var graphicsDevice = Renderer.GraphicsDevice!;
        // Make sure our transform knows us
        if (transform != null)
            transform.RegisterDrawable(this);

        var factory = graphicsDevice.ResourceFactory;
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
            factory.CreateBuffer(
                new BufferDescription((uint)Marshal.SizeOf(drawableUniform), BufferUsage.UniformBuffer)));

        graphicsDevice.UpdateBuffer(_uniformBuffers["Default Uniform"], 0, drawableUniform);


        graphicsDevice.UpdateBuffer(vertexBuffer, 0, _mesh.Vertices);
        graphicsDevice.UpdateBuffer(indexBuffer, 0, _mesh.Indices);
        graphicsDevice.UpdateBuffer(transformBuffer, 0,
            new TransformStruct(Matrix4x4.Identity, Matrix4x4.Identity, Camera.GetTransformMatrix()));

        // Next lets load textures to the gpu
        foreach (var texture in _textureAssets)
            _textures.Add(texture.Name, factory.CreateTextureView(texture.TextureData));


        var vertexLayout = _mesh.VertexLayout;


        var pipelineDescription = new GraphicsPipelineDescription();
        pipelineDescription.BlendState = BlendStateDescription.SingleAlphaBlend;
        var elementDescriptions =
            new ResourceLayoutElementDescription[2 + _textures.Count];
        elementDescriptions[0] =
            new ResourceLayoutElementDescription("TransformMatrices", ResourceKind.UniformBuffer,
                transformShaderStages);
        var i = 1;


        foreach (var texture in _textureAssets)
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

        var uniformElementDescriptions =
            new ResourceLayoutElementDescription[_uniformBuffers.Count];
        uniformElementDescriptions[0] = new ResourceLayoutElementDescription(_uniformBuffers["Default Uniform"].Name,
            ResourceKind.UniformBuffer, uniformShaderStages);
        _uniformResourceLayout = factory.CreateResourceLayout(
            new ResourceLayoutDescription(uniformElementDescriptions));

        //  -- Instancing STUFF --


        _instanceVB = factory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf<TInstanceData>() * _instanceCount,
            BufferUsage.VertexBuffer));
        Renderer.GraphicsDevice.UpdateBuffer(_instanceVB, 0, _instanceData);
        instanceLayoutDescription.InstanceStepRate = 1;

        // End of instancing stuff :( (it was fun while it lasted)

        pipelineDescription.DepthStencilState = new DepthStencilStateDescription(
            false,
            false,
            ComparisonKind.LessEqual);

        pipelineDescription.RasterizerState = new RasterizerStateDescription(
            FaceCullMode.Back,
            PolygonFillMode.Solid,
            FrontFace.Clockwise,
            false,
            false);

        pipelineDescription.PrimitiveTopology = _topology;
        pipelineDescription.ResourceLayouts = Array.Empty<ResourceLayout>();
        pipelineDescription.ShaderSet = new ShaderSetDescription(
            new[] { vertexLayout, instanceLayoutDescription },
            _shaders);
        pipelineDescription.ResourceLayouts = new[] { _transformTextureResourceLayout, _uniformResourceLayout };


        pipelineDescription.Outputs = Renderer.PrimaryFramebuffer.OutputDescription;
        pipeline = factory.CreateGraphicsPipeline(pipelineDescription);
        var buffers = new BindableResource[2 + _textures.Count];
        buffers[0] = transformBuffer;
        i = 1;

        foreach (var texView in _textures.Values)
        {
            buffers[i] = texView;
            i++;
        }

        buffers[^1] = sampler;
        _transformSet = factory.CreateResourceSet(new ResourceSetDescription(
            _transformTextureResourceLayout,
            buffers));
        i = 0;
        buffers = new BindableResource[_uniformBuffers.Count];
        foreach (var buffer in _uniformBuffers.Values)
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

    public override void Draw(CommandList cl)
    {
        if (_instanceData.Length == 0)
            return;
        if (_instancesDirty)
        {
            if (_instanceData.Length != _instanceCount)
            {
                _instanceCount = (uint)_instanceData.Length;
                _instanceVB.Dispose();
                _instanceVB = Renderer.GraphicsDevice.ResourceFactory.CreateBuffer(
                    new BufferDescription((uint)Marshal.SizeOf<TInstanceData>() * _instanceCount,
                        BufferUsage.VertexBuffer));
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
            (uint)_mesh.Indices.Length,
            _instanceCount,
            0,
            0,
            0);
    }

    public override void SetUniformBufferValue<TBufferType>(GraphicsDevice graphicsDevice, TBufferType value)
        where TBufferType : struct
    {
        if (_uniformBuffers.ContainsKey("Default Uniform") && !_uniformBuffers["Default Uniform"].IsDisposed)
            graphicsDevice.UpdateBuffer(_uniformBuffers["Default Uniform"], 0, value);
    }

    public override void SetGlobalMatrix(GraphicsDevice graphicsDevice, Matrix4x4 matrix)
    {
        var tmat = transform.GetTransformationMatrix();
        var cmat = Camera.GetTransformMatrix();
        if (transformBuffer != null && !transformBuffer.IsDisposed)
            graphicsDevice.UpdateBuffer(transformBuffer, 0, new TransformStruct(matrix, tmat, cmat));
    }

    public void UpdateInstanceData(TInstanceData[] newData)
    {
        _instancesDirty = true;
        _instanceData = newData;
    }

    public override void Dispose()
    {
        SoftDispose();
        transform.UnregisterDrawable(this);
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
        foreach (var buffer in _uniformBuffers.Values) buffer.Dispose();

        _uniformBuffers.Clear();
        foreach (var texView in _textures.Values) texView.Dispose();

        _textures.Clear();
    }
}