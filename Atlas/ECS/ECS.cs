﻿using System.Collections.Concurrent;
using System.Reflection;
using SolidCode.Atlas.Rendering;
using SolidCode.Atlas.Standard;
using SolidCode.Atlas.Telescope;
namespace SolidCode.Atlas.ECS
{
    public static class EntityComponentSystem
    {
        /// <summary>
        /// The root entity, every entity in the ECS is a child or grandchild of this entity.
        /// </summary>
        public static readonly Entity RootEntity = new Entity("ROOT", false);
        ///<summary>
        /// The root entity of all destroyed entities.<para />
        /// Note that under normal usage this entity should never have any children. As it is only used as a parent reference for destroyed entities.<para />
        /// If an entity is referencing DESTROYED_ROOT as its parent, it should be collected by the garbage collector. If this doesn't happen, it might be a sign of a memory leak.
        ///</summary>
        public static readonly Entity DestroyedRoot = new Entity("DESTROYED_ROOT", false);


        public static Window? window;
        private static ConcurrentQueue<Entity> _removeQueue = new ();
        private static ConcurrentQueue<Entity> _addQueue = new ();


        private static List<(int, Action)> _tickTasks = new ();
        private static List<(float, Action)> _frameDelays = new ();

        private static ConcurrentDictionary<Component, Action> _updateMethods = new ();
        private static ConcurrentDictionary<Component, Action> _tickMethods = new ();
        private static ConcurrentQueue<Action> _dirtyEntities = new ();
        private static ConcurrentBag<Component> _startMethods = new ();
        private static List<Action> _updateActions = new();
        private static List<Action> _tickActions = new();
           
        public static ConcurrentDictionary<Type, int> InstanceCount = new ();

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
        internal static void RegisterComponentTickMethod(Component c, Action method)
        {
            _tickMethods.TryAdd(c, method);
        }
        internal static void UnregisterComponentUpdateMethod(Component c)
        {
            _updateMethods.Remove(c, out _);
        }
        internal static void UnregisterComponentTickMethod(Component c)
        {
            _tickMethods.Remove(c, out _);
        }
        /// <summary>
        /// Registers a action to be invoked at every Tick
        /// </summary>
        /// <param name="action">The action to be invoked</param>
        public static void RegisterTickAction(Action action)
        {
            lock(_tickActions)
                _tickActions.Add(action);
        }
        /// <summary>
        /// Registers a action to be invoked at every Frame. (Before rendering)
        /// </summary>
        /// <param name="action">The action to be invoked</param>
        public static void RegisterUpdateAction(Action action)
        {
            lock(_updateActions)
                _updateActions.Add(action);
        }
        
        /// <summary>
        /// Unregisters an action from being invoked at every Tick
        /// </summary>
        /// <param name="action"></param>
        public static void UnregisterTickAction(Action action)
        {
            lock(_tickActions)
                _tickActions.Remove(action);
        }
        
        /// <summary>
        /// Unregisters an action from being invoked at every Frame. 
        /// </summary>
        /// <param name="action"></param>
        public static void UnregisterUpdateAction(Action action)
        {
            lock(_updateActions)
                _updateActions.Remove(action);
        }


        static void UpdateECS()
        {
            lock (_dirtyEntities)
            {
                while (_dirtyEntities.Count > 0)
                {
                    Action? m;
                    _dirtyEntities.TryDequeue(out m);
                    if (m != null)
                    {
                        m.Invoke();
                    }
                }
            }
            lock (_startMethods)
            {
                foreach (Component c in _startMethods)
                {
                    c.enabled = true; // This is done so that OnEnable() is called
                    c.TryInvokeMethod("Start");
                    c.isNew = false;
                }
                _startMethods.Clear();

            }

            while (_removeQueue.Count > 0)
            {
                Entity? e;
                _removeQueue.TryDequeue(out e);
                if (e != null)
                {
                    List<Entity> entitiesToRemove = new List<Entity>();
                    entitiesToRemove.AddRange(e.GetAllChildrenRecursively());

                    entitiesToRemove.Add(e);
                    e.parent.RemoveChildren(e);
                    e.children.Clear();
                    foreach (Entity entity in entitiesToRemove)
                    {
                        entity.enabled = false;
                        entity.ForceParent(DestroyedRoot);

                        for (int i = 0; i < entity.components.Count; i++)
                        {
                            Component c = entity.components[i];
                            c.enabled = false;
                            c.entity = null;
                            c.TryInvokeMethod("OnRemove");
                            c.UnregisterMethods();
                            LimitInstanceCountAttribute? attr = (LimitInstanceCountAttribute?)Attribute.GetCustomAttribute(c.GetType(), typeof(LimitInstanceCountAttribute));
                            if (attr != null)
                            {
                                Func<Type, int> add = type => 0;
                                Func<Type, int, int> update = (type, amount) => Interlocked.Add(ref amount, -1);
                                EntityComponentSystem.InstanceCount.AddOrUpdate(c.GetType(), add, update);
                                // We have to remove the component from the instance count limit
                            }
                        }
                        entity.components.Clear();

                    }
                }
            }
        }

