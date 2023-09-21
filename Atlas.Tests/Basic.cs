using SolidCode.Atlas.ECS;
using SolidCode.Atlas.Extras;
using SolidCode.Atlas.Rendering;
using Veldrid;

namespace SolidCode.Atlas.Tests;

public class Basic
{
    [Fact]
    public void Start()
    {
        Atlas.DisableMultiProcessDebugging();
        Atlas.StartCoreFeatures("Atlas Start Test");

        var e = new Entity("Test entity");
        e.AddComponent<EndOnFirstFrame>();
        Atlas.Start();
    }

    [Fact]
    public void TestConfirm()
    {
        Atlas.DisableMultiProcessDebugging();
        Atlas.StartCoreFeatures("Basic Test | Manual Confirm Required");
        StartupAnimations.DefaultSplash(() =>
        {
            Util.ManualConfirm();
            Window.ClearColor = RgbaFloat.Blue;
        });
        Atlas.Start();
        if (UserConfirm.Failed) Assert.Fail("User marked test as failed.");
    }
}