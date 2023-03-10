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
    public static class TimingFunction
    {
        // https://easings.net/
        public static readonly Func<float, float> Linear = (x) => { return x; };
        public static readonly Func<float, float> EaseInOutSine = (x) => { return (float)-(Math.Cos(Math.PI * x) - 1f) / 2f; };
        public static readonly Func<float, float> EaseInOutCubic = (x) =>
        {
            if (x < 0.5)
            {
                return 4 * x * x * x;
            }
            else
            {
                return (float)(1f - Math.Pow(-2 * x + 2, 3) / 2);
            }
        };
        public static readonly Func<float, float> EaseInOutQuint = (x) =>
        {
            if (x < 0.5)
            {
                return 16 * x * x * x * x * x;
            }
            else
            {
                return (float)(1f - Math.Pow(-2 * x + 2, 5) / 2);
            }
        };

        public static readonly Func<float, float> EaseInSine = (x) =>
        {
            return 1f - (float)Math.Cos((x * Math.PI) / 2);
        };

        public static readonly Func<float, float> EaseInCubic = (x) =>
        {
            return x * x * x;
        };

        public static readonly Func<float, float> EaseInQuint = (x) =>
        {
            return x * x * x * x * x;
        };

        public static readonly Func<float, float> EaseOutSine = (x) =>
        {
            return 1f - (float)Math.Sin((x * Math.PI) / 2);
        };

        public static readonly Func<float, float> EaseOutCubic = (x) =>
        {
            return 1f - (float)Math.Pow(1 - x, 4);
        };

        public static readonly Func<float, float> EaseOutQuint = (x) =>
        {
            return 1f - (float)Math.Pow(1 - x, 5);
        };

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
        public static TweenReference DoTween<T>(ValueRef<T> value, T end, float time, Action? onDone = null, Func<float, float>? timingFunction = null)
        {
            if (onDone == null)
            {
                onDone = () => { };
            }
            if (timingFunction == null)
            {
                timingFunction = TimingFunction.Linear;
            }
            AnimationManagerSetup();
            ITween t = new Tween<T>(value, end, time, onDone, timingFunction);
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
            Func<float, float> timingFunction;

            public Tween(ValueRef<T> value, T end, float duration, Action onDone, Func<float, float> timingFunction)
            {
                this.start = value.Value;
                this.value = value;
                this.duration = duration;
                this.end = end;
                this.onDone = onDone;
                this.timingFunction = timingFunction;
            }

            public bool Tick(float diff)
            {
                try
                {
                    age += diff;
                    float t = timingFunction(Math.Clamp(age / duration, 0, 1));
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
                    if (age > duration)
                    {
                        this.value.Value = end;
                        onDone.Invoke();
                        return false;
                    }
                    return true;
                }
            }
        }
        [SingleInstance]
        public class AnimationManager : Component
        {
            public static AnimationManager? instance;

            public void Start()
            {
                instance = this;
            }

            public void Tick()
            {
                List<ITween> curTweens = new List<ITween>(Animation.tweens);
                foreach (ITween tween in curTweens)
                {
                    try
                    {
                        if (!tween.Tick((float)Time.deltaTime))
                        {
                            tweens.Remove(tween);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.Error(LogCategory.Framework, "Failed to tween: " + e.Message);
                        tweens.Remove(tween);
                    }
                }
            }
        }

    }
}