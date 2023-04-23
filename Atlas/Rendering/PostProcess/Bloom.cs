using System.Numerics;
using SolidCode.Atlas.Mathematics;
using Veldrid;

namespace SolidCode.Atlas.Rendering.PostProcess;

public class BloomEffect : PostProcessEffect
{
    List<ShaderPass> Passes = new List<ShaderPass>();
    List<Veldrid.Texture> _textures = new List<Veldrid.Texture>();
    List<TextureView> _textureViews = new List<TextureView>();
    List<Framebuffer> _frameBuffers = new List<Framebuffer>();
    private float _quality = 1f;
    private float _intensity = 0.5f;
    private float _threshold = 0.7f;
    /// <summary>
    /// The quality of the blur, 1 means that the bloom is performed on the full texture, 0.5 means that the texture is half the resolution 
    /// </summary>
    public float Quality
    {
        get => _quality;
        set { 
            _quality = Math.Clamp(value, 0.01f, 1f);
            RequestFullRecreation();
        }
    }

    /// <summary>
    /// How bright should the bloom be
    /// </summary>
    public float Intensity
    {
        get => _intensity;
        set { _intensity = Math.Clamp(value, 0f, 1f); RequestRecreation();}
    }

    /// <summary>
    /// How bright should the pixels be to be considered for bloom
    /// </summary>
    public float Threshold
    {
        get => _threshold;
        set { _threshold = Math.Clamp(value, 0f, 1f); RequestRecreation(); }
    }

    public BloomEffect()
    {
    }

    struct EmptyUniform
    {
        
    }
    
    struct BloomUniform
    {
        public Vector4 Values;

        public BloomUniform(float intensity, float threshold)
        {
            Values = new Vector4(intensity, threshold, 0, 0);
        }
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
        Framebuffer brightBuffer = factory.CreateFramebuffer(ref brightDescription);
        _frameBuffers.Add(brightBuffer);
        ShaderPass<BloomUniform> brightPass = new ShaderPass<BloomUniform>("post/bright/shader", new BloomUniform(Intensity, Threshold));
        brightPass.CreateResources(brightBuffer, new []{textureView});
        Passes.Add(brightPass);

#endregion
#region Kawase Blur

        int blurIterations = AMath.RoundToInt(Math.Sqrt((Window.ScalingIndex * Quality) * 20));
        for (int i = 0; i < blurIterations; i++)
        {
            TextureDescription desc = Window.MainTextureDescription;
            desc.Width = (uint)Math.Clamp(desc.Width * Quality / Math.Pow(2, i), 1, uint.MaxValue);
            desc.Height = (uint)Math.Clamp(desc.Height * Quality / Math.Pow(2, i), 1,uint.MaxValue);

            Veldrid.Texture blurTexture = factory.CreateTexture(desc);
            blurTexture.Name = "Kawase Filter #" + i;
            _textures.Add(blurTexture);
            _textureViews.Add(factory.CreateTextureView(blurTexture));
            FramebufferDescription blurDescription = new FramebufferDescription(null, blurTexture);
            Framebuffer kawaseFramebuffer = factory.CreateFramebuffer(blurDescription);
            kawaseFramebuffer.Name = "Kawase Framebuffer #" + i;
            _frameBuffers.Add(kawaseFramebuffer);
            ShaderPass kawasePass = new ShaderPass<EmptyUniform>("post/kawase/shader", null);
            kawasePass.CreateResources(kawaseFramebuffer, new []{_textureViews[^2]} );
            Passes.Add(kawasePass);
        }
        
#endregion
        
#region Combine Pass
        for (int i = blurIterations - 2; i >= 0; i--)
        {
            ShaderPass kawasePass = new ShaderPass<EmptyUniform>("post/combine/shader", null);
            TextureDescription desc = Window.MainTextureDescription;
            desc.Width = (uint)Math.Clamp(desc.Width * Quality / Math.Pow(2, i), 1, uint.MaxValue);
            desc.Height = (uint)Math.Clamp(desc.Height * Quality / Math.Pow(2, i), 1,uint.MaxValue);
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
        ShaderPass combinePass = new ShaderPass<EmptyUniform>("post/finalcombine/shader", null);
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
        
    /// <summary>
    /// Completely recreates the effect, this calls Window.CreateResources() as the textureView returned by this effect has changed, so the window has to adapt
    /// </summary>
    void RequestFullRecreation()
    {
        lock (Passes)
        {
            if(Passes.Count > 0)
                Window.RequestResourceCreation();
        }
    }

    void RequestRecreation()
    {
        lock (Passes)
        {
            if(Passes.Count > 0)
                ((ShaderPass<BloomUniform>)Passes[0]).UpdateUniform(new BloomUniform(Intensity, Threshold));
        }
    }
}