using Veldrid;

namespace SolidCode.Atlas.Rendering.PostProcess;
/// <summary>
/// A simple shader effect that renders the shader with the provided textures & uniform
/// </summary>
/// <typeparam name="TUniform"></typeparam>
public class ShaderEffect<TUniform> : PostProcessEffect where TUniform : unmanaged
{
    private Veldrid.Texture? _tex;
    private TextureView? _textureView;
    private Framebuffer? _framebuffer;
    private ShaderPass<TUniform>? sPass;
    private TUniform _default;
    private string _shader;

    public ShaderEffect(TUniform @default, string shader)
    {
        _default = @default;
        _shader = shader;
    }

    public override void Draw(CommandList cl)
    {
        sPass?.Draw(cl);
    }

    public override TextureView CreateResources(TextureView textureView)
    {
        ResourceFactory factory = Renderer.GraphicsDevice.ResourceFactory;

        _tex = factory.CreateTexture(Renderer.PostProcessingDescription);
        
        _textureView = factory.CreateTextureView(_tex);
        
        
        FramebufferDescription bufferDescription = new FramebufferDescription(null, _tex);
        _framebuffer = factory.CreateFramebuffer(ref bufferDescription);
        sPass = new ShaderPass<TUniform>(_shader, _default);
        sPass.CreateResources(_framebuffer, new []{textureView});
        return _textureView;
    }
    
    public void UpdateUniform(TUniform uniform)
    {
        sPass?.UpdateUniform(uniform);
    }
    
    
    public override void Dispose()
    {
        sPass?.Dispose();
        _textureView?.Dispose();
        _tex?.Dispose();
        _framebuffer?.Dispose();
    }

    
}