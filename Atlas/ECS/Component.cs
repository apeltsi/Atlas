using System.Reflection;
using SolidCode.Atlas.Telescope;

namespace SolidCode.Atlas.ECS
{
    public abstract class Component
    {
        internal bool isNew = true;
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
            try
            {
                this.GetType().GetMethod(method)?.Invoke(this, null);
            }
            catch (Exception _)
            {

            }
        }

        [HideInInspector]
        public Entity? entity;

        protected Component()
        {
            MethodInfo? updateMethod = this.GetType().GetMethod("Update");
            MethodInfo? tickMethod = this.GetType().GetMethod("Tick");

            if (updateMethod != null)
            {
                EntityComponentSystem.RegisterUpdateMethod(this, () => updateMethod.Invoke(this, null));
            }
            if (tickMethod != null)
            {
                EntityComponentSystem.RegisterTickMethod(this, () => tickMethod.Invoke(this, null));
            }
        }
        internal void UnregisterMethods()
        {
            MethodInfo? updateMethod = this.GetType().GetMethod("Update");
            MethodInfo? tickMethod = this.GetType().GetMethod("Tick");

            if (updateMethod != null)
            {
                EntityComponentSystem.UnregisterUpdateMethod(this);
            }
            if (tickMethod != null)
            {
                EntityComponentSystem.UnregisterTickMethod(this);
            }

        }
    }
}
