using Veldrid;

namespace SolidCode.Atlas.Rendering.PostProcess;

/// <summary>
/// A single step in a post processing effect.
/// </summary>
public abstract class PostProcessPass
{
    public abstract void Draw(CommandList cl);
    public abstract void Dispose();
}