using System.Numerics;
using System.Runtime.InteropServices;
using SolidCode.Atlas.Components;
using SolidCode.Atlas.ECS;
using SolidCode.Atlas.UI;
using Veldrid;

namespace SolidCode.Atlas.Rendering;

/// <summary>
/// A representation of something that will be drawn onscreen
/// </summary>
public abstract class Drawable : IComparable<Drawable>
{
    public Veldrid.Shader[] _shaders;
    protected ResourceSet _transformSet;
    protected ResourceSet _uniformSet;
    public DeviceBuffer indexBuffer;
    public Pipeline pipeline;
    public Transform transform;
    public DeviceBuffer transformBuffer;
    public DeviceBuffer vertexBuffer;

    public virtual int CompareTo(Drawable x)
    {
        if (this == null) return 0;

        if (x == null) return 0;

        if (transform == null) return 0;

        if (x.transform == null) return 0;

        return transform.GlobalZ.CompareTo(x.transform.GlobalZ);
    }


    public virtual void CreateResources()
    {
    }

    public virtual void Dispose()
    {
        Debug.Log(LogCategory.Rendering, "Something went wrong! You shouldn't be disposing an abstract class!");
    }

    public virtual void SetGlobalMatrix(GraphicsDevice graphicsDevice, Matrix4x4 matrix)
    {
    }

    public virtual void SetUniformBufferValue<TBufferType>(GraphicsDevice graphicsDevice, TBufferType value)
        where TBufferType : unmanaged
    {
    }


    public virtual void Draw(CommandList cl)
    {
    }

    public virtual void SetScreenSize(GraphicsDevice graphicsDevice, Vector2 size)
    {
    }

    public virtual void UpdateMeshBuffers()
    {
    }

    public virtual void UpdateTexture(Texture texture, int index)
    {
    }
}

public struct TransformStruct
{
    private Matrix4x4 Screen;
    private Matrix4x4 Transform;
    private Matrix4x4 Camera;

    public TransformStruct(Matrix4x4 matrix, Matrix4x4 transform, Matrix4x4 camera)
    {
        Screen = matrix;
        Transform = transform;
        Camera = camera;
    }
}

public struct DrawableOptions<T, TUniform>
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
}

