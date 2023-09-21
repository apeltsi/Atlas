using System.Collections.Concurrent;
using System.Numerics;
using SolidCode.Atlas.Rendering;

namespace SolidCode.Atlas.ECS;

/// <summary>
/// Represents an entity in the Entity Component System.
/// Entities can have components attached to them, to add functionality & keep track of data.
/// By default, entities are created with a <c>Transform</c> component attached to them
/// </summary>
public class Entity
{
    private readonly ConcurrentQueue<Component> _componentsToAdd = new();
    private readonly ConcurrentQueue<Component> _componentsToRemove = new();
    public bool Enabled = true;

    public string Name;

    /// <summary>
    /// Creates a new <c>Entity</c> with a <c>Transform</c> component attached to it. The entity will be added as a child
    /// of the <c>RootEntity</c>.
    /// </summary>
    /// <param name="name">The name of the entity</param>
    /// <param name="position">The position of the entity</param>
    /// <param name="scale">The scale of the entity</param>
    public Entity(string name, Vector2? position = null, Vector2? scale = null)
    {
        Children = new List<Entity>();
        Components = new List<Component>();
        Name = name;
        Parent = EntityComponentSystem.RootEntity;
        EntityComponentSystem.RootEntity.AddChildren(this);

        var pos = Vector2.Zero;
        if (position != null) pos = (Vector2)position;
        var sca = Vector2.One;
        if (scale != null) sca = (Vector2)scale;
        var t = AddComponent<Transform>();
        t.Position = pos;
        t.Scale = sca;
    }

    /// <summary>
    /// Creates a new <c>Entity</c>
    /// </summary>
    /// <param name="name">The name of the entity</param>
    /// <param name="transform">Should the entity be initialized with a transform</param>
    public Entity(string name, bool transform)
    {
        Children = new List<Entity>();
        Components = new List<Component>();
        Name = name;
        if (EntityComponentSystem.RootEntity != null && EntityComponentSystem.DestroyedRoot != null)
        {
            Parent = EntityComponentSystem.RootEntity;
            EntityComponentSystem.RootEntity.AddChildren(this);
        }
        else
        {
            Parent = this;
        }

        if (transform)
        {
            var t = AddComponent<Transform>();
            t.Position = Vector2.Zero;
            t.Scale = Vector2.One;
        }
    }

    public List<Entity> Children { get; } = new();
    public bool IsDestroyed { get; internal set; } = false;
    public Entity Parent { get; protected set; }
    public List<Component> Components { get; } = new();
    public List<RenderComponent> RenderingComponents { get; } = new();

    /// <summary>
    /// Sets the parent
    /// </summary>
    public void SetParent(Entity e)
    {
        e.AddChildren(this);
    }

    /// <summary>
    /// Forces the parent to be set. This should only be used internally, unless you know what you're doing.
    /// </summary>
    public void ForceParent(Entity e)
    {
        Parent = e;
    }

    /// <summary>
    /// Returns the first child with the given name, or null if no child with the given name exists
    /// </summary>
    /// <param name="childName">Name of the child, case-sensitive</param>
    /// <returns>The child entity, if found</returns>
    public Entity? GetChildByName(string childName)
    {
        for (var i = 0; i < Children.Count; i++)
        {
            var e = Children[i];
            if (e.Name == childName) return e;
        }

        return null;
    }

    /// <summary>
    /// Adds a component to the entity
    /// </summary>
    /// <typeparam name="T">The type of component to add </typeparam>
    /// <returns>The component</returns>
    public T AddComponent<T>() where T : Component, new()
    {
        var attr = (LimitInstanceCountAttribute?)Attribute.GetCustomAttribute(typeof(T),
            typeof(LimitInstanceCountAttribute));
        if (attr != null)
        {
            if (EntityComponentSystem.InstanceCount.GetValueOrDefault(typeof(T), 0) >= attr.Count)
            {
                Telescope.Debug.Error(LogCategory.ECS, "Maximum instance count of component \"" + typeof(T) + "\"");
                return
                    null!; // FIXME(amos): It might be a bit harsh to return a null reference, although it could also be argued that it is up to the developer to check for null references when adding components with an instance limit
            }

            Func<Type, int> add = type => 1;
            Func<Type, int, int> update = (type, amount) => Interlocked.Add(ref amount, 1);
            EntityComponentSystem.InstanceCount.AddOrUpdate(typeof(T), add, update);
        }

        Component component = new T();
        component.Entity = this;
        _componentsToAdd.Enqueue(component);
        EntityComponentSystem.AddDirtyEntity(UpdateComponentsAndChildren);

        return (T)component;
    }

