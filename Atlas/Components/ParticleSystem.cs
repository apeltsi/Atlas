using System.Numerics;
using SolidCode.Atlas.Standard;

namespace SolidCode.Atlas.Components;

/// <summary>
/// A customizable particle system
/// </summary>
public class ParticleSystem : InstancedSpriteRenderer
{
    public delegate float FloatGenerator();

    public delegate void ParticleUpdateMethod(ref Particle particle);

    public delegate Vector2 Vector2Generator();

    public delegate Vector4 Vector4Generator();

    private readonly Queue<Particle> _deadParticles = new();
    private readonly List<Particle> _particles = new();


    private float _currentPeriod;
    private bool _hasStarted;

    private uint _particleCount = 10;
    private uint _prevParticleCount;
    public bool Burst = false;
    public Vector4Generator InitialColor = () => new Vector4(1f);
    public FloatGenerator InitialLifetime = () => 1f;
    public Vector2Generator InitialPosition = () => ARandom.Vector2();
    public FloatGenerator InitialRotation = () => 0f;
    public Vector2Generator InitialScale = () => new Vector2(0.1f, 0.1f);
    public Vector2Generator InitialVelocity = () => new Vector2(0, 0.75f);

    public List<ParticleUpdateMethod> ParticleUpdates = new()
    {
        (ref Particle particle) =>
        {
            particle.Velocity += particle.Acceleration * (float)Time.deltaTime;
            particle.Position += particle.Velocity * (float)Time.deltaTime;
        },
        (ref Particle particle) =>
        {
            particle.Color = new Color(particle.Color.R, particle.Color.G, particle.Color.B,
                1f - particle.Age / particle.Lifetime);
            particle.Scale += new Vector2(0.5f, 0.5f) * (float)Time.deltaTime;
        }
    };

    public uint ParticleCount
    {
        get => _particleCount;
        set
        {
            _particleCount = value;
            if (_hasStarted)
                GenerateInstances();
        }
    }

    public void Start()
    {
        GenerateInstances();
        _hasStarted = true;
    }

    public void Update()
    {
        _currentPeriod += (float)Time.deltaTime;


        var maxLifeTime = InitialLifetime();
        for (var i = 0; i < _particles.Count; i++)
        {
            var p = _particles[i];
            maxLifeTime = Math.Max(maxLifeTime, p.Lifetime);
            if (p.Alive)
            {
                p.Age += (float)Time.deltaTime;
                if (p.Age > p.Lifetime)
                {
                    _particles[i] = new Particle(this, (uint)i);
                    _deadParticles.Enqueue(_particles[i]);
                    continue;
                }

                foreach (var pu in ParticleUpdates) pu(ref p);
            }
            else if (!Burst)
            {
                if (!_deadParticles.Contains(p)) _deadParticles.Enqueue(p);
                if (maxLifeTime / ParticleCount < _currentPeriod)
                {
                    var dp = _deadParticles.Dequeue();
                    dp.Alive = true;
                    dp.ForceUpdate();
                    _currentPeriod = 0f;
                }
            }
        }
    }

    public void GenerateInstances()
    {
        if (ParticleCount > _prevParticleCount)
        {
            var instances = new InstanceData[ParticleCount];
            var oldInstances = Instances;
            for (var i = 0; i < oldInstances.Length; i++) instances[i] = oldInstances[i];

            var toAdd = (int)ParticleCount - (int)_prevParticleCount;
            for (var i = 0; i < toAdd; i++)
            {
                var p = new Particle(this, (uint)i + _prevParticleCount);
                if (Burst) p.Alive = true;
                instances[i + (int)_prevParticleCount] =
                    new InstanceData(p.Position, p.Rotation, Vector2.Zero, p.Color);
                _particles.Add(p);
            }

            Instances = instances;
        }
        else if (ParticleCount < _prevParticleCount)
        {
            var instances = new InstanceData[ParticleCount];
            for (var i = 0; i < ParticleCount; i++) instances[i] = Instances[i];
            var toRemove = (int)_prevParticleCount - (int)ParticleCount;
            for (var i = 0; i < toRemove; i++) _particles.RemoveAt(_particles.Count - 1);
            Instances = instances;
        }

        _prevParticleCount = ParticleCount;
        UpdateData();
    }

    public class Particle
    {
        private readonly ParticleSystem _system;
        private Color _color;
        private Vector2 _position;
        private float _rotation;
        private Vector2 _scale;
        public Vector2 Acceleration;
        public float Age;
        public bool Alive;
        public uint Index;
        public float Lifetime;
        public Vector2 Velocity;

        public Particle(ParticleSystem system, uint index)
        {
            _system = system;
            Index = index;

            Scale = Vector2.Zero;
            _scale = _system.InitialScale();

            Color = _system.InitialColor();
            Position = _system.InitialPosition();
            Velocity = _system.InitialVelocity();
            Lifetime = _system.InitialLifetime();
            Rotation = _system.InitialRotation();
        }

        public Vector2 Position
        {
            get => _position;
            set
            {
                _position = value;
                if (_system.Instances.Length <= Index) return;
                var data = _system.Instances[(int)Index];
                data.InstancePosition = value;
                _system.Instances[(int)Index] = data;
                _system.UpdateData();
            }
        }

        public Vector2 Scale
        {
            get => _scale;
            set
            {
                _scale = value;
                if (_system.Instances.Length <= Index) return;
                var data = _system.Instances[(int)Index];
                data.InstanceScale = value;
                _system.Instances[(int)Index] = data;
                _system.UpdateData();
            }
        }

        public float Rotation
        {
            get => _rotation;
            set
            {
                _rotation = value;
                if (_system.Instances.Length <= Index) return;
                var data = _system.Instances[(int)Index];
                data.InstanceRotation = value;
                _system.Instances[(int)Index] = data;
                _system.UpdateData();
            }
        }

        public Color Color
        {
            get => _color;
            set
            {
                _color = value;
                if (_system.Instances.Length <= Index) return;
                var data = _system.Instances[(int)Index];
                data.InstanceColor = value;
                _system.Instances[(int)Index] = data;
                _system.UpdateData();
            }
        }

        public void ForceUpdate()
        {
            Color = _color;
            Position = _position;
            Rotation = _rotation;
            Scale = _scale;
        }
    }
}