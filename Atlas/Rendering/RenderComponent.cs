
namespace SolidCode.Atlas.Rendering
{
    using SolidCode.Atlas.ECS;
    using Veldrid;

    public class RenderComponent : Component
    {
        private Drawable[] drawables;
        public virtual Drawable[] StartRender(GraphicsDevice _graphicsDevice)
        {
            return new Drawable[0];
        }

        public void OnDisable()
        {
            for (int i = 0; i < this.drawables.Length; i++)
            {
                Renderer.RemoveDrawable(this.drawables[i]);
                this.drawables[i].Dispose();
            }
            this.drawables = new Drawable[0];
        }

        public void OnEnable()
        {
            try
            {
                this.drawables = StartRender(Renderer.GraphicsDevice);
                Renderer.AddDrawables(new List<Drawable>(this.drawables));
            }
            catch (Exception e)
            {
                Debug.Error(LogCategory.Rendering, "Error while creating drawable: " + e.Message);
                Debug.Error(LogCategory.Rendering, e.StackTrace ?? "Stack trace not available");
            }
        }
    }
}
