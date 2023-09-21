using System.Collections.Concurrent;
using SolidCode.Atlas.Telescope;

namespace SolidCode.Atlas.ECS;

/// <summary>
/// The main class for the Entity Component System or ECS.
/// </summary>
public static class EntityComponentSystem
{
    /// <summary>
    /// The root entity, every entity in the ECS is a child or grandchild of this entity.
    /// </summary>
    public static readonly Entity RootEntity = new("ROOT", false);

    /// <summary>
    /// The root entity of all destroyed entities.
    /// <para />
    /// Note that under normal usage this entity should never have any children. As it is only used as a parent reference
    /// for destroyed entities.
    /// <para />
    /// If an entity is referencing DESTROYED_ROOT as its parent, it should be collected by the garbage collector. If this
    /// doesn't happen, it might be a sign of a memory leak.
    /// </summary>
    public static readonly Entity DestroyedRoot = new("DESTROYED_ROOT", false);


    private static readonly ConcurrentQueue<Entity> _removeQueue = new();


    private static readonly List<(int, Action)> _tickTasks = new();
    private static readonly List<(float, Action)> _frameDelays = new();

    private static readonly ConcurrentDictionary<Component, Action> _updateMethods = new();
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<Component, Action>> _tickMethods = new();
    private static readonly ConcurrentQueue<Action> _dirtyEntities = new();
    private static readonly ConcurrentBag<Component> _startMethods = new();
    private static readonly List<Action> _updateActions = new();
    private static readonly List<Action> _tickActions = new();

    public static ConcurrentDictionary<Type, int> InstanceCount = new();

    public static bool HasStarted { get; set; }


    public static void RemoveEntity(Entity entity)
    {
        _removeQueue.Enqueue(entity);
    }

    internal static void AddDirtyEntity(Action method)
    {
        _dirtyEntities.Enqueue(method);
    }

    internal static void AddStartMethod(Component c)
    {
        lock (_startMethods)
        {
            _startMethods.Add(c);
        }
    }

    internal static void RegisterComponentUpdateMethod(Component c, Action method)
    {
        _updateMethods.TryAdd(c, method);
    }

    internal static void RegisterComponentTickMethod(Component c, Action method, string thread)
    {
        lock (_tickMethods)
        {
            ConcurrentDictionary<Component, Action> methods;
            if (_tickMethods.TryGetValue(thread, out methods))
            {
                lock (methods)
                {
                    methods.TryAdd(c, method);
                }
            }
            else
            {
                methods = new ConcurrentDictionary<Component, Action>();
                methods.TryAdd(c, method);
                _tickMethods.TryAdd(thread, methods);
            }
        }
    }

    internal static void UnregisterComponentUpdateMethod(Component c)
    {
        _updateMethods.Remove(c, out _);
    }

    internal static void UnregisterComponentTickMethod(Component c, string thread)
    {
        lock (_tickMethods)
        {
            ConcurrentDictionary<Component, Action> methods;
            if (_tickMethods.TryGetValue(thread, out methods))
                lock (methods)
                {
                    methods.Remove(c, out _);
                }
        }
    }

    /// <summary>
    /// Registers a action to be invoked at every Tick
    /// </summary>
    /// <param name="action">The action to be invoked</param>
    public static void RegisterTickAction(Action action)
    {
        lock (_tickActions)
        {
            _tickActions.Add(action);
        }
    }

    /// <summary>
    /// Registers a action to be invoked at every Frame. (Before rendering)
    /// </summary>
    /// <param name="action">The action to be invoked</param>
    public static void RegisterUpdateAction(Action action)
    {
        lock (_updateActions)
        {
            _updateActions.Add(action);
        }
    }

    /// <summary>
    /// Unregisters an action from being invoked at every Tick
    /// </summary>
    /// <param name="action"></param>
    public static void UnregisterTickAction(Action action)
    {
        lock (_tickActions)
        {
            _tickActions.Remove(action);
        }
    }

