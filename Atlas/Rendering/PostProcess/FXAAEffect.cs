using System.Numerics;
using SolidCode.Atlas.AssetManagement;
using SolidCode.Atlas.Mathematics;
using Veldrid;

namespace SolidCode.Atlas.Rendering.PostProcess;

public class FXAAEffect : PostProcessEffect
{
    List<ShaderPass> _passes = new List<ShaderPass>();
    List<Veldrid.Texture> _textures = new List<Veldrid.Texture>();
    List<TextureView> _textureViews = new List<TextureView>();
    List<Framebuffer> _frameBuffers = new List<Framebuffer>();


    struct EmptyUniform
    {
        
    }

    struct FXAAUniform
    {
        public Vector2 TexelSize;
        public float ContrastThreshold;
        public float RelativeThreshold;

        public FXAAUniform()
        {
            TexelSize = new Vector2(1f / Renderer.PostRenderResolution.X, 1f / Renderer.PostRenderResolution.Y);
            ContrastThreshold = 0.0625f;
            RelativeThreshold = 0.125f;
        }
    }

    public FXAAEffect()
    {
    }

    public override TextureView CreateResources(TextureView textureView)
    {
        if (Renderer.GraphicsDevice == null) throw new NullReferenceException("Graphics device is null! FXAA cannot be created.");
        if (!AssetPack.CheckIfLoaded("%ASSEMBLY%/atlas-post"))
        {
            Debug.Log(LogCategory.Rendering, "Necessary assets for FXAA effect loading...");
            new AssetPack("%ASSEMBLY%/atlas-post").Load();
        }

        ResourceFactory factory = Renderer.GraphicsDevice.ResourceFactory;
        _textureViews.Add(textureView);
        
        _textures.Add(factory.CreateTexture(Renderer.PostProcessingDescription));
        
        _textureViews.Add(factory.CreateTextureView(_textures[0]));

        
        _passes.Add(new ShaderPass<EmptyUniform>("post/luma/shader", null));
        FramebufferDescription bufferDescription = new FramebufferDescription(null, _textures[0]);
        _frameBuffers.Add(factory.CreateFramebuffer(ref bufferDescription));
        _passes[0].CreateResources(_frameBuffers[0], new []{_textureViews[0]});
        
        _passes.Add(new ShaderPass<FXAAUniform>("post/fxaa/shader", new FXAAUniform()));
        _textures.Add(factory.CreateTexture(Renderer.PostProcessingDescription));
        _textureViews.Add(factory.CreateTextureView(_textures[1]));
        bufferDescription = new FramebufferDescription(null, _textures[1]);
        _frameBuffers.Add(factory.CreateFramebuffer(ref bufferDescription));
        _passes[1].CreateResources(_frameBuffers[1], new []{_textureViews[1]});
        return _textureViews[2];
    }

    public Veldrid.Texture? Texture
    {
        get{
            if(_textureViews.Count > 0 && !_textureViews[^1].IsDisposed)
                return _textureViews[^1].Target;
            return null;
        }
    } 
    
        
    public override void Draw(CommandList cl)
    {
        foreach (var buffer in _frameBuffers)
        {
            cl.SetFramebuffer(buffer);
            cl.ClearColorTarget(0, RgbaFloat.Clear);
        }
        foreach (var pass in _passes)
        {
            pass.Draw(cl);
        }
    }

    public override void Dispose()
    {
        foreach (var texture in _textures)
        {
            texture.Dispose();
        }
        _textures.Clear();
        foreach (var textureView in _textureViews)
        {
            textureView.Dispose();
        }
        _textureViews.Clear();
        foreach (var frameBuffer in _frameBuffers)
        {
            frameBuffer.Dispose();
        }
        _frameBuffers.Clear();
        
        foreach (var pass in _passes)
        {
            pass.Dispose();
        }
        _passes.Clear();
    }

}