    /// <summary>
    /// Removes a component from an entity
    /// </summary>
    /// <param name="component">The component to be removed</param>
    /// <returns>This entity</returns>
    public Entity RemoveComponent(Component component)
    {
        _componentsToRemove.Enqueue(component);
        EntityComponentSystem.AddDirtyEntity(UpdateComponentsAndChildren);
        return this;
    }

    /// <summary>
    /// Removes a component from an entity
    /// </summary>
    /// <param name="allowInheritedClasses">Should components that inherit this type be included in the search?</param>
    /// <typeparam name="T">The type of component to be removed </typeparam>
    /// <returns>This entity</returns>
    public Entity RemoveComponent<T>(bool allowInheritedClasses = false) where T : Component
    {
        Component? c = GetComponent<T>(allowInheritedClasses);
        if (c != null)
            RemoveComponent(c);
        return this;
    }

    /// <summary>
    /// Gets a component from the entity
    /// </summary>
    /// <param name="allowInheritedClasses">Should components that inherit this type be included in the search?</param>
    /// <typeparam name="T">The type of component to get </typeparam>
    /// <returns>The component, if found</returns>
    public T? GetComponent<T>(bool allowInheritedClasses = false) where T : Component
    {
        var queuedComponents = _componentsToAdd.ToArray();
        var curComponents = Components.ToArray();
        foreach (var c in curComponents)
        {
            if (c == null)
                continue;
            if (typeof(T) == c.GetType())
                return (T)c;
            if (allowInheritedClasses && c as T != null) return (T)c;
        }

        // It is possible, that the component the user is trying to access is still in the queue. lets check if that's the case
        foreach (var c in queuedComponents)
        {
            if (c == null)
                continue;
            if (typeof(T) == c.GetType())
                return (T)c;
            if (allowInheritedClasses && c as T != null) return (T)c;
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
                if (e != null)
                {
                    if (e.Parent != this)
                    {
                        lock (e.Parent.Children)
                        {
                            e.Parent.Children.Remove(e);
                            e.Parent = this;
                            var tr = e.GetComponent<Transform>(true);
                            if (tr != null) tr.Layer = GetComponent<Transform>(true)?.Layer ?? 0;
                            Children.Add(e);
                        }
                    }
                    else
                    {
                        if (!Children.Contains(e)) Children.Add(e);
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
        lock (Children)
        {
            foreach (var e in childrenToRemove)
                if (e != null)
                    Children.Remove(e);
        }

        return this;
    }

    /// <summary>
    /// Destroys children provided
    /// </summary>
    /// <returns>Self</returns>
    public Entity DestroyChildren(params Entity[] childrenToDestroy)
    {
        lock (Children)
        {
            foreach (var e in childrenToDestroy)
                if (e != null)
                {
                    Children.Remove(e);
                    e.Parent = EntityComponentSystem.DestroyedRoot;
                    e.Destroy();
                }
        }

        return this;
    }

    private void UpdateComponentsAndChildren()
    {
        while (_componentsToAdd.Count > 0)
        {
            Component? component;
            _componentsToAdd.TryDequeue(out component);
            if (component != null)
            {
                component.Entity = this;
                if (EntityComponentSystem.HasStarted) EntityComponentSystem.AddStartMethod(component);
                Components.Add(component);
                if (typeof(RenderComponent).IsAssignableFrom(component.GetType()))
                    RenderingComponents.Add((RenderComponent)component);
            }
        }

        while (_componentsToRemove.Count > 0)
        {
            Component? component;
            _componentsToRemove.TryDequeue(out component);
            if (component != null)
            {
                var attr = (LimitInstanceCountAttribute?)Attribute.GetCustomAttribute(component.GetType(),
                    typeof(LimitInstanceCountAttribute));
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
                    RenderingComponents.Remove((RenderComponent)component);
            }
        }
    }

    /// <summary>
    /// Returns all children of this entity, their children and so on
    /// </summary>
    public Entity[] GetAllChildrenRecursively()
    {
        var allchildren = new List<Entity>();
        foreach (var e in Children)
        {
            allchildren.Add(e);
            allchildren.AddRange(e.GetAllChildrenRecursively());
        }

        return allchildren.ToArray();
    }


    /// <summary>
    /// Destroy this entity
    /// </summary>
    public void Destroy()
    {
        EntityComponentSystem.RemoveEntity(this);
    }

    /// <summary>
    /// The name of the entity
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return Name;
    }
}