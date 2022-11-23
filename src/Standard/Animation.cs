using System.Numerics;
using SolidCode.Atlas.ECS;
using SolidCode.Atlas.Mathematics;
using SolidCode.Atlas.Rendering;
using static SolidCode.Atlas.Animation.Animation;

namespace SolidCode.Atlas.Animation
{
    public class ValueRef<T>
    {
        private Func<T> _get;
        private Action<T> _set;

        public ValueRef(Func<T> @get, Action<T> @set)
        {
            _get = @get;
            _set = @set;
        }

        public T Value
        {
            get { return _get(); }
            set { _set(value); }
        }
    }
    public static class Animation
    {
        static List<ITween> tweens = new List<ITween>();
        static void AnimationManagerSetup()
        {
            if (AnimationManager.instance != null)
            {
                return;
            }
            Entity e = new Entity("ATLAS | Animation Manager");
            e.AddComponent<AnimationManager>();
        }
        public static TweenReference DoTween<T>(ValueRef<T> value, T end, float time, Action? onDone = null)
        {
            if (onDone == null)
            {
                onDone = () => { };
            }
            AnimationManagerSetup();
            ITween t = new Tween<T>(value, end, time, onDone);
            tweens.Add(t);
            return new TweenReference(t);
        }
        public interface ITween
        {
            public bool Tick(float diff);
            public float age { get; }

        }

        public class TweenReference
        {
            private ITween tween;
            public float time
            {
                get
                {
                    return tween.age;
                }
            }
            public bool isPlaying
            {
                get
                {
                    return Animation.tweens.Contains(tween);
                }
            }
            public TweenReference(ITween tween)
            {
                this.tween = tween;
            }

            public void Stop()
            {
                Animation.tweens.Remove(tween);
            }
        }

        class Tween<T> : ITween
        {
            ValueRef<T> value;
            T end;
            T start;
            float duration;
            public float age { get; protected set; }
            Action onDone;

            public Tween(ValueRef<T> value, T end, float duration, Action onDone)
            {
                this.start = value.Value;
                this.value = value;
                this.duration = duration;
                this.end = end;
                this.onDone = onDone;
            }

            public bool Tick(float diff)
            {
                try
                {
                    age += diff;
                    float t = Math.Clamp(age / duration, 0, 1);
                    if (typeof(T) == typeof(float))
                    {
                        // Im really sorry for what im about to do...
                        this.value.Value = (T)(object)AMath.Lerp((float)(object)start, (float)(object)end, t);
                    }
                    else if (typeof(T) == typeof(int))
                    {
                        // Im really sorry for what im about to do...
                        this.value.Value = (T)(object)AMath.Lerp((int)(object)start, (int)(object)end, t);
                    }
                    else if (typeof(T) == typeof(double))
                    {
                        // Im really sorry for what im about to do...
                        this.value.Value = (T)(object)AMath.Lerp((double)(object)start, (double)(object)end, t);
                    }
                    else if (typeof(T) == typeof(Vector2))
                    {
                        // Im really sorry for what im about to do...
                        this.value.Value = (T)(object)AMath.Lerp((Vector2)(object)start, (Vector2)(object)end, t);
                    }
                    else if (typeof(T) == typeof(Vector3))
                    {
                        // Im really sorry for what im about to do...
                        this.value.Value = (T)(object)AMath.Lerp((Vector3)(object)start, (Vector3)(object)end, t);
                    }
                    else if (typeof(T) == typeof(Vector4))
                    {
                        // Im really sorry for what im about to do...
                        this.value.Value = (T)(object)AMath.Lerp((Vector4)(object)start, (Vector4)(object)end, t);
                    }
                    if (age > duration)
                    {
                        this.value.Value = end;
                        onDone.Invoke();
                        return false;
                    }
                    return true;
                }
                catch (Exception e)
                {
                    // If this fails, then we should probably just get rid of the animation
                    return false;
                }
            }
        }
        [SingleInstance]
        public class AnimationManager : Component
        {
            public static AnimationManager? instance;

            public override void Start()
            {
                instance = this;
            }

            public override void Update()
            {
                List<ITween> curTweens = new List<ITween>(Animation.tweens);
                foreach (ITween tween in curTweens)
                {
                    if (!tween.Tick(Window.frameDeltaTime))
                    {
                        tweens.Remove(tween);
                    }
                }
            }
        }

    }
}