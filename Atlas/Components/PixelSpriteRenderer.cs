namespace SolidCode.Atlas.Components;

using System.Numerics;
using AssetManagement;
using ECS;
using Rendering;
using Veldrid;

public class PixelSpriteRenderer : SpriteRenderer
{
    public PixelSpriteRenderer()
    {
        sampler = Renderer.GraphicsDevice.PointSampler;
    }
}