    /// <summary>
    /// Unregisters an action from being invoked at every Frame.
    /// </summary>
    /// <param name="action"></param>
    public static void UnregisterUpdateAction(Action action)
    {
        lock (_updateActions)
        {
            _updateActions.Remove(action);
        }
    }


    private static void UpdateECS()
    {
        lock (_dirtyEntities)
        {
            while (_dirtyEntities.Count > 0)
            {
                Action? m;
                _dirtyEntities.TryDequeue(out m);
                if (m != null) m.Invoke();
            }
        }

        lock (_startMethods)
        {
            foreach (var c in _startMethods)
            {
                c.Enabled = true; // This is done so that OnEnable() is called
                c.TryInvokeMethod("Start");
                c.IsNew = false;
            }

            _startMethods.Clear();
        }

        while (_removeQueue.Count > 0)
        {
            Entity? e;
            _removeQueue.TryDequeue(out e);
            if (e != null)
            {
                var entitiesToRemove = new List<Entity>();
                entitiesToRemove.AddRange(e.GetAllChildrenRecursively());

                entitiesToRemove.Add(e);
                e.Parent.RemoveChildren(e);
                e.Children.Clear();
                foreach (var entity in entitiesToRemove)
                {
                    entity.IsDestroyed = true;
                    entity.Enabled = false;
                    entity.ForceParent(DestroyedRoot);

                    for (var i = 0; i < entity.Components.Count; i++)
                    {
                        var c = entity.Components[i];
                        c.Enabled = false;
                        c.TryInvokeMethod("OnRemove");
                        c.UnregisterMethods();
                        var attr =
                            (LimitInstanceCountAttribute?)Attribute.GetCustomAttribute(c.GetType(),
                                typeof(LimitInstanceCountAttribute));
                        if (attr != null)
                        {
                            Func<Type, int> add = type => 0;
                            Func<Type, int, int> update = (type, amount) => Interlocked.Add(ref amount, -1);
                            InstanceCount.AddOrUpdate(c.GetType(), add, update);
                            // We have to remove the component from the instance count limit
                        }
                    }

                    entity.Components.Clear();
                }
            }
        }
    }

    internal static void Update()
    {
        if (!HasStarted) return;

        UpdateECS();
        lock (_frameDelays)
        {
            for (var i = _frameDelays.Count - 1; i >= 0; i--)
            {
                var task = _frameDelays[i];
                if (task.Item1 <= 0f)
                {
                    try
                    {
                        task.Item2.Invoke();
                    }
                    catch (Exception e)
                    {
                        Debug.Error("Update task failed : " + e.Message);
                    }

                    _frameDelays.RemoveAt(i);
                }
                else
                {
                    _frameDelays[i] = (task.Item1 - (float)Time.deltaTime, task.Item2);
                }
            }
        }

        // Run through each of our Update Actions
        lock (_updateActions)
        {
            foreach (var action in _updateActions)
                try
                {
                    action.Invoke();
                }
                catch (Exception e)
                {
                    Debug.Error("Update task failed : " + e.Message);
                }
        }

        lock (_updateMethods)
        {
            foreach (var pair in _updateMethods)
            {
                if (!pair.Key.Enabled || !pair.Key.Entity!.Enabled || pair.Key.IsNew) continue;
                try
                {
                    pair.Value.Invoke();
                }
                catch (Exception e)
                {
                    Debug.Error("Error while performing Update(): " + e);
                }
            }
        }
    }

