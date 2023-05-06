using System.Collections.Concurrent;
using System.Numerics;
using SolidCode.Atlas.AssetManagement;
using SolidCode.Atlas.Mathematics;
using SolidCode.Atlas.Rendering.PostProcess;
using SolidCode.Atlas.Standard;
using SolidCode.Atlas.Telescope;
using Veldrid;

namespace SolidCode.Atlas.Rendering;

public static class Renderer
{
    public static Framebuffer PrimaryFramebuffer { get; set; }
    private static Veldrid.Texture _mainColorTexture;
    private static TextureView? _mainColorView;
    private static ShaderPass? _resolvePass;
    private static TextureView? _finalTextureView;
    public static GraphicsDevice? GraphicsDevice { get; internal set; }
    internal static CommandList CommandList = null!;
    private static ConcurrentDictionary<int, ManualConcurrentList<Drawable>> _layers = new();
    private static TextureView? _downSampledTextureView;
    private static bool _resourcesDirty = false;
    private static object _resourcesLock = new();
    private static Matrix4x4 _windowScalingMatrix;

    /// <summary>
    /// The scale of the screen in units.
    /// </summary>
    public static Vector2 UnitScale = Vector2.One;

    public static List<PostProcessEffect> PostProcessEffects { get; set; } = new();

    public static TextureDescription MainTextureDescription { get; set; }


    // Post Process
    public static bool DoPostProcess = true;
    private static readonly TextureSampleCount SampleCount = TextureSampleCount.Count1;

    /// <summary>
    /// Describes the scale of one unit. A scaling index of 1 = 1000px. A scaling index of 2 = 2000px etc etc.  
    /// </summary>
    public static float ScalingIndex { get; internal set; } = 1;

    /// <summary>
    /// Describes the scale of one unit in the Post-Processing step. A scaling index of 1 = 1000px. A scaling index of 2 = 2000px etc etc.
    /// </summary>
    public static float PostScalingIndex { get; internal set; } = 1;

    private static float _resolutionScale = 1f;

    /// <summary>
    /// Scales the resolution that non-post processed rendering steps are done at. 0.5 = Half the width and height of the window resolution.
    /// </summary>
    public static float ResolutionScale
    {
        get => _resolutionScale;
        set
        {
            _resolutionScale = value;
            _resourcesDirty = true;
        }
    }

    /// <summary>
    /// The actual resolution that non-post processed rendering steps are done at
    /// To change this please use <see cref="ResolutionScale"/> instead.
    /// </summary>
    public static Vector2 RenderResolution => Window.Size * ResolutionScale;

    public static int PostProcessLayer = 1;

    private static float _postResolutionScale = 1f;

    /// <summary>
    /// Scales the resolution that post-processed rendering steps are done at. 0.5 = Half the width and height of the window resolution.
    /// </summary>
    public static float PostResolutionScale
    {
        get => _postResolutionScale;
        set
        {
            _postResolutionScale = value;
            _resourcesDirty = true;
        }
    }

    /// <summary>
    /// The actual resolution that post-processed rendering steps are done at
    /// To change this please use <see cref="PostResolutionScale"/> instead.
    /// </summary>
    public static Vector2 PostRenderResolution => Window.Size * PostResolutionScale;

    public static TextureDescription PostProcessingDescription => new TextureDescription()
    {
        Width = (uint)AMath.RoundToInt(PostRenderResolution.X), Height = (uint)AMath.RoundToInt(PostRenderResolution.Y),
        Depth = 1, ArrayLayers = 1, MipLevels = 1, SampleCount = TextureSampleCount.Count1,
        Format = PixelFormat.R16_G16_B16_A16_Float, Usage = TextureUsage.RenderTarget | TextureUsage.Sampled,
        Type = TextureType.Texture2D
    };


