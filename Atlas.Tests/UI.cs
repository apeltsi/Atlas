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

        Entity e = new Entity("Text");
        e.RemoveComponent<Transform>();
        RectTransform rt = e.AddComponent<RectTransform>();
        rt.Scale = new RelativeVector(0.5f, 0.5f);
        TextRenderer tr = e.AddComponent<TextRenderer>();
        tr.Text = "This is a test of multiline text rendering.\nThis should be on a new line. Otherwise the text should just wrap automatically when necessary.";
        Util.ManualConfirm(-0.75f);
        Atlas.Start();
    }

}