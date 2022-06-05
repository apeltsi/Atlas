
namespace SolidCode.Caerus.ECS.SceneManagement
{

    public class Scene
    {
        public List<Entity> entities;
        public Scene(List<Entity> entities)
        {
            this.entities = entities;
        }
    }
}
