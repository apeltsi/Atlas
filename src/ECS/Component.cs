namespace SolidCode.Atlas.ECS
{
    public abstract class Component
    {
        private bool _enabled = false;
        public bool enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                if (_enabled == value)
                    return;
                _enabled = value;
                switch (value)
                {
                    case true:

                        this.OnEnable();
                        break;
                    case false:
                        this.OnDisable();
                        break;
                }
            }
        }


        [HideInInspector]
        public Entity? entity;

        public virtual void Start()
        {

        }
        public virtual void Update()
        {

        }
        public virtual void FixedUpdate()
        {

        }
        public virtual void OnRemove()
        {

        }
        protected virtual void OnEnable()
        {

        }
        protected virtual void OnDisable()
        {
        }
    }
}
