using SolidCode.Atlas;
using SolidCode.Atlas.Rendering;
using SolidCode.Atlas.ECS;

namespace SolidCode.Atlas.Tests;

public class Basic
{
    [Fact]
    public void Start()
    {
        Atlas.InitializeLogging(DebuggingMode.Disabled);
        Atlas.StartCoreFeatures("Atlas Start Test");

        Entity e = new Entity("Test entity");
        e.AddComponent<EndOnFirstFrame>();
        Atlas.Start();
    }
    [Fact]
    public void TestConfirm()
    {
        Atlas.InitializeLogging(DebuggingMode.Disabled);
        Atlas.StartCoreFeatures("Atlas Manual Confirm Test");

        Entity e = new Entity("Test entity");
        e.AddComponent<UserConfirm>();
        Util.TextEntity("Space = Pass | Backspace = Fail");
        Window.ClearColor = Veldrid.RgbaFloat.Blue;
        Atlas.Start();
    }

}