public class Drawable<T, TUniform> : Drawable
    where T : unmanaged
    where TUniform : unmanaged
{
    protected Mesh<T> _mesh;
    protected List<Texture> _textureAssets = new();
    protected Dictionary<string, TextureView> _textures = new();
    protected PrimitiveTopology _topology;
    protected ResourceLayout? _transformTextureResourceLayout;
    protected Dictionary<string, DeviceBuffer> _uniformBuffers = new();
    protected ResourceLayout? _uniformResourceLayout;
    protected TUniform drawableUniform;
    protected Sampler? sampler;
    protected ShaderStages transformShaderStages;
    protected ShaderStages uniformShaderStages;

    public Drawable(DrawableOptions<T, TUniform> o)
    {
        _shaders = o.Shader.Shaders;
        _mesh = o.Mesh ?? new Mesh<T>(new T[0], new ushort[0], new VertexLayoutDescription());
        if (o.Transform == null)
            Debug.Error(LogCategory.Rendering,
                "Drawable is missing a transform. Drawable can not be properly sorted!");

        _topology = o.Topology ?? PrimitiveTopology.TriangleStrip;
        transform = o.Transform;
        drawableUniform = o.Uniform;
        uniformShaderStages = o.UniformShaderStages ?? ShaderStages.Vertex | ShaderStages.Fragment;
        transformShaderStages = o.TransformShaderStages ?? ShaderStages.Vertex;


        _textureAssets = o.Textures ?? new List<Texture>();
        sampler = o.Sampler;
        if (o.Mesh != null) // Required for the text drawable
            CreateResources();
    }


    public override void CreateResources()
    {
        var graphicsDevice = Renderer.GraphicsDevice!;
        // Make sure our transform knows us
        if (transform != null)
            transform.RegisterDrawable(this);

        var factory = graphicsDevice.ResourceFactory;
        vertexBuffer =
            factory.CreateBuffer(new BufferDescription((uint)_mesh.Vertices.Length * (uint)Marshal.SizeOf<T>(),
                BufferUsage.VertexBuffer));
        vertexBuffer.Name = "Vertex Buffer";
        indexBuffer = factory.CreateBuffer(new BufferDescription((uint)_mesh.Indices.Length * sizeof(ushort),
            BufferUsage.IndexBuffer));
        vertexBuffer.Name = "Index Buffer";
        transformBuffer = factory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf<TransformStruct>(),
            BufferUsage.UniformBuffer));
        vertexBuffer.Name = "Transform Buffer";

        // Uniform
        _uniformBuffers.Add("Default Uniform",
            factory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf(drawableUniform),
                BufferUsage.UniformBuffer)));

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
        elementDescriptions[0] = new ResourceLayoutElementDescription("TransformMatrices",
            ResourceKind.UniformBuffer, transformShaderStages);
        var i = 1;


        foreach (var texture in _textureAssets)
        {
            elementDescriptions[i] = new ResourceLayoutElementDescription(texture.Name,
                ResourceKind.TextureReadOnly, ShaderStages.Fragment);
            i++;
        }

        elementDescriptions[^1] =
            new ResourceLayoutElementDescription("Sampler", ResourceKind.Sampler, ShaderStages.Fragment);

        _transformTextureResourceLayout = factory.CreateResourceLayout(
            new ResourceLayoutDescription(elementDescriptions));

        // Next up we have to create the layout for our uniform
        var uniformElementDescriptions =
            new ResourceLayoutElementDescription[_uniformBuffers.Count];
        uniformElementDescriptions[0] = new ResourceLayoutElementDescription(
            _uniformBuffers["Default Uniform"].Name, ResourceKind.UniformBuffer, uniformShaderStages);
        _uniformResourceLayout = factory.CreateResourceLayout(
            new ResourceLayoutDescription(uniformElementDescriptions));

        pipelineDescription.DepthStencilState = new DepthStencilStateDescription(
            false,
            false,
            ComparisonKind.LessEqual);

        pipelineDescription.RasterizerState = new RasterizerStateDescription(
            FaceCullMode.None,
            PolygonFillMode.Solid,
            FrontFace.Clockwise,
            false,
            false);

        pipelineDescription.PrimitiveTopology = _topology;
        pipelineDescription.ResourceLayouts = Array.Empty<ResourceLayout>();
        pipelineDescription.ShaderSet = new ShaderSetDescription(
            new[] { vertexLayout },
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

        buffers[^1] = sampler ?? graphicsDevice.LinearSampler;
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

    public void UpdateMesh(Mesh<T> mesh)
    {
        _mesh = mesh;
        UpdateMeshBuffers();
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
        cl.SetPipeline(pipeline);
        cl.SetVertexBuffer(0, vertexBuffer);
        cl.SetIndexBuffer(indexBuffer, IndexFormat.UInt16);
        cl.SetGraphicsResourceSet(0, _transformSet);
        cl.SetGraphicsResourceSet(1, _uniformSet);
        cl.DrawIndexed(
            (uint)_mesh.Indices.Length,
            1,
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
        var cmat = Matrix4x4.Identity;

        if (transform.GetType() != typeof(RectTransform))
            cmat = Camera.GetTransformMatrix();
        if (transformBuffer != null && !transformBuffer.IsDisposed)
            graphicsDevice.UpdateBuffer(transformBuffer, 0, new TransformStruct(matrix, tmat, cmat));
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
        if (pipeline == null)
            return; // Uhhhh? what?
        pipeline.Dispose();

        vertexBuffer.Dispose();
        indexBuffer.Dispose();
        transformBuffer.Dispose();
        _transformSet.Dispose();
        _transformTextureResourceLayout?.Dispose();
        _uniformResourceLayout?.Dispose();
        if (_uniformSet != null)
            _uniformSet.Dispose();
        foreach (var buffer in _uniformBuffers.Values) buffer.Dispose();

        _uniformBuffers.Clear();
        foreach (var texView in _textures.Values) texView.Dispose();

        _textures.Clear();
        if (sampler != Renderer.GraphicsDevice.PointSampler && sampler != Renderer.GraphicsDevice.LinearSampler &&
            sampler != Renderer.GraphicsDevice.Aniso4xSampler)
            sampler?.Dispose();
    }

    ~Drawable()
    {
        // Sanity check
        // We should dispose ourselves if we haven't already, just in case
        if (_transformSet != null && !_transformSet.IsDisposed)
        {
            SoftDispose();
            Debug.Warning("Drawable wasn't properly disposed! You might have a memory leak");
        }
    }
}