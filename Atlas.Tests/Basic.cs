using SolidCode.Atlas;
using SolidCode.Atlas.Rendering;
using SolidCode.Atlas.ECS;
using SolidCode.Atlas.ECS.SceneManagement;

namespace SolidCode.Atlas.Tests;

public class Basic
{
    [Fact]
    public void Start()
    {
        Atlas.InitializeLogging();
        Atlas.StartCoreFeatures("Atlas Start Test");

        List<Entity> entities = new List<Entity>();
        Entity e = new Entity("Test entity");
        e.AddComponent<EndOnFirstFrame>();
        entities.Add(e);
        Atlas.Start(new Scene(entities));
    }
}