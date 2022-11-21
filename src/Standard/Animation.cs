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
        public static void DoTween<T>(ValueRef<T> value, T end, float time)
        {
            AnimationManagerSetup();
            tweens.Add(new Tween<T>(value, end, time));
        }
        public interface ITween
        {
            public bool Tick(float diff);
        }
        public class Tween<T> : ITween
        {
            ValueRef<T> value;
            T end;
            T start;
            float time;
            float age;

            public Tween(ValueRef<T> value, T end, float time)
            {
                this.start = value.Value;
                this.value = value;
                this.time = time;
                this.end = end;
            }

            public bool Tick(float diff)
            {
                age += diff;
                float t = Math.Clamp(age / time, 0, 1);
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
                if (age > time)
                {
                    return false;
                }
                return true;
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
                List<ITween> tweensToRemove = new List<ITween>();
                foreach (ITween tween in Animation.tweens)
                {
                    if (!tween.Tick(Window.frameDeltaTime))
                    {
                        tweensToRemove.Add(tween);
                    }
                }
                foreach (ITween tween in tweensToRemove)
                {
                    tweens.Remove(tween);
                }
            }
        }

    }
}