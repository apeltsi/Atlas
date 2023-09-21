namespace SolidCode.Atlas.ECS;

/// <summary>
/// Represents a component that can be attached to an entity.
/// Components are the main way to add functionality to an entity.
/// </summary>
public abstract class Component
{
    private bool _enabled;

    private Entity? _entity;
    internal bool IsNew = true;

    /// <summary>
    /// Runs some important initialization code for the component.
    /// </summary>
    protected Component()
    {
        var updateMethod = GetType().GetMethod("Update");

        if (updateMethod != null)
            EntityComponentSystem.RegisterComponentUpdateMethod(this, () => updateMethod.Invoke(this, null));
        // We'll find our tick methods by looking for methods ending in "Tick"
        // as these are prime candidates, if the ECS has a tick thread with that name. Then we'll add it.
        // We'll also check if a tick by the name of "Tick()" exists, and add it to the Main tick thread

        foreach (var m in GetType().GetMethods())
            if (m.Name.EndsWith("Tick"))
            {
                if (m.Name == "Tick")
                {
                    EntityComponentSystem.RegisterComponentTickMethod(this, () => m.Invoke(this, null), "Main");
                }
                else
                {
                    var tickName = m.Name.Substring(0, m.Name.Length - 4);
                    EntityComponentSystem.RegisterComponentTickMethod(this, () => m.Invoke(this, null), tickName);
                }
            }
    }

    public bool Enabled
    {
        get => _enabled;
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

    public Entity Entity
    {
        get
        {
            if (_entity == null)
                throw new EntityComponentSystem.ECSException(
                    "Entity is uninitialized. You can't access an Entity in the constructor of a component! Use Start() or OnEnable() instead.");
            return _entity;
        }
        internal set => _entity = value;
    }


    public void TryInvokeMethod(string method)
    {
        var methodToInvoke = GetType().GetMethod(method);
        if (methodToInvoke == null) return;

        methodToInvoke.Invoke(this, null);
    }

    /// <summary>
    /// Gets a component from the entity
    /// </summary>
    /// <param name="allowInheritedClasses">Should components that inherit this type be included in the search?</param>
    /// <typeparam name="T">The type of component to get </typeparam>
    /// <returns>The component, if found</returns>
    protected T? GetComponent<T>(bool allowInheritedClasses = false) where T : Component
    {
        return Entity.GetComponent<T>(allowInheritedClasses);
    }

    /// <summary>
    /// Adds a component to the entity
    /// </summary>
    /// <typeparam name="T">The type of component to add </typeparam>
    /// <returns>The component</returns>
    protected T? AddComponent<T>() where T : Component, new()
    {
        return Entity.AddComponent<T>();
    }

    /// <summary>
    /// Removes a component from an entity
    /// </summary>
    /// <param name="allowInheritedClasses">Should components that inherit this type be included in the search?</param>
    /// <typeparam name="T">The type of component to be removed </typeparam>
    /// <returns>This entity</returns>
    protected void RemoveComponent<T>(bool allowInheritedClasses = false) where T : Component
    {
        Entity.RemoveComponent<T>(allowInheritedClasses);
    }

    /// <summary>
    /// Removes a component from an entity
    /// </summary>
    /// <param name="c">The component to be removed</param>
    /// <returns>This entity</returns>
    protected void RemoveComponent(Component c)
    {
        Entity.RemoveComponent(c);
    }

    internal void UnregisterMethods()
    {
        var updateMethod = GetType().GetMethod("Update");
        var tickMethod = GetType().GetMethod("Tick");

        if (updateMethod != null) EntityComponentSystem.UnregisterComponentUpdateMethod(this);
        if (tickMethod != null)
            foreach (var m in GetType().GetMethods())
                if (m.Name.EndsWith("Tick"))
                {
                    if (m.Name == "Tick")
                    {
                        EntityComponentSystem.UnregisterComponentTickMethod(this, "Main");
                    }
                    else
                    {
                        var tickName = m.Name.Substring(0, m.Name.Length - 4);
                        EntityComponentSystem.UnregisterComponentTickMethod(this, tickName);
                    }
                }
    }
}