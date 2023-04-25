using System.Numerics;
using System.Reflection;
using SolidCode.Atlas.Animation;
using SolidCode.Atlas.AssetManagement;
using SolidCode.Atlas.Components;
using SolidCode.Atlas.ECS;
using SolidCode.Atlas.Rendering;

namespace SolidCode.Atlas.Extras;

public static class StartupAnimations
{
    public static void LoadExtras()
    {
        if (!AssetPack.CheckIfLoaded("%ASSEMBLY%/atlas-extras"))
        {
            new AssetPack("%ASSEMBLY%/atlas-extras", Assembly.GetExecutingAssembly()).Load();
        }
    }
    
    public static Entity DefaultSplash(Action onDone)
    {
        LoadExtras();
        Entity star = new Entity("Star", new Vector2(1.2f, 1.5f), new Vector2(0.5f));
        SpriteRenderer sr = star.AddComponent<SpriteRenderer>();
        sr.Sprite = AssetManager.GetAsset<Texture>("Atlas-Star");
        Transform t = star.GetComponent<Transform>();
        
        Animation.Animation.DoTween(new ValueRef<Vector2>(() => t.Position, (val) => t.Position = val), Vector2.Zero, 1f, null, TimingFunction.EaseInQuint);
        Animation.Animation.DoTween(new ValueRef<float>(() => t.Rotation, (val) => t.Rotation = val), 360f, 1f, () =>
        {
            sr.Sprite = AssetManager.GetAsset<Texture>("Atlas");
            t.Scale = new Vector2(0.5f);
            Animation.Animation.DoTween(new ValueRef<Vector2>(() => t.Scale, (val) => t.Scale = val), new Vector2(0.75f), 0.4f, null, TimingFunction.EaseOutQuint);
        }, TimingFunction.Linear);
        return star;
    }
}