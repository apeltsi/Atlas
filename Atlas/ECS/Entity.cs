namespace SolidCode.Atlas.ECS
{
    using System.Collections.Concurrent;
    using System.Numerics;
    using SolidCode.Atlas.Components;
    using SolidCode.Atlas.Rendering;
    using SolidCode.Atlas.Telescope;
    public class Entity
    {

        public string name;
        public bool enabled = true;
        public List<Entity> children = new List<Entity>();
        public Entity parent { get; protected set; }
        public List<Component> components = new List<Component>();
        public List<RenderComponent> renderingComponents = new List<RenderComponent>();
        private ConcurrentQueue<Entity> _childrenToAdd = new ConcurrentQueue<Entity>();
        private ConcurrentQueue<Entity> _childrenToRemove = new ConcurrentQueue<Entity>();
        private ConcurrentQueue<Entity> _childrenToDestroy = new ConcurrentQueue<Entity>();

        private ConcurrentQueue<Component> _componentsToAdd = new ConcurrentQueue<Component>();
        private ConcurrentQueue<Component> _componentsToRemove = new ConcurrentQueue<Component>();

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
            e.AddChildren(this);
        }

        public void ForceParent(Entity e)
        {
            parent = e;
        }

        public Entity? GetChildByName(string childName)
        {
            for (int i = 0; i < this.children.Count; i++)
            {
                Entity e = this.children[i];
                if (e.name == childName)
                {
                    return e;
                }
            }
            return null;
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
            _componentsToAdd.Enqueue(component);
            EntityComponentSystem.AddDirtyEntity(UpdateComponentsAndChildren);

            return (T)component;
        }
        public Entity RemoveComponent(Component component)
        {
            _componentsToRemove.Enqueue(component);
            EntityComponentSystem.AddDirtyEntity(UpdateComponentsAndChildren);
            return this;
        }
        public T? GetComponent<T>(bool allowInheritedClasses = false) where T : Component
        {
            Component[] queuedComponents = _componentsToAdd.ToArray();
            Component[] curComponents = components.ToArray();
            foreach (Component c in curComponents)
            {
                if (c == null)
                    continue;
                if (typeof(T) == c.GetType())
                {
                    return (T)c;
                }
                else if (allowInheritedClasses && (c as T) != null)
                {
                    return (T)c;
                }
            }
            // It is possible, that the component the user is trying to access is still in the queue. lets check if thats the case
            foreach (Component c in queuedComponents)
            {
                if (c == null)
                    continue;
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


        /// <summary>
        /// Assigns children to Entity
        /// </summary>
        /// <returns>
        /// Self
        /// </returns>
        public Entity AddChildren(params Entity[] childrenToAdd)
        {
            foreach (Entity e in childrenToAdd)
            {
                _childrenToAdd.Enqueue(e);
            }
            EntityComponentSystem.AddDirtyEntity(UpdateComponentsAndChildren);
            return this;
        }

        /// <summary>
        /// Removes children from Entity
        /// </summary>
        /// <returns>
        /// Self
        /// </returns>
        public Entity RemoveChildren(params Entity[] childrenToRemove)
        {
            foreach (Entity e in childrenToRemove)
            {
                _childrenToRemove.Enqueue(e);
            }
            EntityComponentSystem.AddDirtyEntity(UpdateComponentsAndChildren);
            return this;
        }
        /// <summary>
        /// Destroys children
        /// </summary>
        /// <returns>
        /// Self
        /// </returns>

        public Entity DestroyChildren(params Entity[] childrenToDestroy)
        {
            foreach (Entity e in childrenToDestroy)
            {
                _childrenToDestroy.Enqueue(e);
            }
            EntityComponentSystem.AddDirtyEntity(UpdateComponentsAndChildren);

            return this;
        }

        void UpdateComponentsAndChildren()
        {
            while (_childrenToAdd.Count > 0)
            {
                Entity? e;
                _childrenToAdd.TryDequeue(out e);
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

            while (_childrenToRemove.Count > 0)
            {
                Entity? e;
                _childrenToRemove.TryDequeue(out e);
                if (e != null)
                {
                    children.Remove(e);
                }
            }

            while (_childrenToDestroy.Count > 0)
            {
                Entity? e;
                _childrenToDestroy.TryDequeue(out e);
                if (e != null)
                {
                    children.Remove(e);
                    e.parent = EntityComponentSystem.DestroyedRoot;
                    e.Destroy();
                }
            }

            while (_componentsToAdd.Count > 0)
            {

                Component? component;
                _componentsToAdd.TryDequeue(out component);
                if (component != null)
                {
                    component.entity = this;
                    if (EntityComponentSystem.HasStarted)
                    {
                        EntityComponentSystem.AddStartMethod(component);
                    }
                    components.Add(component);
                    if (typeof(RenderComponent).IsAssignableFrom(component.GetType()))
                    {
                        renderingComponents.Add((RenderComponent)component);
                    }

                }
            }

            while (_componentsToRemove.Count > 0)
            {
                Component? component;
                _componentsToRemove.TryDequeue(out component);
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
                    component.enabled = false;
                    component.entity = null;
                    component.TryInvokeMethod("OnRemove");

                    components.Remove(component);
                    if (typeof(RenderComponent).IsAssignableFrom(component.GetType()))
                    {
                        renderingComponents.Remove((RenderComponent)component);
                    }
                }
            }

        }

        public Entity[] GetAllChildrenRecursively()
        {
            List<Entity> allchildren = new List<Entity>();
            foreach (Entity e in children)
            {
                allchildren.Add(e);
                allchildren.AddRange(e.GetAllChildrenRecursively());
            }
            return allchildren.ToArray();
        }


        public void Destroy()
        {
            EntityComponentSystem.RemoveEntity(this);
            /*foreach (Component c in components)
            {
                c.OnDisable();
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
            components.Clear();*/
        }
        public override string ToString()
        {
            return this.name;
        }
    }
}
