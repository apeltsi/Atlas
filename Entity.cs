using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using SolidCode.Caerus.Components;

namespace SolidCode.Caerus.ECS
{
    class Entity
    {
        public string name = "Entity";
        public bool enabled = true;
        public List<Entity> children = new List<Entity>();
        public List<Component> components = new List<Component>();
        public Entity(string name, Vector2? position = null, Vector2? scale = null)
        {
            this.children = new List<Entity>();
            this.components = new List<Component>();
            this.name = name;
            Vector2 pos = Vector2.Zero;
            if (position != null)
            {
                pos = (Vector2)position;
            }
            Vector2 sca = Vector2.One;
            if (scale != null)
            {
                sca = (Vector2)scale;
            }
            AddComponent(new Transform(pos, sca));
        }

        public void AddComponent(Component component)
        {
            component.entity = this;

            components.Add(component);
        }
        public void RemoveComponent(Component component)
        {
            component.OnRemove();

            components.Remove(component);
        }
        public T? GetComponent<T>() where T : Component
        {
            foreach (Component c in components)
            {
                if (typeof(T) == c.GetType())
                {
                    return (T)c;
                }
            }
            return default(T);
        }
        public void Start()
        {
            foreach (Component component in components)
            {
                if (component.enabled)
                    component.Start();
            }
        }
        public void Update()
        {
            foreach (Component component in components)
            {
                if (component.enabled)
                    component.Update();
            }
        }

        public void FixedUpdate()
        {
            foreach (Component component in components)
            {
                if (component.enabled)
                    component.FixedUpdate();
            }
        }

    }
}