    internal static void Draw()
    {
        if (GraphicsDevice == null) return;
#if DEBUG
        Profiler.StartTimer(Profiler.TickType.Update);
#endif
        if (_resourcesDirty)
        {
            CreateResources();
        }
        // We should begin by removing any stray drawables


        // The first thing we need to do is call Begin() on our CommandList. Before commands can be recorded into a CommandList, this method must be called.
        ResourceFactory factory = GraphicsDevice.ResourceFactory;
        CommandList.Begin();

        // Before we can issue a Draw command, we need to set a Framebuffer.
        CommandList.SetFramebuffer(PrimaryFramebuffer);

        // At the beginning of every frame, we clear the screen to black. 
        CommandList.ClearColorTarget(0, Window.ClearColor);
        int ppLayer = PostProcessLayer;
        for (int i = 0; i < ppLayer; i++)
        {
            if (_layers.TryGetValue(i, out var layer))
            {
                CommandList.InsertDebugMarker("Begin Layer " + i);
                RenderDrawables(_windowScalingMatrix, layer);
            }
        }

        CommandList.InsertDebugMarker("Begin Post-Process");
        if (DoPostProcess)
        {
            foreach (var effect in PostProcessEffects)
            {
                effect.Draw(CommandList);
            }
        }

        int curLayer = ppLayer;
        if (!_layers.ContainsKey(curLayer))
        {
            curLayer++;
        }

        while (_layers.ContainsKey(curLayer))
        {
            CommandList.InsertDebugMarker("Begin Layer " + curLayer);
            RenderDrawables(_windowScalingMatrix, _layers[curLayer]);
            curLayer++;
        }

        CommandList.InsertDebugMarker("Final Resolve shader");
        // If we're using multi-sampling we need to resolve the texture first
        if (SampleCount != TextureSampleCount.Count1)
        {
            CommandList.ResolveTexture(_finalTextureView.Target, _downSampledTextureView.Target);
        }

        _resolvePass.Draw(CommandList);
        CommandList.End();
        TickScheduler.FreeThreads(); // Everything we need should now be free for use!

        // Now that we have done that, we need to bind the resources that we created in the last section, and issue a draw call.
#if DEBUG
        Profiler.EndTimer(Profiler.TickType.Update, "Pre-Render Tasks");
        Profiler.StartTimer(Profiler.TickType.Update);
#endif

        GraphicsDevice.SubmitCommands(CommandList);
        GraphicsDevice.WaitForIdle();
        try
        {
            GraphicsDevice.SwapBuffers();
        }
        catch(VeldridException e)
        {
            
        }
#if DEBUG
        Profiler.EndTimer(Profiler.TickType.Update, "Render");
        Profiler.SubmitTimes(Profiler.TickType.Update);
#endif
    }

    internal static void RenderDrawables(Matrix4x4 scalingMatrix, ManualConcurrentList<Drawable> drawables)
    {
        drawables.Update();
        foreach (Drawable drawable in drawables)
        {
            if (drawable == null || drawable.transform == null)
            {
                continue;
            }

            drawable.SetGlobalMatrix(GraphicsDevice, scalingMatrix);
            drawable.SetScreenSize(GraphicsDevice, RenderResolution);
            drawable.Draw(CommandList);
        }
    }

    public static void UpdateGetScalingMatrix(Vector2 dimensions)
    {
        float width = dimensions.X * ScalingIndex;
        float height = dimensions.Y * ScalingIndex;
        float max = Math.Max(width, height);
        UnitScale = new Vector2(max / height, max / width);
        ScalingIndex = max / 1000f;
        PostScalingIndex = Math.Max(PostRenderResolution.X, PostRenderResolution.Y) / 1000f;
        _windowScalingMatrix = new Matrix4x4(
            height / max, 0, 0, 0,
            0, width / max, 0, 0,
            0, 0, 1, 0,
            0, 0, 0, 1);
    }

    public static void AddDrawables(List<Drawable> drawables)
    {
        foreach (var d in drawables)
        {
            int layer = (int)d.transform.Layer;
            if (_layers.TryGetValue(layer, out var layer1))
            {
                layer1.AddSorted(d);
            }
            else
            {
                var list = new ManualConcurrentList<Drawable>();
                _layers.TryAdd(layer, list);
                list.AddSorted(d);
            }
        }
    }

    public static void RemoveDrawable(Drawable drawable, uint? itemLayer = null)
    {
        uint layerIndex = 0;
        if (itemLayer == null)
        {
            layerIndex = drawable.transform.Layer;
        }
        else
        {
            layerIndex = itemLayer.Value;
        }
        bool removed = _layers[(int)layerIndex].Remove(drawable);
        if (!removed)
        {
            foreach (var layer in _layers)
            {
                if (layer.Value.Remove(drawable))
                {
                    return;
                }
            }
        }
    }

    public static void ReloadAllShaders()
    {
        if (GraphicsDevice == null) return;
        TickScheduler.RequestTick().Wait();
        Debug.Log(LogCategory.Rendering, "RELOADING ALL SHADERS...");
        // Next, lets dispose all drawables
        foreach (var layer in _layers)
        {
            foreach (var d in layer.Value)
            {
                d.Dispose();
                d.CreateResources(GraphicsDevice);
            }
        }

        CreateResources();
        Debug.Log(LogCategory.Rendering, "All shaders have been reloaded...");
        TickScheduler.FreeThreads();
    }

