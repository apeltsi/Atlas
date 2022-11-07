namespace SolidCode.Atlas.ECS
{
    using System.Collections.Concurrent;
    using System.Numerics;
    using SolidCode.Atlas.Components;
    using SolidCode.Atlas.Rendering;

    public class Entity
    {

        public string name = "Entity";
        public bool enabled = true;
        public List<Entity> children = new List<Entity>();
        public Entity parent;
        public List<Component> components = new List<Component>();
        public List<RenderComponent> renderingComponents = new List<RenderComponent>();
        private ConcurrentQueue<Entity> childrenToAdd = new ConcurrentQueue<Entity>();
        private ConcurrentQueue<Entity> childrenToRemove = new ConcurrentQueue<Entity>();
        private ConcurrentQueue<Entity> childrenToDestroy = new ConcurrentQueue<Entity>();

        private ConcurrentQueue<Component> componentsToAdd = new ConcurrentQueue<Component>();
        private ConcurrentQueue<Component> componentsToRemove = new ConcurrentQueue<Component>();

        public Entity(string name, Vector2? position = null, Vector2? scale = null)
        {
            this.children = new List<Entity>();
            this.components = new List<Component>();
            this.name = name;
            this.parent = EntityComponentSystem.RootEntity;
            EntityComponentSystem.RootEntity.AddChildren(this);

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
            Transform t = AddComponent<Transform>();
            t.position = pos;
            t.scale = sca;
        }
        public Entity(string name, bool transform)
        {
            this.children = new List<Entity>();
            this.components = new List<Component>();
            this.name = name;
            if (EntityComponentSystem.RootEntity != null && EntityComponentSystem.DestroyedRoot != null)
            {
                this.parent = EntityComponentSystem.RootEntity;
                EntityComponentSystem.RootEntity.AddChildren(this);
            }
            else
            {
                this.parent = this;
            }
            if (transform)
            {
                Transform t = AddComponent<Transform>();
                t.position = Vector2.Zero;
                t.scale = Vector2.One;
            }
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
            LimitInstanceCountAttribute? attr = (LimitInstanceCountAttribute?)Attribute.GetCustomAttribute(typeof(T), typeof(LimitInstanceCountAttribute));
            if (attr != null)
            {
                if (EntityComponentSystem.InstanceCount.GetValueOrDefault(typeof(T), 0) >= attr.count)
                {
                    Debug.Error(LogCategory.ECS, "Maximum instance count of component \"" + typeof(T).ToString() + "\"");
                    return null!; // FIXME(amos): It might be a bit harsh to return a null reference, although it could also be argued that it is up to the developer to check for null references when adding components with an instance limit
                }
                else
                {
                    Func<Type, int> add = type => 1;
                    Func<Type, int, int> update = (type, amount) => Interlocked.Add(ref amount, 1);
                    EntityComponentSystem.InstanceCount.AddOrUpdate(typeof(T), add, update);
                }
            }

            Component component = new T();
            component.entity = this;
            componentsToAdd.Enqueue(component);
            return (T)component;
        }
        public Entity RemoveComponent(Component component)
        {
            componentsToRemove.Enqueue(component);
            return this;
        }
        public T? GetComponent<T>(bool allowInheritedClasses = false) where T : Component
        {
            if (!EntityComponentSystem.HasStarted)
            {
                UpdateComponentsAndChildren();
            }
            Component[] cur_components = components.ToArray();
            foreach (Component c in cur_components)
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
            return null;
        }

        public void Start()
        {
            UpdateComponentsAndChildren();
            Component[] cur_components = components.ToArray();

            foreach (Component component in cur_components)
            {
                if (component.enabled)
                {
                    component.OnEnable();
                    component.Start();
                }
            }
            Entity[] cur_children = children.ToArray();

            foreach (Entity e in cur_children)
            {
                if (e != null)
                    e.Start();
            }
        }
        public void Update()
        {
            UpdateComponentsAndChildren();
            Component[] cur_components = components.ToArray();
            foreach (Component component in cur_components)
            {
                if (component.enabled)
                    component.Update();
            }
            Entity[] cur_children = children.ToArray();
            foreach (Entity e in cur_children)
            {
                if (e != null)
                    e.Update();
            }
        }

        public void FixedUpdate()
        {
            UpdateComponentsAndChildren();
            Component[] cur_components = components.ToArray();
            foreach (Component component in cur_components)
            {
                if (component.enabled)
                    component.FixedUpdate();
            }
            Entity[] cur_children = children.ToArray();

            foreach (Entity e in cur_children)
            {
                if (e != null)
                    e.FixedUpdate();
            }
        }

        /// <summary>
        /// Assigns children to Entity
        /// </summary>
        /// <returns>
        /// Self
        /// </returns>
        public Entity AddChildren(params Entity[] children)
        {
            foreach (Entity e in children)
            {
                childrenToAdd.Enqueue(e);
            }
            return this;
        }

        /// <summary>
        /// Removes children from Entity
        /// </summary>
        /// <returns>
        /// Self
        /// </returns>
        public Entity RemoveChildren(params Entity[] children)
        {
            foreach (Entity e in children)
            {
                childrenToRemove.Enqueue(e);
            }
            return this;
        }
        /// <summary>
        /// Destroys children
        /// </summary>
        /// <returns>
        /// Self
        /// </returns>

        public Entity DestroyChildren(params Entity[] children)
        {
            foreach (Entity e in children)
            {
                childrenToDestroy.Enqueue(e);
            }

            return this;
        }

        void UpdateComponentsAndChildren()
        {
            while (childrenToAdd.Count > 0)
            {
                Entity? e;
                childrenToAdd.TryDequeue(out e);
                if (e != null)
                {
                    if (e.parent != this)
                    {
                        e.parent.children.Remove(e);
                        e.parent = this;
                        this.children.Add(e);
                    }
                    else
                    {
                        if (!this.children.Contains(e))
                        {
                            this.children.Add(e);
                        }
                    }
                }
            }

            while (childrenToRemove.Count > 0)
            {
                Entity? e;
                childrenToRemove.TryDequeue(out e);
                if (e != null)
                {
                    children.Remove(e);
                }
            }

            while (childrenToDestroy.Count > 0)
            {
                Entity? e;
                childrenToDestroy.TryDequeue(out e);
                if (e != null)
                {
                    children.Remove(e);
                    e.parent = EntityComponentSystem.DestroyedRoot;
                    e.Destroy();
                }
            }

            while (componentsToAdd.Count > 0)
            {
                Component? component;
                componentsToAdd.TryDequeue(out component);
                if (component != null)
                {
                    component.entity = this;
                    if (EntityComponentSystem.HasStarted)
                        component.OnEnable();
                    components.Add(component);
                    if (typeof(RenderComponent).IsAssignableFrom(component.GetType()))
                    {
                        renderingComponents.Add((RenderComponent)component);
                    }

                }
            }

            while (componentsToRemove.Count > 0)
            {
                Component? component;
                componentsToRemove.TryDequeue(out component);
                if (component != null)
                {
                    LimitInstanceCountAttribute? attr = (LimitInstanceCountAttribute?)Attribute.GetCustomAttribute(component.GetType(), typeof(LimitInstanceCountAttribute));
                    if (attr != null)
                    {
                        Func<Type, int> add = type => 0;
                        Func<Type, int, int> update = (type, amount) => Interlocked.Add(ref amount, -1);
                        EntityComponentSystem.InstanceCount.AddOrUpdate(component.GetType(), add, update);
                        // We have to remove the component from the instance count limit
                    }
                    component.OnDisable();
                    component.OnRemove();

                    components.Remove(component);
                    if (typeof(RenderComponent).IsAssignableFrom(component.GetType()))
                    {
                        renderingComponents.Remove((RenderComponent)component);
                    }
                }
            }

        }


        public void Destroy()
        {
            foreach (Component c in components)
            {
                c.OnRemove();
                LimitInstanceCountAttribute? attr = (LimitInstanceCountAttribute?)Attribute.GetCustomAttribute(c.GetType(), typeof(LimitInstanceCountAttribute));
                if (attr != null)
                {
                    Func<Type, int> add = type => 0;
                    Func<Type, int, int> update = (type, amount) => Interlocked.Add(ref amount, -1);
                    EntityComponentSystem.InstanceCount.AddOrUpdate(c.GetType(), add, update);
                    // We have to remove the component from the instance count limit
                }
            }
            components.Clear();
        }

    }
}
