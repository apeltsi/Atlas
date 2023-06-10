using System.Reflection;
using SolidCode.Atlas.Telescope;

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

        [HideInInspector]
        public Entity? Entity;

        protected Component()
        {
            MethodInfo? updateMethod = this.GetType().GetMethod("Update");
            MethodInfo? tickMethod = this.GetType().GetMethod("Tick");

            if (updateMethod != null)
            {
                EntityComponentSystem.RegisterComponentUpdateMethod(this, () => updateMethod.Invoke(this, null));
            }
            if (tickMethod != null)
            {
                EntityComponentSystem.RegisterComponentTickMethod(this, () => tickMethod.Invoke(this, null));
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
                EntityComponentSystem.UnregisterComponentTickMethod(this);
            }

        }
    }
}