    internal static void CreateResources()
    {
        lock (_resourcesLock)
        {
            if (GraphicsDevice == null) return;

            ResourceFactory factory = GraphicsDevice.ResourceFactory;
            if (CommandList is { IsDisposed: false })
            {
                CommandList.Dispose();
            }

            CommandList = factory.CreateCommandList();
            if (_mainColorTexture is { IsDisposed: false })
            {
                _mainColorTexture.Dispose();
            }

            if (PrimaryFramebuffer is { IsDisposed: false })
            {
                PrimaryFramebuffer.Dispose();
            }


            if (_mainColorView is { IsDisposed: false })
            {
                _mainColorView.Dispose();
            }

            if (_downSampledTextureView != null && _downSampledTextureView.Target is { IsDisposed: false })
            {
                _downSampledTextureView.Target.Dispose();
            }

            if (_downSampledTextureView is { IsDisposed: false })
            {
                _downSampledTextureView.Dispose();
            }

            _resolvePass?.Dispose();

            MainTextureDescription = TextureDescription.Texture2D(
                (uint)AMath.RoundToInt(RenderResolution.X),
                (uint)AMath.RoundToInt(RenderResolution.Y),
                1,
                1,
                PixelFormat.R16_G16_B16_A16_Float,
                TextureUsage.RenderTarget | TextureUsage.Sampled, SampleCount);

            _mainColorTexture = factory.CreateTexture(MainTextureDescription);
            _mainColorTexture.Name = "Primary Color Texture";
            _mainColorView = factory.CreateTextureView(_mainColorTexture);
            _mainColorView.Name = "Primary Color Texture View";
            FramebufferDescription fbDesc = new FramebufferDescription(null, _mainColorTexture);
            PrimaryFramebuffer = factory.CreateFramebuffer(ref fbDesc);
            PrimaryFramebuffer.Name = "Primary Framebuffer";

            TextureView previousView = _mainColorView;

            if (DoPostProcess)
            {
                foreach (var effect in PostProcessEffects)
                {
                    effect.Dispose();
                    previousView = effect.CreateResources(previousView);
                }
            }

            _finalTextureView = previousView;
            _resolvePass = new ShaderPass<EmptyStruct>("resolve/shader", null);

            if (SampleCount != TextureSampleCount.Count1)
            {
                TextureDescription downSampledTextureDescription = TextureDescription.Texture2D(
                    GraphicsDevice.SwapchainFramebuffer.Width,
                    GraphicsDevice.SwapchainFramebuffer.Height,
                    1,
                    1,
                    GraphicsDevice.SwapchainFramebuffer.ColorTargets[0].Target.Format,
                    TextureUsage.RenderTarget | TextureUsage.Sampled, TextureSampleCount.Count1);

                _downSampledTextureView =
                    factory.CreateTextureView(factory.CreateTexture(downSampledTextureDescription));
                _resolvePass.CreateResources(GraphicsDevice.SwapchainFramebuffer,
                    new[] { _downSampledTextureView });
            }
            else
            {
                _resolvePass.CreateResources(GraphicsDevice.SwapchainFramebuffer, new[] { _finalTextureView });
            }

            if (_resourcesDirty)
            {
                _resourcesDirty = false;
            }
        }
    }

    /// <summary>
    /// Adds a post process effect to the list of effects to be applied to the scene
    /// </summary>
    /// <param name="effect">The effect to be applied</param>
    public static void AddPostProcessEffect(PostProcessEffect effect)
    {
        PostProcessEffects.Add(effect);
        lock (_resourcesLock)
            _resourcesDirty = true;
    }

    /// <summary>
    /// Removes a post process effect from the list of effects to be applied to the scene
    /// </summary>
    /// <param name="effect">The effect to be removed</param>
    public static void RemovePostProcessEffect(PostProcessEffect effect)
    {
        PostProcessEffects.Remove(effect);
        lock (_resourcesLock)
            _resourcesDirty = true;
    }

    struct EmptyStruct
    {
    }

    public static void ResortDrawable(Drawable d, uint? prevLayer = null)
    {
        
        // Lets just grab out the drawable out of our sorted List and add it back
        lock (_layers)
        {
            RemoveDrawable(d, prevLayer);
            AddDrawables(new List<Drawable>() { d });
        }
    }

    public static void RequestResourceCreation()
    {
        lock (_resourcesLock)
            _resourcesDirty = true;
    }


    internal static void Dispose()
    {
        if (GraphicsDevice == null) return;
        lock (GraphicsDevice)
        {
            Debug.Log(LogCategory.Rendering, "Disposing all rendering resources");
            GraphicsDevice.WaitForIdle();
            _mainColorTexture.Dispose();
            _mainColorView?.Dispose();
            CommandList.Dispose();
            _resolvePass?.Dispose();
            PrimaryFramebuffer.Dispose();
            
            foreach (var layer in _layers)
            {
                foreach (Drawable drawable in layer.Value)
                {
                    drawable.Dispose();
                }
            }

            _downSampledTextureView?.Target.Dispose();
            _downSampledTextureView?.Dispose();
            foreach (var effect in PostProcessEffects)
            {
                effect.Dispose();
            }
            // Dispose any remaining shaders or textures owned by the asset manager
            AssetManager.Dispose();
            GraphicsDevice.Dispose();

            Debug.Log(LogCategory.Rendering, "Disposed all rendering resources");
        }
    }
}