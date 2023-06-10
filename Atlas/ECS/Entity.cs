namespace SolidCode.Atlas.ECS
{
    using System.Collections.Concurrent;
    using System.Numerics;
    using SolidCode.Atlas.Components;
    using SolidCode.Atlas.Rendering;
    using SolidCode.Atlas.Telescope;
    public class Entity
    {

        public string Name;
        public bool Enabled = true;
        public List<Entity> Children { get; private set; } = new List<Entity>();
        public bool IsDestroyed { get; internal set; } = false; 
        public Entity Parent { get; protected set; }
        public List<Component> Components { get; private set; } = new List<Component>();
        public List<RenderComponent> RenderingComponents { get; private set; }= new List<RenderComponent>();

        private ConcurrentQueue<Component> _componentsToAdd = new ConcurrentQueue<Component>();
        private ConcurrentQueue<Component> _componentsToRemove = new ConcurrentQueue<Component>();

        public Entity(string name, Vector2? position = null, Vector2? scale = null)
        {
            this.Children = new List<Entity>();
            this.Components = new List<Component>();
            this.Name = name;
            this.Parent = EntityComponentSystem.RootEntity;
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
            t.Position = pos;
            t.Scale = sca;
        }
        public Entity(string name, bool transform)
        {
            this.Children = new List<Entity>();
            this.Components = new List<Component>();
            this.Name = name;
            if (EntityComponentSystem.RootEntity != null && EntityComponentSystem.DestroyedRoot != null)
            {
                this.Parent = EntityComponentSystem.RootEntity;
                EntityComponentSystem.RootEntity.AddChildren(this);
            }
            else
            {
                this.Parent = this;
            }
            if (transform)
            {
                Transform t = AddComponent<Transform>();
                t.Position = Vector2.Zero;
                t.Scale = Vector2.One;
            }
        }

        public void SetParent(Entity e)
        {
            e.AddChildren(this);
        }

        public void ForceParent(Entity e)
        {
            Parent = e;
        }

        public Entity? GetChildByName(string childName)
        {
            for (int i = 0; i < this.Children.Count; i++)
            {
                Entity e = this.Children[i];
                if (e.Name == childName)
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
                if (EntityComponentSystem.InstanceCount.GetValueOrDefault(typeof(T), 0) >= attr.Count)
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
            component.Entity = this;
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

        public Entity RemoveComponent<T>(bool allowInheritedClasses = false) where T : Component
        {
            Component? c = GetComponent<T>(allowInheritedClasses);
            if(c != null)
                RemoveComponent(c);
            return this;
        }
        
        public T? GetComponent<T>(bool allowInheritedClasses = false) where T : Component
        {
            Component[] queuedComponents = _componentsToAdd.ToArray();
            Component[] curComponents = Components.ToArray();
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
        /// <returns>Self</returns>
        public Entity AddChildren(params Entity[] childrenToAdd)
        {
            lock (Children)
            {
                foreach (var e in childrenToAdd) 
                {
                    if (e != null)
                    {
                        if (e.Parent != this)
                        {
                            lock (e.Parent.Children)
                            {
                                e.Parent.Children.Remove(e);
                                e.Parent = this;
                                Transform? tr = e.GetComponent<Transform>(true);
                                if (tr != null)
                                {
                                    tr.Layer = GetComponent<Transform>(true)?.Layer ?? 0;
                                }
                                this.Children.Add(e);
                            }
                        }
                        else
                        {
                            if (!this.Children.Contains(e))
                            {
                                this.Children.Add(e);
                            }
                        }
                    }
                }
            }
            return this;
        }

        /// <summary>
        /// Removes children from Entity
        /// </summary>
        /// <returns>Self</returns>
        public Entity RemoveChildren(params Entity[] childrenToRemove)
        {
            lock(Children)
                foreach (Entity e in childrenToRemove)
                {
                    if (e != null)
                    {
                        Children.Remove(e);
                    }
                }
            return this;
        }
        /// <summary>
        /// Destroys children provided
        /// </summary>
        /// <returns>Self</returns>
        public Entity DestroyChildren(params Entity[] childrenToDestroy)
        {
            lock(Children)
                foreach (Entity e in childrenToDestroy)
                {
                    if (e != null)
                    {
                        Children.Remove(e);
                        e.Parent = EntityComponentSystem.DestroyedRoot;
                        e.Destroy();
                    }
                }

            return this;
        }

        void UpdateComponentsAndChildren()
        {

            while (_componentsToAdd.Count > 0)
            {

                Component? component;
                _componentsToAdd.TryDequeue(out component);
                if (component != null)
                {
                    component.Entity = this;
                    if (EntityComponentSystem.HasStarted)
                    {
                        EntityComponentSystem.AddStartMethod(component);
                    }
                    Components.Add(component);
                    if (typeof(RenderComponent).IsAssignableFrom(component.GetType()))
                    {
                        RenderingComponents.Add((RenderComponent)component);
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
                    component.Enabled = false;
                    component.Entity = null;
                    component.TryInvokeMethod("OnRemove");

                    Components.Remove(component);
                    if (typeof(RenderComponent).IsAssignableFrom(component.GetType()))
                    {
                        RenderingComponents.Remove((RenderComponent)component);
                    }
                }
            }

        }

        /// <summary>
        /// Returns all children of this entity, their children and so on
        /// </summary>
        public Entity[] GetAllChildrenRecursively()
        {
            List<Entity> allchildren = new List<Entity>();
            foreach (Entity e in Children)
            {
                allchildren.Add(e);
                allchildren.AddRange(e.GetAllChildrenRecursively());
            }
            return allchildren.ToArray();
        }


        public void Destroy()
        {
            EntityComponentSystem.RemoveEntity(this);
        }
        public override string ToString()
        {
            return this.Name;
        }
    }
}
