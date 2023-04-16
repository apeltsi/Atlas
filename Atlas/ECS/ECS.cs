using System.Collections.Concurrent;
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
        private static ConcurrentQueue<Entity> removeQueue = new ConcurrentQueue<Entity>();
        private static ConcurrentQueue<Entity> addQueue = new ConcurrentQueue<Entity>();
        internal delegate void ComponentMethod();

        private static ConcurrentDictionary<Component, ComponentMethod> UpdateMethods = new ConcurrentDictionary<Component, ComponentMethod>();
        private static ConcurrentDictionary<Component, ComponentMethod> TickMethods = new ConcurrentDictionary<Component, ComponentMethod>();
        private static ConcurrentQueue<ComponentMethod> DirtyEntities = new ConcurrentQueue<ComponentMethod>();
        private static ConcurrentBag<Component> StartMethods = new ConcurrentBag<Component>();

        public static ConcurrentDictionary<Type, int> InstanceCount = new ConcurrentDictionary<Type, int>();

        public static bool HasStarted { get; set; }


        public static void RemoveEntity(Entity entity)
        {
            removeQueue.Enqueue(entity);
        }
        static internal void AddDirtyEntity(ComponentMethod method)
        {
            DirtyEntities.Enqueue(method);
        }

        static internal void AddStartMethod(Component c)
        {
            StartMethods.Add(c);
        }

        static internal void RegisterUpdateMethod(Component c, ComponentMethod method)
        {
            UpdateMethods.TryAdd(c, method);
        }
        static internal void RegisterTickMethod(Component c, ComponentMethod method)
        {
            TickMethods.TryAdd(c, method);
        }
        static internal void UnregisterUpdateMethod(Component c)
        {
            UpdateMethods.Remove(c, out _);
        }
        static internal void UnregisterTickMethod(Component c)
        {
            TickMethods.Remove(c, out _);
        }



        static void UpdateECS()
        {
            lock (DirtyEntities)
            {
                while (DirtyEntities.Count > 0)
                {
                    ComponentMethod? m;
                    DirtyEntities.TryDequeue(out m);
                    if (m != null)
                    {
                        m.Invoke();
                    }
                }
            }
            lock (StartMethods)
            {
                foreach (Component c in StartMethods)
                {
                    c.enabled = true; // This is done so that OnEnable() is called
                    c.TryInvokeMethod("Start");
                    c.isNew = false;
                }
                StartMethods.Clear();

            }

            while (removeQueue.Count > 0)
            {
                Entity? e;
                removeQueue.TryDequeue(out e);
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
            lock (UpdateMethods)
            {
                foreach (KeyValuePair<Component, ComponentMethod> pair in UpdateMethods)
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
            lock (TickMethods)
            {
                foreach (KeyValuePair<Component, ComponentMethod> pair in TickMethods)
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
                        pair.Value.Invoke();
                    }
                    catch (Exception e)
                    {
                        Debug.Error("Error while performing Tick(): " + e.ToString());
                    }
                }
            }
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
            List<ECSComponent> ecscomponents = new List<ECSComponent>();
            for (int i = 0; i < components.Length; i++)
            {
                ecscomponents.Add(new ECSComponent(components[i]));
            }
            return new ECSElement(name, ecscomponents.ToArray(), children);
        }
    }
}
