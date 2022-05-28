using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolidCode.Caerus.ECS
{
    class Component
    {
        private bool _enabled = true;
        public bool enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
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
        public virtual void OnEnable()
        {

        }
        public virtual void OnDisable()
        {

        }
    }
}
