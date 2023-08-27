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
    public static CommandList CommandList = null!;
    private static ConcurrentDictionary<int, ManualConcurrentList<Drawable>> _layers = new();
    private static TextureView? _downSampledTextureView;
    private static bool _resourcesDirty = false;
    private static object _resourcesLock = new();
    private static Matrix4x4 _windowScalingMatrix;

    /// <summary>
    /// The scale of the screen in units.
    /// (Ignores camera scaling)
    /// </summary>
    public static Vector2 UnitScale = Vector2.One;
    /// <summary>
    /// The scale of a single pixel
    /// (Ignores camera scaling)
    /// </summary>
    public static Vector2 PixelScale => UnitScale / Window.Size;

    public static List<PostProcessEffect> PostProcessEffects { get; set; } = new();

    public static TextureDescription MainTextureDescription { get; set; }


    // Post Process
    public static bool DoPostProcess = true;

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
        CommandList.Begin();

        // Before we can issue a Draw command, we need to set a Framebuffer.
        CommandList.SetFramebuffer(PrimaryFramebuffer);

        // At the beginning of every frame, we clear the screen to black. 
        CommandList.ClearColorTarget(0, Window.ClearColor);
        int currentLayer = 0;
        int ppEffectIndex = 0;
        while (true)
        {
            bool skip = true;
            if (_layers.TryGetValue(currentLayer, out var layer))
            {
                CommandList.InsertDebugMarker("Begin Layer " + currentLayer);
                RenderDrawables(_windowScalingMatrix, layer);
                skip = false;
            }

            if (DoPostProcess)
            {
                // lets draw every effect on this layer in order
                CommandList.InsertDebugMarker("Post-Process (Layer " + currentLayer +  ")");
                while (PostProcessEffects.Count > ppEffectIndex &&
                       PostProcessEffects[ppEffectIndex].Layer == currentLayer)
                {
                    PostProcessEffects[ppEffectIndex].Draw(CommandList);
                    ppEffectIndex++;
                    skip = false;
                }

            }

            currentLayer++;
            if (skip)
                break;
        }

#if DEBUG
        CommandList.InsertDebugMarker("Drawing Debug Markers");
        Debug.RenderMarkers(CommandList, _windowScalingMatrix);
#endif
        
        CommandList.InsertDebugMarker("Final Resolve shader");

        _resolvePass.Draw(CommandList);
        CommandList.End();

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
        TickScheduler.FreeThreads(); // Everything we need should now be free for use!
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
        float width = dimensions.X;
        float height = dimensions.Y;
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

    public static void AddDrawables(Drawable[] drawables)
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
            Debug.Warning(LogCategory.Framework, "Couldn't remove Drawable! ");
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
                d.CreateResources();
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
                TextureUsage.RenderTarget | TextureUsage.Sampled, TextureSampleCount.Count1);

            _mainColorTexture = factory.CreateTexture(MainTextureDescription);
            _mainColorTexture.Name = "Primary Color Texture";
            _mainColorView = factory.CreateTextureView(_mainColorTexture);
            _mainColorView.Name = "Primary Color Texture View";
            FramebufferDescription fbDesc = new FramebufferDescription(null, _mainColorTexture);
            PrimaryFramebuffer = factory.CreateFramebuffer(ref fbDesc);
            PrimaryFramebuffer.Name = "Primary Framebuffer";

            TextureView previousView = _mainColorView;
            
            if (DoPostProcess && PostProcessEffects.Count > 0)
            {
                foreach (var effect in PostProcessEffects)
                {
                    effect.Dispose();
                    previousView = effect.CreateResources(previousView);
                }
            }

            _finalTextureView = previousView;
            _finalTextureView.Name += " - Final Texture View";
            _resolvePass = new ShaderPass<EmptyStruct>("resolve/shader", null);

            _resolvePass.CreateResources(GraphicsDevice.SwapchainFramebuffer, new[] { _finalTextureView });
            

            if (_resourcesDirty)
            {
                _resourcesDirty = false;
            }
        }
    }

    /// <summary>
    /// Adds a post process effect to the list of effects to be applied to the scene. Note that calling order matters, as the effects are applied in the order they are added.
    /// DO NOT add an effect with a lower layer than a previous effect!
    /// </summary>
    /// <param name="effect">The effect to be applied</param>
    /// <param name="layer">The layer of the effect, should be larger or equal to the layer of a previous effect</param>

    public static void AddPostProcessEffect(PostProcessEffect effect, uint layer = 1)
    {
        effect.Layer = layer;
        foreach (var e in PostProcessEffects)
        {
            if (e.Layer > effect.Layer)
            {
                Debug.Error(LogCategory.Rendering,
                    $"Can't add Post Process Effect {effect.GetType().Name}. It has a lower layer than {e.GetType().Name}!");
                return;
            }
        }
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
            AddDrawables(new [] { d });
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
            FontSet.DisposeAll();

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