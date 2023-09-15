using System.Numerics;
using SolidCode.Atlas.Components;
using SolidCode.Atlas.ECS;
namespace SolidCode.Atlas.Tests;

public static class Util
{
    public static Entity TextEntity(string text, Vector2? position = null, Vector2? scale = null)
    {
        Entity e = new Entity(text, position, scale ?? new Vector2(0.5f, 0.5f));
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
        Util.TextEntity("Space = Pass | Backspace = Fail", new Vector2(0f, y), new Vector2(3f, 1f));
        return e;
    }
}