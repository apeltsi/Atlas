using SolidCode.Atlas.ECS;
using Veldrid;

namespace SolidCode.Atlas.Rendering;

public class RenderComponent : Component
{
    private Drawable[] _drawables = Array.Empty<Drawable>();

    public virtual Drawable[] StartRender(GraphicsDevice graphicsDevice)
    {
        return new Drawable[0];
    }

    public void OnDisable()
    {
        for (var i = 0; i < _drawables.Length; i++)
        {
            Renderer.RemoveDrawable(_drawables[i]);
            _drawables[i].Dispose();
        }

        _drawables = Array.Empty<Drawable>();
    }

    public void OnEnable()
    {
        try
        {
            _drawables = StartRender(Renderer.GraphicsDevice);
            Renderer.AddDrawables(_drawables);
        }
        catch (Exception e)
        {
            Debug.Error(LogCategory.Rendering, "Error while creating drawable: " + e.Message);
            Debug.Error(LogCategory.Rendering, e.StackTrace ?? "Stack trace not available");
        }
    }
}