namespace SolidCode.Caerus.ECS
{
    using System.Numerics;
    using SolidCode.Caerus.Components;
    using SolidCode.Caerus.Rendering;

    public class Entity
    {

        public string name = "Entity";
        public bool enabled = true;
        public List<Entity> children = new List<Entity>();
        public Entity? parent;
        public List<Component> components = new List<Component>();
        public List<RenderComponent> renderingComponents = new List<RenderComponent>();
        private bool componentsLocked = false;
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
            AddComponent<Transform>();
        }

        public void SetParent(Entity e)
        {
            if (parent != null)
            {
                parent.children.Remove(this);
            }
            parent = e;
            parent.children.Add(this);
        }

        public T AddComponent<T>() where T : Component, new()
        {
            while (componentsLocked)
            {
                Thread.Sleep(20);
            }
            // FIXME(amos): Adding components should only happen when all threads are idle. This is done to avoid a race-condition
            Component component = new T();
            component.entity = this;
            if (Caerus.ecs != null && Caerus.ecs.HasStarted)
                component.OnEnable();
            components.Add(component);
            if (typeof(RenderComponent).IsAssignableFrom(typeof(T)))
            {
                renderingComponents.Add((RenderComponent)component);
            }
            return (T)component;
        }
        public Entity RemoveComponent(Component component)
        {
            while (componentsLocked)
            {
                Thread.Sleep(20);
            }

            // FIXME(amos): Removing components should only happen when all threads are idle. This is done to avoid a race-condition
            component.OnDisable();
            component.OnRemove();

            components.Remove(component);
            if (typeof(RenderComponent).IsAssignableFrom(component.GetType()))
            {
                renderingComponents.Remove((RenderComponent)component);
            }
            return this;
        }
        public T? GetComponent<T>(bool allowInheritedClasses = false) where T : Component
        {
            foreach (Component c in components)
            {
                if (typeof(T) == c.GetType())
                {
                    return (T)c;
                }
                else if (allowInheritedClasses && (c as T) != null)
                {
                    return (T)c;
                }
            }
            return default(T);
        }
        public void Start()
        {
            componentsLocked = true;
            foreach (Component component in components)
            {
                if (component.enabled)
                {
                    component.OnEnable();
                    component.Start();
                }
            }
            foreach (Entity e in children)
            {
                e.Start();
            }
            componentsLocked = false;


        }
        public void Update()
        {
            componentsLocked = true;

            foreach (Component component in components)
            {
                if (component.enabled)
                    component.Update();
            }
            foreach (Entity e in children)
            {
                e.Update();
            }
            componentsLocked = false;

        }

        public void FixedUpdate()
        {
            componentsLocked = true;

            foreach (Component component in components)
            {
                if (component.enabled)
                    component.FixedUpdate();
            }
            foreach (Entity e in children)
            {
                e.FixedUpdate();
            }
            componentsLocked = false;

        }

        /// <summary>
        /// Assigns children to Entity
        /// </summary>
        public Entity AddChildren(params Entity[] children)
        {
            foreach (Entity e in children)
            {
                e.parent = this;
                this.children.Add(e);
            }
            return this;
        }

        public void Destroy()
        {
            foreach (Component c in components)
            {
                c.OnRemove();
            }
            components.Clear();
        }

    }
}
