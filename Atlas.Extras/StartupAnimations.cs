using System.Numerics;
using System.Reflection;
using SolidCode.Atlas.Animation;
using SolidCode.Atlas.AssetManagement;
using SolidCode.Atlas.Audio;
using SolidCode.Atlas.Components;
using SolidCode.Atlas.ECS;
using SolidCode.Atlas.Rendering;
using SolidCode.Atlas.Standard;

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
        Entity star = new Entity("Star", new Vector2(2f, 2.5f), new Vector2(0.5f));
        SpriteRenderer sr = star.AddComponent<SpriteRenderer>();
        sr.Sprite = AssetManager.GetAsset<Texture>("Atlas-Star");
        Transform t = star.GetComponent<Transform>();
        star.AddComponent<ExecOnStart>().OnStart = () =>
        {
            Audio.Audio.Play(AssetManager.GetAsset<AudioTrack>("Atlas-Impact"));
            Animation.Animation.DoTween(new ValueRef<Vector2>(() => t.Position, (val) => t.Position = val), Vector2.Zero, 0.9f, null, TimingFunction.EaseInQuint);
            Animation.Animation.DoTween(new ValueRef<float>(() => t.Rotation, (val) => t.Rotation = val), 360f, 0.9f, () =>
            {
                ParticleSystem ps = star.AddComponent<ParticleSystem>();
                ps.Sprite = AssetManager.GetAsset<Texture>("Atlas-Triangle");
                ps.Burst = true;
                ps.InitialPosition = () => ARandom.Vector2() * 0.75f - new Vector2(0f, 0.5f);
                ps.InitialScale = () => new Vector2(0.1f);
                ps.InitialRotation = () => ARandom.Value() * 3.14f;
                ps.InitialVelocity = () => new Vector2(-(ARandom.Value() + 0.75f) * 3f, ARandom.Value() * -2f);
                ps.InitialColor = () => new Vector4(1f, 1f, 1f, ARandom.Value());
                sr.Sprite = AssetManager.GetAsset<Texture>("Atlas");
                ps.ParticleUpdates[1] = (ref ParticleSystem.Particle particle) =>
                {
                    particle.Rotation += (float)Time.deltaTime;
                    particle.Color = new Vector4(particle.Color.X, particle.Color.Y, particle.Color.Z,
                        1f - particle.Age / particle.Lifetime);
                    particle.Scale -= new Vector2(0.5f, 0.5f) * (float)Time.deltaTime;
                    if (particle.Scale.X < 0)
                    {
                        particle.Scale = Vector2.Zero;
                    }
                };
                t.Scale = new Vector2(0.5f);
                Animation.Animation.DoTween(new ValueRef<Vector2>(() => t.Scale, (val) => t.Scale = val), new Vector2(0.75f), 0.6f, (
                    () =>
                    {
                        EntityComponentSystem.ScheduleFrameTaskAfter(3f, () =>
                        {
                            
                            Animation.Animation.DoTween(new ValueRef<Vector4>(() => sr.Color, (val) => sr.Color = val), Vector4.Zero, 0.4f, 
                                () =>
                                {
                                    onDone.Invoke();
                                    star.Destroy();
                                }, TimingFunction.Linear);
                        });
                    }), TimingFunction.EaseOutQuint);
            }, TimingFunction.Linear);

        };
        
        return star;
    }

    private class ExecOnStart : Component
    {
        public Action OnStart;

        public void Start()
        {
            OnStart.Invoke();
        }
    }
}