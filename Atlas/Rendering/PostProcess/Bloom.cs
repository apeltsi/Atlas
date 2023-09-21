using System.Numerics;
using SolidCode.Atlas.AssetManagement;
using SolidCode.Atlas.Mathematics;
using Veldrid;

namespace SolidCode.Atlas.Rendering.PostProcess;

/// <summary>
/// A customizable bloom effect
/// </summary>
public class BloomEffect : PostProcessEffect
{
    private readonly List<Framebuffer> _frameBuffers = new();
    private readonly List<ShaderPass> _passes = new();
    private readonly List<Veldrid.Texture> _textures = new();
    private readonly List<TextureView> _textureViews = new();
    private float _intensity = 0.5f;
    private float _quality = 1f;
    private float _threshold = 0.7f;

    /// <summary>
    /// The quality of the blur, 1 means that the bloom is performed on the full texture, 0.5 means that the texture is
    /// half the resolution
    /// </summary>
    public float Quality
    {
        get => _quality;
        set
        {
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
        set
        {
            _intensity = Math.Clamp(value, 0f, 1f);
            RequestRecreation();
        }
    }

    /// <summary>
    /// How bright should the pixels be to be considered for bloom
    /// </summary>
    public float Threshold
    {
        get => _threshold;
        set
        {
            _threshold = Math.Clamp(value, 0f, 1f);
            RequestRecreation();
        }
    }

    public override TextureView CreateResources(TextureView textureView)
    {
        if (Renderer.GraphicsDevice == null)
            throw new NullReferenceException("Graphics device is null! Bloom cannot be created.");
        if (!AssetPack.CheckIfLoaded("%ASSEMBLY%/atlas-post"))
        {
            Debug.Log(LogCategory.Rendering, "Necessary assets for bloom effect loading...");
            new AssetPack("%ASSEMBLY%/atlas-post").Load();
        }

        #region Bright Pixels

        var factory = Renderer.GraphicsDevice.ResourceFactory;

        var brightTexture = factory.CreateTexture(Renderer.PostProcessingDescription);
        brightTexture.Name = "Brightness Texture";
        _textures.Add(brightTexture);
        var brightView = factory.CreateTextureView(brightTexture);
        brightView.Name = "Brightness Texture View";
        _textureViews.Add(brightView);
        var brightDescription = new FramebufferDescription(null, brightTexture);
        var brightBuffer = factory.CreateFramebuffer(ref brightDescription);
        _frameBuffers.Add(brightBuffer);
        var brightPass = new ShaderPass<BloomUniform>("post/bright/shader", new BloomUniform(Intensity, Threshold));
        brightPass.CreateResources(brightBuffer, new[] { textureView });
        _passes.Add(brightPass);

        #endregion

        #region Kawase Blur

        var blurIterations = AMath.RoundToInt(Math.Sqrt(Renderer.PostScalingIndex * Quality * 20));
        for (var i = 0; i < blurIterations; i++)
        {
            var desc = Renderer.PostProcessingDescription;
            desc.Width = (uint)Math.Clamp(desc.Width * Quality / Math.Pow(2, i), 1, uint.MaxValue);
            desc.Height = (uint)Math.Clamp(desc.Height * Quality / Math.Pow(2, i), 1, uint.MaxValue);

            var blurTexture = factory.CreateTexture(desc);
            blurTexture.Name = "Kawase Filter #" + i;
            _textures.Add(blurTexture);
            _textureViews.Add(factory.CreateTextureView(blurTexture));
            var blurDescription = new FramebufferDescription(null, blurTexture);
            var kawaseFramebuffer = factory.CreateFramebuffer(blurDescription);
            kawaseFramebuffer.Name = "Kawase Framebuffer #" + i;
            _frameBuffers.Add(kawaseFramebuffer);
            ShaderPass kawasePass = new ShaderPass<EmptyUniform>("post/kawase/shader", null);
            kawasePass.CreateResources(kawaseFramebuffer, new[] { _textureViews[^2] });
            _passes.Add(kawasePass);
        }

        #endregion

        #region Combine Pass

        for (var i = blurIterations - 2; i >= 0; i--)
        {
            ShaderPass kawasePass = new ShaderPass<EmptyUniform>("post/combine/shader", null);
            var desc = Renderer.PostProcessingDescription;
            desc.Width = (uint)Math.Clamp(desc.Width * Quality / Math.Pow(2, i), 1, uint.MaxValue);
            desc.Height = (uint)Math.Clamp(desc.Height * Quality / Math.Pow(2, i), 1, uint.MaxValue);
            var upscaleTexture = factory.CreateTexture(desc);
            upscaleTexture.Name = "Upscale Filter Texture";
            _textures.Add(upscaleTexture);
            _textureViews.Add(factory.CreateTextureView(upscaleTexture));
            var fbDesc = new FramebufferDescription(null, upscaleTexture);
            _frameBuffers.Add(factory.CreateFramebuffer(fbDesc));
            kawasePass.CreateResources(_frameBuffers[^1], new[] { _textureViews[i + 1], _textureViews[^2] });
            _passes.Add(kawasePass);
        }

        #endregion

        ShaderPass combinePass = new ShaderPass<EmptyUniform>("post/finalcombine/shader", null);
        combinePass.CreateResources(_frameBuffers[0], new[] { textureView, _textureViews[^1] });
        _passes.Add(combinePass);
        return _textureViews[0];
    }

    public override void Draw(CommandList cl)
    {
        foreach (var buffer in _frameBuffers)
        {
            cl.SetFramebuffer(buffer);
            cl.ClearColorTarget(0, RgbaFloat.Clear);
        }

        foreach (var pass in _passes) pass.Draw(cl);
    }

    public override void Dispose()
    {
        foreach (var texture in _textures) texture.Dispose();
        _textures.Clear();
        foreach (var textureView in _textureViews) textureView.Dispose();
        _textureViews.Clear();
        foreach (var frameBuffer in _frameBuffers) frameBuffer.Dispose();
        _frameBuffers.Clear();

        foreach (var pass in _passes) pass.Dispose();
        _passes.Clear();
    }

    /// <summary>
    /// Completely recreates the effect, this calls Window.CreateResources() as the textureView returned by this effect has
    /// changed, so the window has to adapt
    /// </summary>
    private void RequestFullRecreation()
    {
        lock (_passes)
        {
            if (_passes.Count > 0)
                Renderer.RequestResourceCreation();
        }
    }

    private void RequestRecreation()
    {
        lock (_passes)
        {
            if (_passes.Count > 0)
                ((ShaderPass<BloomUniform>)_passes[0]).UpdateUniform(new BloomUniform(Intensity, Threshold));
        }
    }

    private struct EmptyUniform
    {
    }

    private struct BloomUniform
    {
        public Vector4 Values;

        public BloomUniform(float intensity, float threshold)
        {
            Values = new Vector4(intensity, threshold, 0, 0);
        }
    }
}