using SolidCode.Atlas;
using SolidCode.Atlas.Rendering;
using SolidCode.Atlas.ECS;
using SolidCode.Atlas.Extras;

namespace SolidCode.Atlas.Tests;

public class Basic
{
    [Fact]
    public void Start()
    {
        Atlas.DisableMultiProcessDebugging();
        Atlas.StartCoreFeatures("Atlas Start Test");

        Entity e = new Entity("Test entity");
        e.AddComponent<EndOnFirstFrame>();
        Atlas.Start();
    }
    [Fact]
    public void TestConfirm()
    {
        Atlas.DisableMultiProcessDebugging();
        Atlas.StartCoreFeatures("Atlas Manual Confirm Test");
        Extras.StartupAnimations.DefaultSplash(() =>
        {
            Entity e = new Entity("Test entity");
            e.AddComponent<UserConfirm>();
            Util.TextEntity("Space = Pass | Backspace = Fail");
            Window.ClearColor = Veldrid.RgbaFloat.Blue;
        });
        Atlas.Start();
    }

}