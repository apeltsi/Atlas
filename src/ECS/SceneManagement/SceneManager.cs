
namespace SolidCode.Atlas.ECS.SceneManagement
{

    public static class SceneManager
    {
        public static void LoadScene(Scene scene)
        {
            foreach (Entity e in scene.entities)
            {
                EntityComponentSystem.AddEntity(e);
            }
        }
    }
}
