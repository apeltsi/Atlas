using System.Reflection;

namespace SolidCode.Atlas.ECS
{
    public abstract class Component
    {
        internal bool IsNew = true;
        private bool _enabled = false;
        public bool Enabled
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
                        TryInvokeMethod("OnEnable");
                        break;
                    case false:
                        TryInvokeMethod("OnDisable");
                        break;
                }
            }
        }


        public void TryInvokeMethod(string method)
        {
            MethodInfo? methodToInvoke = this.GetType().GetMethod(method);
            if (methodToInvoke == null) return;
            
            methodToInvoke.Invoke(this, null);
            
        }

        private Entity? _entity;
        public Entity Entity
        {
            get
            {
                if (_entity == null)
                {
                    throw new EntityComponentSystem.ECSException(
                        "Entity is uninitialized. You can't access an Entity in the constructor of a component! Use Start() or OnEnable() instead.");
                }
                return _entity;
            }
            internal set => _entity = value;
        }

        protected T? GetComponent<T>(bool allowInheritedClasses = false) where T : Component
        {
            return Entity.GetComponent<T>(allowInheritedClasses);
        }
        
        protected T? AddComponent<T>() where T : Component, new()
        {
            return Entity.AddComponent<T>();
        }

        protected void RemoveComponent<T>() where T : Component
        {
            Entity.RemoveComponent<T>();
        }

        protected void RemoveComponent(Component c)
        {
            Entity.RemoveComponent(c);
        }

        protected Component()
        {
            MethodInfo? updateMethod = this.GetType().GetMethod("Update");

            if (updateMethod != null)
            {
                EntityComponentSystem.RegisterComponentUpdateMethod(this, () => updateMethod.Invoke(this, null));
            }
            // We'll find our tick methods by looking for methods ending in "Tick"
            // as these are prime candidates, if the ECS has a tick thread with that name. Then we'll add it.
            // We'll also check if a tick by the name of "Tick()" exists, and add it to the Main tick thread

            foreach (var m in this.GetType().GetMethods())
            {
                if (m.Name.EndsWith("Tick"))
                {
                    if (m.Name == "Tick")
                    {
                        EntityComponentSystem.RegisterComponentTickMethod(this, () => m.Invoke(this, null), "Main");
                    }
                    else
                    {
                        string tickName = m.Name.Substring(0, m.Name.Length - 4);
                        EntityComponentSystem.RegisterComponentTickMethod(this, () => m.Invoke(this, null), tickName);
                    }
                }
            }
        }
        internal void UnregisterMethods()
        {
            MethodInfo? updateMethod = this.GetType().GetMethod("Update");
            MethodInfo? tickMethod = this.GetType().GetMethod("Tick");

            if (updateMethod != null)
            {
                EntityComponentSystem.UnregisterComponentUpdateMethod(this);
            }
            if (tickMethod != null)
            {
                foreach (var m in this.GetType().GetMethods())
                {
                    if (m.Name.EndsWith("Tick"))
                    {
                        if (m.Name == "Tick")
                        {
                            EntityComponentSystem.UnregisterComponentTickMethod(this, "Main");
                        }
                        else
                        {
                            string tickName = m.Name.Substring(0, m.Name.Length - 4);
                            EntityComponentSystem.UnregisterComponentTickMethod(this, tickName);
                        }
                    }
                }

                
            }

        }
    }
}
