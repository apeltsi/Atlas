using Veldrid;

namespace SolidCode.Atlas.Rendering.PostProcess;

/// <summary>
/// A group of post processing steps that make up an effect.
/// Example for bloom: Separating bright pixels, horizontal blur, vertical blur, combine with main texture
/// </summary>
public abstract class PostProcessEffect
{
    internal uint Layer = 1;
    public abstract void Draw(CommandList cl);
    public abstract void Dispose();
    public abstract TextureView CreateResources(TextureView textureView);
}