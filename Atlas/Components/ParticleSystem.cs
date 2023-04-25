using System.Numerics;
using SolidCode.Atlas.Standard;

namespace SolidCode.Atlas.Components;

public class ParticleSystem : InstancedSpriteRenderer
{
    public class Particle
    {
        public bool Alive = false;
        private ParticleSystem _system;
        private Vector2 _position;
        public Vector2 Position
        {
            get => _position;
            set
            {
                _position = value;
                if (_system.Instances.Length <= Index) return;
                InstanceData data = _system.Instances[(int)Index];
                data.InstancePosition = value;
                _system.Instances[(int)Index] = data;
                _system.UpdateData();
            }
        }
        private Vector2 _scale;
        public Vector2 Scale
        {
            get => _scale;
            set
            {
                _scale = value;
                if (_system.Instances.Length <= Index) return;
                InstanceData data = _system.Instances[(int)Index];
                data.InstanceScale = value;
                _system.Instances[(int)Index] = data;
                _system.UpdateData();
            }
        }
        private float _rotation;
        public float Rotation
        {
            get => _rotation;
            set
            {
                _rotation = value;
                if (_system.Instances.Length <= Index) return;
                InstanceData data = _system.Instances[(int)Index];
                data.InstanceRotation = value;
                _system.Instances[(int)Index] = data;
                _system.UpdateData();
            }
        }
        public Vector2 Velocity;
        public Vector2 Acceleration;
        private Vector4 _color;
        public Vector4 Color
        {
            get => _color;
            set
            {
                _color = value;
                if (_system.Instances.Length <= Index) return;
                InstanceData data = _system.Instances[(int)Index];
                data.InstanceColor = value;
                _system.Instances[(int)Index] = data;
                _system.UpdateData();
            }
        }
        public uint Index;
        public float Age;
        public float Lifetime;

        public Particle(ParticleSystem system, uint index)
        {
            this._system = system;
            this.Index = index;
    
            this.Scale = Vector2.Zero;
            this._scale = _system.InitialScale();
            
            this.Color = _system.InitialColor();
            this.Position = _system.InitialPosition();
            this.Velocity = _system.InitialVelocity();
            this.Lifetime = _system.InitialLifetime();
        }

        public void ForceUpdate()
        {
            this.Color = _color;
            this.Position = _position;
            this.Rotation = _rotation;
            this.Scale = _scale;
        }
    }

    private uint _particleCount = 20;
    private bool _hasStarted = false;
    public uint ParticleCount
    {
        get => _particleCount;
        set
        {
            _particleCount = value;
            if(_hasStarted)
                GenerateInstances();
        }
    }
    private uint _prevParticleCount = 0;
    private List<Particle> _particles = new();

    public delegate Vector2 Vector2Generator();
    public delegate Vector4 Vector4Generator();
    public delegate float FloatGenerator();

    public delegate void ParticleUpdateMethod(ref Particle particle);

    public List<ParticleUpdateMethod> ParticleUpdates = new List<ParticleUpdateMethod>()
    {
        (ref Particle particle) =>
        {
            particle.Velocity += particle.Acceleration * (float)Time.deltaTime;
            particle.Position += particle.Velocity * (float)Time.deltaTime;
        },
        (ref Particle particle) =>
        {
            particle.Color = new Vector4(1f - particle.Age / particle.Lifetime);
            particle.Scale += new Vector2(0.5f, 0.5f) * (float)Time.deltaTime;
        } 
    };
    public Vector2Generator InitialVelocity = () => new Vector2(0, 0.75f);
    public Vector4Generator InitialColor = () => new Vector4(1f);
    public Vector2Generator InitialScale = () => new Vector2(0.1f, 0.1f);
    public Vector2Generator InitialPosition = () => ARandom.Vector2();
    public FloatGenerator InitialLifetime = () => 1f;

    public float Period = 5f;
    private float _currentPeriod = 0f;
    public void Start()
    {
        GenerateInstances();
        _hasStarted = true;
    }
    
    public void Update()
    {
        _currentPeriod += (float)Time.deltaTime;
        if (_currentPeriod > Period)
        {
            _currentPeriod -= Period;
        }
        for (int i = 0; i < _particles.Count; i++)
        {
            Particle p = _particles[i];

            if (p.Alive)
            {
                p.Age += (float)Time.deltaTime;
                if (p.Age > p.Lifetime)
                {
                    _particles[i] = new Particle(this, (uint)i);
                    continue;
                }
                foreach (var pu in ParticleUpdates)
                {
                    pu(ref p);
                }
            }
            else
            {
                if ((Period / (float)ParticleCount) * i < _currentPeriod)
                {
                    p.Alive = true;
                    p.ForceUpdate();
                }
            }
        }
        
    }

    public void GenerateInstances()
    {
        if (ParticleCount > _prevParticleCount)
        {
            InstanceData[] instances = new InstanceData[ParticleCount];
            InstanceData[] oldInstances = Instances;
            for (int i = 0; i < oldInstances.Length; i++)
            {
                instances[i] = oldInstances[i];
            }

            int toAdd = (int)ParticleCount - (int)_prevParticleCount;
            for (int i = 0; i < toAdd; i++)
            {
                Particle p = new Particle(this, (uint)i + _prevParticleCount);
                instances[i + (int)_prevParticleCount] = new InstanceData(p.Position, p.Rotation, Vector2.Zero, p.Color);
                _particles.Add(p);
            }

            Instances = instances;
        } else if (ParticleCount < _prevParticleCount)
        {
            InstanceData[] instances = new InstanceData[ParticleCount];
            for (int i = 0; i < ParticleCount; i++)
            {
                instances[i] = Instances[i];
            }
            int toRemove = (int)_prevParticleCount - (int)ParticleCount;
            for (int i = 0; i < toRemove; i++)
            {
                _particles.RemoveAt(_particles.Count - 1);
            }
            Instances = instances;
        }
        _prevParticleCount = ParticleCount;
        UpdateData();

    }
    
    
}