using System.Numerics;
using SolidCode.Atlas.Components;
using SolidCode.Atlas.ECS;
namespace SolidCode.Atlas.Tests;

public static class Util
{
    public static Entity TextEntity(string text, Vector2? position = null)
    {
        Entity e = new Entity(text, position, new Vector2(0.3f, 0.3f));
        TextRenderer tr = e.AddComponent<TextRenderer>();
        tr.HorizontalAlignment = TextAlignment.Center;
        tr.Text = text;
        tr.Color = new Vector4(1, 1, 1, 1);
        return e;
    }

    public static Entity ManualConfirm(float y = 0f)
    {
        Entity e = new Entity("Test entity");
        e.AddComponent<UserConfirm>();
        Util.TextEntity("Space = Pass | Backspace = Fail", new Vector2(0f, y));
        return e;
    }
}