
namespace SolidCode.Caerus.Rendering
{
    using SolidCode.Caerus.ECS;
    using Veldrid;

    public class RenderComponent : Component
    {
        private Drawable[] drawables;
        public virtual Drawable[] StartRender(GraphicsDevice _graphicsDevice)
        {
            return new Drawable[0];
        }

        public override void OnDisable()
        {
            for (int i = 0; i < this.drawables.Length; i++)
            {
                Window.RemoveDrawable(this.drawables[i]);
            }
            this.drawables = new Drawable[0];
        }

        public override void OnEnable()
        {
            this.drawables = StartRender(Window._graphicsDevice);
            Window.AddDrawables(new List<Drawable>(this.drawables));
        }
    }
}
