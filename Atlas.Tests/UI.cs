using SolidCode.Atlas.Components;
using SolidCode.Atlas.ECS;
using SolidCode.Atlas.UI;

namespace SolidCode.Atlas.Tests;

public class UI
{
    [Fact]
    public void Start()
    {
        Atlas.DisableMultiProcessDebugging();
        Atlas.StartCoreFeatures("UI Test | Manual Confirm Required");

        var e = new Entity("Text");
        e.RemoveComponent<Transform>();
        var rt = e.AddComponent<RectTransform>();
        rt.Scale = new RelativeVector(0.5f, 0.5f);
        var tr = e.AddComponent<TextRenderer>();
        tr.HorizontalAlignment = TextAlignment.Left;
        tr.VerticalAlignment = TextVerticalAlignment.Top;
        tr.Text =
            "This is a test of multiline text rendering.\nThis should be on a new line. Otherwise the text should just wrap automatically when necessary.";
        Util.ManualConfirm(-0.75f);
        Atlas.Start();
        if (UserConfirm.Failed) Assert.Fail("User marked test as failed.");
    }
}