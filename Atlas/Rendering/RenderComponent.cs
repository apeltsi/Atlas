
namespace SolidCode.Atlas.Rendering
{
    using ECS;
    using Veldrid;

    public class RenderComponent : Component
    {
        private Drawable[] _drawables = Array.Empty<Drawable>();
        public virtual Drawable[] StartRender(GraphicsDevice graphicsDevice)
        {
            return new Drawable[0];
        }

        public void OnDisable()
        {
            for (int i = 0; i < this._drawables.Length; i++)
            {
                Renderer.RemoveDrawable(this._drawables[i]);
                this._drawables[i].Dispose();
            }
            this._drawables = Array.Empty<Drawable>();
        }

        public void OnEnable()
        {
            try
            {
                this._drawables = StartRender(Renderer.GraphicsDevice);
                Renderer.AddDrawables(this._drawables);
            }
            catch (Exception e)
            {
                Debug.Error(LogCategory.Rendering, "Error while creating drawable: " + e.Message);
                Debug.Error(LogCategory.Rendering, e.StackTrace ?? "Stack trace not available");
            }
        }
    }
}
