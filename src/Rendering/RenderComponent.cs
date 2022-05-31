
namespace SolidCode.Caerus.Rendering
{
    using SolidCode.Caerus.ECS;
    using Veldrid;

    public class RenderComponent : Component
    {
        public virtual Drawable[] StartRender(GraphicsDevice _graphicsDevice)
        {
            throw new NotImplementedException("No StartRender() method implemented!");
        }

    }
}
