using System.Numerics;
using System.Runtime.InteropServices;
using SolidCode.Atlas.AssetManagement;
using Veldrid;

namespace SolidCode.Atlas.Rendering.PostProcess;

/// <summary>
/// A simple effect pass that renders the shader with the provided textures & uniform
/// </summary>
public abstract class ShaderPass : PostProcessPass
{
    public abstract void CreateResources(Framebuffer? targetBuffer, TextureView[] textureViews);
}

/// <summary>
/// A post-process pass that renders the shader with the provided textures.
/// </summary>
public class ShaderPass<TUniform> : ShaderPass
    where TUniform : unmanaged
{
    private readonly Mesh<VertexPositionUV> _mesh;

    private readonly string _shaderName;
    private DeviceBuffer? _indexBuffer;
    private Pipeline _pipeline;
    private ResourceLayout _primaryResourceLayout;
    private ResourceSet _primaryResourceSet;
    private Sampler _sampler;
    private Framebuffer _targetBuffer;
    private TUniform? _uniform;
    private DeviceBuffer? _uniformBuffer;
    private ResourceLayout? _uniformResourceLayout;
    private ResourceSet? _uniformResourceSet;
    private DeviceBuffer? _vertexBuffer;

    public ShaderPass(string shader, TUniform? uniform)
    {
        _shaderName = shader;
        _uniform = uniform;

        #region Mesh Generation

        // Initialize the mesh
        VertexPositionUV[] positions =
        {
            new(new Vector2(-1f, 1f), new Vector4(0, 0, 0, 0)),
            new(new Vector2(1f, 1f), new Vector4(1, 0, 0, 0)),
            new(new Vector2(-1f, -1f), new Vector4(0, 1, 0, 0)),
            new(new Vector2(1f, -1f), new Vector4(1, 1, 0, 0))
        };
        ushort[] quadIndices = { 0, 1, 2, 3 };
        var layout = new VertexLayoutDescription(
            new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate,
                VertexElementFormat.Float4),
            new VertexElementDescription("UV", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4));
        _mesh = new Mesh<VertexPositionUV>(positions, quadIndices, layout);

        #endregion
    }

    // Called by the PostProcessEffect class with the target framebuffer
    public override void CreateResources(Framebuffer? targetBuffer, TextureView[] textureViews)
    {
        if (Renderer.GraphicsDevice == null)
            throw new NullReferenceException(
                "GraphicsDevice is null! PostProcess requires a GraphicsDevice to be initialized.");
        var graphicsDevice = Renderer.GraphicsDevice;

        // Get the shader
        var shader = AssetManager.GetShader(_shaderName);
        // Get the resource factory
        var factory = Renderer.GraphicsDevice.ResourceFactory;

        // Initialize buffers
        _vertexBuffer = factory.CreateBuffer(new BufferDescription(
            (uint)_mesh.Vertices.Length * (uint)Marshal.SizeOf(new VertexPositionUV()), BufferUsage.VertexBuffer));
        _indexBuffer =
            factory.CreateBuffer(new BufferDescription((uint)_mesh.Indices.Length * sizeof(ushort),
                BufferUsage.IndexBuffer));

        // Update the buffers to their initial (and final in this case) values
        graphicsDevice.UpdateBuffer(_vertexBuffer, 0, _mesh.Vertices);
        graphicsDevice.UpdateBuffer(_indexBuffer, 0, _mesh.Indices);


        // Lets generate the pipeline description
        var pipelineDescription = new GraphicsPipelineDescription();
        pipelineDescription.BlendState = BlendStateDescription.SingleOverrideBlend;

        // Generate Element Descriptions for our textures and lets add a single sampler for good measure
        var elementDescriptions = new ResourceLayoutElementDescription[textureViews.Length + 1];
        for (var i = 0; i < textureViews.Length; i++)
            elementDescriptions[i] =
                new ResourceLayoutElementDescription("Texture" + i, ResourceKind.TextureReadOnly,
                    ShaderStages.Fragment);
        elementDescriptions[textureViews.Length] =
            new ResourceLayoutElementDescription("Sampler", ResourceKind.Sampler, ShaderStages.Fragment);

        // Lets generate the uniform resource layout
        _primaryResourceLayout = factory.CreateResourceLayout(
            new ResourceLayoutDescription(elementDescriptions));

        _uniformResourceLayout = null;
        // Lets generate our uniform if present
        if (_uniform != null)
        {
            var u = _uniform.Value;
            _uniformBuffer =
                factory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf(_uniform), BufferUsage.UniformBuffer));
            graphicsDevice.UpdateBuffer(_uniformBuffer, 0, u);

            var uniformElementDescriptions = new ResourceLayoutElementDescription[1];
            uniformElementDescriptions[0] = new ResourceLayoutElementDescription("Uniform", ResourceKind.UniformBuffer,
                ShaderStages.Fragment | ShaderStages.Vertex);
            _uniformResourceLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(uniformElementDescriptions));
        }


        pipelineDescription.DepthStencilState = new DepthStencilStateDescription(
            false, // We don't need depth testing for our 2D purposes
            false, // same here
            ComparisonKind.LessEqual);

        // Rasterizer settings
        pipelineDescription.RasterizerState = new RasterizerStateDescription(
            FaceCullMode.Back,
            PolygonFillMode.Solid,
            FrontFace.Clockwise,
            true,
            false);

        pipelineDescription.PrimitiveTopology = PrimitiveTopology.TriangleStrip;

        pipelineDescription.ShaderSet = new ShaderSetDescription(
            new[] { _mesh.VertexLayout },
            shader.Shaders);
        if (_uniformResourceLayout != null)
            pipelineDescription.ResourceLayouts = new[] { _primaryResourceLayout, _uniformResourceLayout };
        else
            pipelineDescription.ResourceLayouts = new[] { _primaryResourceLayout };


        targetBuffer ??= Renderer.PrimaryFramebuffer;

        pipelineDescription.Outputs = targetBuffer.OutputDescription;

        _targetBuffer = targetBuffer;
        _pipeline = factory.CreateGraphicsPipeline(pipelineDescription);
        var buffers = new BindableResource[textureViews.Length + 1];
        for (var i = 0; i < textureViews.Length; i++) buffers[i] = textureViews[i];

        var sdesc = new SamplerDescription(SamplerAddressMode.Clamp, SamplerAddressMode.Clamp, SamplerAddressMode.Clamp,
            SamplerFilter.MinLinear_MagLinear_MipLinear, null, 4, 0, uint.MaxValue, 0,
            SamplerBorderColor.TransparentBlack);
        _sampler = factory.CreateSampler(sdesc);
        buffers[textureViews.Length] = _sampler;
        _primaryResourceSet = factory.CreateResourceSet(new ResourceSetDescription(
            _primaryResourceLayout,
            buffers));

        if (_uniformResourceLayout != null)
            _uniformResourceSet = factory.CreateResourceSet(new ResourceSetDescription(
                _uniformResourceLayout,
                _uniformBuffer));
        else
            _uniformResourceSet = null;
    }


    public override void Draw(CommandList cl)
    {
        cl.SetFramebuffer(_targetBuffer);
        cl.SetVertexBuffer(0, _vertexBuffer);
        cl.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
        cl.SetPipeline(_pipeline);
        cl.SetGraphicsResourceSet(0, _primaryResourceSet);
        if (_uniformResourceSet != null) cl.SetGraphicsResourceSet(1, _uniformResourceSet);
        cl.DrawIndexed(
            (uint)_mesh.Indices.Length,
            1,
            0,
            0,
            0);
    }

    public void UpdateUniform(TUniform uniform)
    {
        _uniform = uniform;
        Renderer.GraphicsDevice.UpdateBuffer(_uniformBuffer, 0, uniform);
    }

    public override void Dispose()
    {
        _vertexBuffer?.Dispose();
        _indexBuffer?.Dispose();
        _uniformBuffer?.Dispose();
        _pipeline.Dispose();
        _uniformResourceLayout?.Dispose();
        _primaryResourceLayout.Dispose();
        _primaryResourceSet.Dispose();
        _sampler.Dispose();
    }

    private struct VertexPositionUV
    {
        private Vector4 Position;
        private Vector4 UV;

        public VertexPositionUV(Vector2 position, Vector4 uV)
        {
            Position = new Vector4(position.X, position.Y, 0, 0);
            UV = uV;
        }
    }
}