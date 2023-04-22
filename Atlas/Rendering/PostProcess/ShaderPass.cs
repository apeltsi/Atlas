using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;

namespace SolidCode.Atlas.Rendering.PostProcess;

/// <summary>
/// A post-process pass that renders the shader with the provided textures.
/// </summary>
public class ShaderPass : PostProcessPass
{
    private Mesh<VertexPositionUV> _mesh;
    private DeviceBuffer? _vertexBuffer;
    private DeviceBuffer? _indexBuffer;
    
    private string _shaderName;
    private Pipeline _pipeline;
    private ResourceSet _uniformSet;
    private Framebuffer _targetBuffer;

    private struct VertexPositionUV
    {
        Vector4 Position;
        Vector4 UV;

        public VertexPositionUV(Vector2 position, Vector4 uV)
        {
            Position = new Vector4(position.X, position.Y, 0, 0);
            UV = uV;
        }

    }
    public ShaderPass(string shader)
    {
        _shaderName = shader;
#region Mesh Generation
        // Initialize the mesh
        VertexPositionUV[] positions = {
            new VertexPositionUV(new Vector2(-1f, 1f), new Vector4(0, 0,0,0)),
            new VertexPositionUV(new Vector2(1f, 1f), new Vector4(1, 0,0,0)),
            new VertexPositionUV(new Vector2(-1f, -1f), new Vector4(0, 1,0,0)),
            new VertexPositionUV(new Vector2(1f, -1f), new Vector4(1, 1,0,0))
        };
        ushort[] quadIndices = { 0, 1, 2, 3 };
        var layout = new VertexLayoutDescription(
            new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4),
            new VertexElementDescription("UV", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4));
        this._mesh = new Mesh<VertexPositionUV>(positions, quadIndices, layout);
#endregion
    }
    // Called by the PostProcessEffect class with the target framebuffer
    internal void CreateResources(Framebuffer? targetBuffer, TextureView[] textureViews)
    {
        if (Window.GraphicsDevice == null)
        {
            throw new NullReferenceException(
                "GraphicsDevice is null! PostProcess requires a GraphicsDevice to be initialized.");
        }
        GraphicsDevice graphicsDevice = Window.GraphicsDevice;
        
        // Get the shader
        Shader shader = ShaderManager.GetShader(_shaderName);
        // Get the resource factory
        ResourceFactory factory = Window.GraphicsDevice.ResourceFactory;

        // Initialize buffers
        _vertexBuffer = factory.CreateBuffer(new BufferDescription((uint)_mesh.Vertices.Length * (uint)Marshal.SizeOf<VertexPositionUV>(), BufferUsage.VertexBuffer));
        _indexBuffer = factory.CreateBuffer(new BufferDescription((uint)_mesh.Indicies.Length * sizeof(ushort), BufferUsage.IndexBuffer));

        // Update the buffers to their initial (and final in this case) values
        graphicsDevice.UpdateBuffer(_vertexBuffer, 0, _mesh.Vertices);
        graphicsDevice.UpdateBuffer(_indexBuffer, 0, _mesh.Indicies);


        // Lets generate the pipeline description
        GraphicsPipelineDescription pipelineDescription = new GraphicsPipelineDescription();
        pipelineDescription.BlendState = BlendStateDescription.SingleOverrideBlend;
        
        // Generate Element Descriptions for our textures and lets add a single sampler for good measure
        ResourceLayoutElementDescription[] elementDescriptions = new ResourceLayoutElementDescription[textureViews.Length + 1];
        for (int i = 0; i < textureViews.Length; i++)
        {
            elementDescriptions[i] = new ResourceLayoutElementDescription("Texture" + i, ResourceKind.TextureReadOnly, ShaderStages.Fragment);
        }
        elementDescriptions[textureViews.Length] = new ResourceLayoutElementDescription("Sampler", ResourceKind.Sampler, ShaderStages.Fragment);
        
        // Lets generate the uniform resource layout
        ResourceLayout uniformResourceLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(elementDescriptions));
            pipelineDescription.DepthStencilState = new DepthStencilStateDescription(
                depthTestEnabled: false, // We don't need depth testing for our 2D purposes
                depthWriteEnabled: false, // same here
                comparisonKind: ComparisonKind.LessEqual);

            // Rasterizer settings
            pipelineDescription.RasterizerState = new RasterizerStateDescription(
                cullMode: FaceCullMode.Back,
                fillMode: PolygonFillMode.Solid,
                frontFace: FrontFace.Clockwise,
                depthClipEnabled: true,
                scissorTestEnabled: false);

            pipelineDescription.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
            pipelineDescription.ResourceLayouts = System.Array.Empty<ResourceLayout>();
            
            pipelineDescription.ShaderSet = new ShaderSetDescription(
                vertexLayouts: new VertexLayoutDescription[] { _mesh.VertexLayout },
                shaders: shader.shaders);
            
            pipelineDescription.ResourceLayouts = new[] { uniformResourceLayout };
            
            
            if (targetBuffer == null)
            {
                pipelineDescription.Outputs = Window.PrimaryFramebuffer.OutputDescription;
                targetBuffer = Window.PrimaryFramebuffer;
            }
            else
            {
                pipelineDescription.Outputs = targetBuffer.OutputDescription;
            }

            _targetBuffer = targetBuffer;
            _pipeline = factory.CreateGraphicsPipeline(pipelineDescription);
            BindableResource[] buffers = new BindableResource[textureViews.Length + 1];
            for (int i = 0; i < textureViews.Length; i++)
            {
                buffers[i] = textureViews[i];
            }
        
            SamplerDescription sdesc = new SamplerDescription(SamplerAddressMode.Clamp, SamplerAddressMode.Clamp, SamplerAddressMode.Clamp, SamplerFilter.MinLinear_MagLinear_MipLinear, null, 4, 0, uint.MaxValue, 0, SamplerBorderColor.TransparentBlack);
            Sampler s = factory.CreateSampler(sdesc);
            buffers[textureViews.Length] = s;
            _uniformSet = factory.CreateResourceSet(new ResourceSetDescription(
                uniformResourceLayout,
                buffers));
    }


    public override void Draw(CommandList cl)
    {
        cl.SetFramebuffer(_targetBuffer);
        cl.SetVertexBuffer(0, _vertexBuffer);
        cl.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
        cl.SetPipeline(_pipeline);
        cl.SetGraphicsResourceSet(0, _uniformSet);
        cl.DrawIndexed(
            indexCount: (uint)_mesh.Indicies.Length,
            instanceCount: 1,
            indexStart: 0,
            vertexOffset: 0,
            instanceStart: 0);

    }

    public override void Dispose()
    {
        _vertexBuffer?.Dispose();
        _indexBuffer?.Dispose();
        _pipeline?.Dispose();
    }
}