    internal static void Tick(string name)
    {
        if (!HasStarted) HasStarted = true;

        if (name == "Main" || TickManager.ThreadIsSynced(name))
            UpdateECS();
        lock (_tickMethods)
        {
            if (name == "Main")
            {
                // Run through each of our tick tasks
                lock (_tickTasks)
                {
                    for (var i = _tickTasks.Count - 1; i >= 0; i--)
                    {
                        var task = _tickTasks[i];
                        if (task.Item1 <= 0)
                        {
                            try
                            {
                                task.Item2.Invoke();
                            }
                            catch (Exception e)
                            {
                                Debug.Error("Tick task failed : " + e.Message);
                            }

                            _tickTasks.RemoveAt(i);
                        }
                        else
                        {
                            _tickTasks[i] = (task.Item1 - 1, task.Item2);
                        }
                    }
                }

                // Run through each of our Tick Actions
                lock (_tickActions)
                {
                    foreach (var action in _tickActions)
                        try
                        {
                            action.Invoke();
                        }
                        catch (Exception e)
                        {
                            Debug.Error("Tick task failed : " + e.Message);
                        }
                }
            }

            ConcurrentDictionary<Component, Action> methods;
            if (_tickMethods.TryGetValue(name, out methods))
                foreach (var pair in methods)
                {
                    if (!pair.Key.Enabled || !pair.Key.Entity!.Enabled) continue;
                    if (pair.Key.IsNew)
                    {
                        pair.Key.Enabled = true; // This is done so that OnEnable() is called
                        pair.Key.TryInvokeMethod("Start");
                        pair.Key.IsNew = false;
                    }

                    try
                    {
#if DEBUG
                        if (name == "Main")
                            Profiler.StartTimer(Profiler.TickType.Tick);
#endif

                        pair.Value.Invoke();
#if DEBUG
                        if (name == "Main")
                            Profiler.EndTimer(Profiler.TickType.Tick, pair.Key.GetType().FullName);
#endif
                    }
                    catch (Exception e)
                    {
                        Debug.Error($"Error while performing Tick() (Thread: {name}): {e}");
                    }
                }
        }
    }

    public static void ScheduleTickTaskIn(int ticks, Action task)
    {
        lock (_tickTasks)
        {
            _tickTasks.Add((ticks, task));
        }
    }

    public static void ScheduleFrameTaskAfter(float time, Action task)
    {
        lock (_frameDelays)
        {
            _frameDelays.Add((time, task));
        }
    }

    public static void Dispose()
    {
        foreach (var e in RootEntity.Children)
            if (e != null)
                e.Destroy();

        // Force an update (so that OnRemove & OnDisable are called)
        UpdateECS();
        TickManager.Dispose();
        _tickTasks.Clear();
        _tickMethods.Clear();
        _updateMethods.Clear();
        _dirtyEntities.Clear();
        _startMethods.Clear();

        _tickActions.Clear();
        _updateActions.Clear();
    }

    public static void PrintHierarchy()
    {
        PrintEntity(RootEntity, 0);
    }

    public static ECSElement GetECSHierarchy()
    {
        return GetEntityECSElement(RootEntity);
    }

    private static ECSElement GetEntityECSElement(Entity e)
    {
        if (e == null) return ElementFromEntity("NULL ENTITY", new Component[0], new ECSElement[0]);

        var children = new List<ECSElement>();
        for (var i = 0; i < e.Children.Count; i++) children.Add(GetEntityECSElement(e.Children[i]));

        var components = new List<Component>();
        for (var i = 0; i < e.Components.Count; i++) components.Add(e.Components[i]);

        return ElementFromEntity(e.Name, components.ToArray(), children.ToArray());
    }

    private static void PrintEntity(Entity e, int layer)
    {
        var children = e.Children.Count;
        Console.WriteLine(
            string.Concat(Enumerable.Repeat("   ", layer)) + e.Name + " - (" + children + " children)");
        for (var i = 0; i < children; i++) PrintEntity(e.Children[i], layer + 1);
    }

    internal static ECSElement ElementFromEntity(string name, Component[] components, ECSElement[] children)
    {
        var ecsComponents = new List<ECSComponent>();
        for (var i = 0; i < components.Length; i++) ecsComponents.Add(new ECSComponent(components[i]));

        return new ECSElement(name, ecsComponents.ToArray(), children);
    }

    public class ECSException : Exception
    {
        public ECSException(string message) : base(message)
        {
        }
    }
}