        public static void Update()
        {
            if (!HasStarted)
            {
                return;
            }
            UpdateECS();
            lock(_frameDelays)
                for (int i = _frameDelays.Count - 1; i >= 0; i--)
                {
                    (float, Action) task = _frameDelays[i];
                    if (task.Item1 <= 0f)
                    {
                        task.Item2.Invoke();
                        _frameDelays.RemoveAt(i);
                    }
                    else
                    {
                        _frameDelays[i] = (task.Item1 - (float)Time.deltaTime, task.Item2);
                    }
                }
            // Run through each of our Update Actions
            lock(_updateActions)
                foreach (var action in _updateActions)
                {
                    action.Invoke();
                }

            lock (_updateMethods)
            {
                foreach (KeyValuePair<Component, Action> pair in _updateMethods)
                {
                    if (!pair.Key.enabled || !pair.Key.entity!.enabled || pair.Key.isNew) continue;
                    try
                    {
                        pair.Value.Invoke();
                    }
                    catch (Exception e)
                    {
                        Debug.Error("Error while performing Update(): " + e.ToString());
                    }
                }
            }
        }

        public static void Tick()
        {
            if (!HasStarted)
            {
                HasStarted = true;
            }
            UpdateECS();
            lock (_tickMethods)
            {
                // Run through each of our tick tasks
                lock(_tickTasks)
                    for (int i = _tickTasks.Count - 1; i >= 0; i--)
                    {
                        (int, Action) task = _tickTasks[i];
                        if (task.Item1 <= 0)
                        {
                            task.Item2.Invoke();
                            _tickTasks.RemoveAt(i);
                        }
                        else
                        {
                            _tickTasks[i] = (task.Item1 - 1, task.Item2);
                        }
                    }
                // Run through each of our Tick Actions
                lock(_tickActions)
                    foreach (var action in _tickActions)
                    {
                        action.Invoke();
                    }
                foreach (KeyValuePair<Component, Action> pair in _tickMethods)
                {
                    if (!pair.Key.enabled || !pair.Key.entity!.enabled) continue;
                    if (pair.Key.isNew)
                    {
                        pair.Key.enabled = true; // This is done so that OnEnable() is called
                        pair.Key.TryInvokeMethod("Start");
                        pair.Key.isNew = false;
                    }
                    try
                    {
#if DEBUG
                        Profiler.StartTimer(Profiler.TickType.Tick);
#endif

                        pair.Value.Invoke();
#if DEBUG
                        Profiler.EndTimer(Profiler.TickType.Tick, pair.Key.GetType().FullName);
#endif

                    }
                    catch (Exception e)
                    {
                        Debug.Error("Error while performing Tick(): " + e.ToString());
                    }
                }
            }
        }

        public static void ScheduleTickTaskIn(int ticks, Action task)
        {
            lock(_tickTasks)
                _tickTasks.Add((ticks, task));
        }

        public static void ScheduleFrameTaskAfter(float time, Action task)
        {
            lock(_frameDelays)
                _frameDelays.Add((time, task));
        }

        public static void Dispose()
        {
            foreach (Entity e in RootEntity.children)
            {
                if (e != null)
                {
                    e.Destroy();
                }
            }
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

        static ECSElement GetEntityECSElement(Entity e)
        {
            if (e == null)
            {
                return ElementFromEntity("NULL ENTITY", new Component[0], new ECSElement[0]);
            }
            List<ECSElement> children = new List<ECSElement>();
            for (int i = 0; i < e.children.Count; i++)
            {
                children.Add(GetEntityECSElement(e.children[i]));
            }
            List<Component> components = new List<Component>();
            for (int i = 0; i < e.components.Count; i++)
            {
                components.Add(e.components[i]);
            }
            return ElementFromEntity(e.name, components.ToArray(), children.ToArray());
        }

        static void PrintEntity(Entity e, int layer)
        {
            int children = e.children.Count;
            Console.WriteLine(String.Concat(Enumerable.Repeat("   ", layer)) + e.name + " - (" + children + " children)");
            for (int i = 0; i < children; i++)
            {
                PrintEntity(e.children[i], layer + 1);
            }
        }

        internal static ECSElement ElementFromEntity(string name, Component[] components, ECSElement[] children)
        {
            List<ECSComponent> ecsComponents = new List<ECSComponent>();
            for (int i = 0; i < components.Length; i++)
            {
                ecsComponents.Add(new ECSComponent(components[i]));
            }
            return new ECSElement(name, ecsComponents.ToArray(), children);
        }
    }
}
