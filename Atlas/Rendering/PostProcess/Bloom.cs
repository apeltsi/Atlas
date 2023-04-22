using Veldrid;

namespace SolidCode.Atlas.Rendering.PostProcess;

public class BloomEffect : PostProcessEffect
{
    List<ShaderPass> Passes = new List<ShaderPass>();
    List<Veldrid.Texture> _textures = new List<Veldrid.Texture>();
    List<TextureView> _textureViews = new List<TextureView>();
    List<Framebuffer> _frameBuffers = new List<Framebuffer>();

    public BloomEffect()
    {
    }

    public override TextureView CreateResources(TextureView textureView)
    {
        
#region Bright Pixels
        ResourceFactory factory = Window.GraphicsDevice.ResourceFactory;
        Veldrid.Texture brightTexture = factory.CreateTexture(Window.MainTextureDescription);
        brightTexture.Name = "Brightness Texture";
        _textures.Add(brightTexture);
        TextureView brightView = factory.CreateTextureView(brightTexture);
        brightView.Name = "Brightness Texture View";
        _textureViews.Add(brightView);
        FramebufferDescription brightDescription = new FramebufferDescription(null, brightTexture);
        Framebuffer brightBuffer = factory.CreateFramebuffer(brightDescription);
        _frameBuffers.Add(brightBuffer);
        ShaderPass brightPass = new ShaderPass("post/bright/shader");
        brightPass.CreateResources(brightBuffer, new []{textureView});
        Passes.Add(brightPass);

#endregion
#region Kawase Blur

        int blurIterations = 8;
        for (int i = 0; i < blurIterations; i++)
        {
            TextureDescription desc = Window.MainTextureDescription;
            desc.Width /= (uint)Math.Pow(2, i);
            desc.Height /= (uint)Math.Pow(2, i);

            Veldrid.Texture blurTexture = factory.CreateTexture(desc);
            blurTexture.Name = "Kawase Filter #" + i;
            _textures.Add(blurTexture);
            _textureViews.Add(factory.CreateTextureView(blurTexture));
            FramebufferDescription blurDescription = new FramebufferDescription(null, blurTexture);
            Framebuffer kawaseFramebuffer = factory.CreateFramebuffer(blurDescription);
            _frameBuffers.Add(kawaseFramebuffer);
            ShaderPass kawasePass = new ShaderPass("post/kawase/shader");
            kawasePass.CreateResources(kawaseFramebuffer, new []{_textureViews[^2]} );
            Passes.Add(kawasePass);
        }
        
#endregion
        
#region Combine Pass
        for (int i = blurIterations - 2; i >= 0; i--)
        {
            ShaderPass kawasePass = new ShaderPass("post/combine/shader");
            TextureDescription desc = Window.MainTextureDescription;
            desc.Width /= (uint)Math.Pow(2, i);
            desc.Height /= (uint)Math.Pow(2, i);
            Veldrid.Texture upscaleTexture = factory.CreateTexture(desc);
            upscaleTexture.Name = "Upscale Filter Texture";
            _textures.Add(upscaleTexture);
            _textureViews.Add(factory.CreateTextureView(upscaleTexture));
            FramebufferDescription fbDesc = new FramebufferDescription(null, upscaleTexture);
            _frameBuffers.Add(factory.CreateFramebuffer(fbDesc));
            kawasePass.CreateResources(_frameBuffers[^1], new []{_textureViews[i + 1], _textureViews[^2]} );
            Passes.Add(kawasePass);
        }
        
        
#endregion 
        ShaderPass combinePass = new ShaderPass("post/finalcombine/shader");
        combinePass.CreateResources(_frameBuffers[0], new []{textureView, _textureViews[^1]});
        Passes.Add(combinePass);
        return _textureViews[0];
    }
        
    public override void Draw(CommandList cl)
    {
        foreach (var buffer in _frameBuffers)
        {
            cl.SetFramebuffer(buffer);
            cl.ClearColorTarget(0, RgbaFloat.Clear);
        }
        foreach (var pass in Passes)
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
        
        foreach (var pass in Passes)
        {
            pass.Dispose();
        }
        Passes.Clear();
    }
}