namespace SolidCode.Atlas.Components;

using Rendering;

public class PixelSpriteRenderer : SpriteRenderer
{
    public PixelSpriteRenderer()
    {
        sampler = Renderer.GraphicsDevice.PointSampler;
    }
}