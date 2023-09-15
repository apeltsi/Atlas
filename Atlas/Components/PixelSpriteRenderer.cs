namespace SolidCode.Atlas.Components;

using Rendering;

/// <summary>
/// A sprite renderer that uses point sampling instead of linear sampling
/// </summary>
public class PixelSpriteRenderer : SpriteRenderer
{
    /// <summary>
    /// Creates a new pixel sprite renderer
    /// </summary>
    public PixelSpriteRenderer()
    {
        sampler = Renderer.GraphicsDevice.PointSampler;
    }
}