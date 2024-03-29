﻿using SolidCode.Atlas.AssetManagement;
using SolidCode.Atlas.Mathematics;
using Veldrid;

namespace SolidCode.Atlas.Rendering.PostProcess;

/// <summary>
/// A simple & fast blur effect. Supports bypass
/// </summary>
public class BlurEffect : PostProcessEffect
{
    private readonly bool _bypass;
    private readonly List<Framebuffer> _frameBuffers = new();
    private readonly List<ShaderPass> _passes = new();
    private readonly List<Veldrid.Texture> _textures = new();
    private readonly List<TextureView> _textureViews = new();
    private float _intensity = 0.5f;
    private float _quality = 1f;

    public BlurEffect(float intensity = 1f, bool bypass = false)
    {
        _intensity = intensity;
        _bypass = bypass;
    }

    /// <summary>
    /// The quality of the blur, 1 means that the blur is performed on the full texture, 0.5 means that the texture is half
    /// the resolution
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
    /// How big should the blur be
    /// </summary>
    public float Intensity
    {
        get => _intensity;
        set
        {
            _intensity = Math.Clamp(value, 0f, 1f);
            RequestFullRecreation();
        }
    }

    public Veldrid.Texture? Texture
    {
        get
        {
            if (_textureViews.Count > 0 && !_textureViews[^1].IsDisposed)
                return _textureViews[^1].Target;
            return null;
        }
    }

    public override TextureView CreateResources(TextureView textureView)
    {
        if (Renderer.GraphicsDevice == null)
            throw new NullReferenceException("Graphics device is null! Blur cannot be created.");
        if (!AssetPack.CheckIfLoaded("%ASSEMBLY%/atlas-post"))
        {
            Debug.Log(LogCategory.Rendering, "Necessary assets for Blur effect loading...");
            new AssetPack("%ASSEMBLY%/atlas-post").Load();
        }

        var factory = Renderer.GraphicsDevice.ResourceFactory;
        _textureViews.Add(textureView);

        #region Kawase Blur

        var blurIterations = AMath.RoundToInt(Math.Sqrt(Renderer.PostScalingIndex * Quality * 20) * Intensity);
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
            ShaderPass kawasePass = new ShaderPass<EmptyUniform>("post/weighted-combine/shader", null);
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

        if (_bypass)
            return textureView;
        return _textureViews[^1];
    }


    public override void Draw(CommandList cl)
    {
        foreach (var buffer in _frameBuffers)
        {
            cl.SetFramebuffer(buffer);
            cl.ClearColorTarget(0, RgbaFloat.Clear);
        }

        foreach (var pass in _passes) pass.Draw(cl);
        if (_bypass)
            cl.SetFramebuffer(Renderer.PrimaryFramebuffer);
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


    private struct EmptyUniform
    {
    }
}