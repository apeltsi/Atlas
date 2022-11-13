using System.Collections.Concurrent;
using SolidCode.Atlas.Rendering;

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
        public static ConcurrentDictionary<Type, int> InstanceCount = new ConcurrentDictionary<Type, int>();

        public static bool HasStarted { get; set; }


        public static void RemoveEntity(Entity entity)
        {
            removeQueue.Enqueue(entity);
        }

        static void UpdateECS()
        {
            while (removeQueue.Count > 0)
            {
                Entity? e;
                removeQueue.TryDequeue(out e);
                if (e != null)
                {
                    List<Entity> entitiesToRemove = new List<Entity>();
                    for (int i = 0; i < e.children.Count; i++)
                    {
                        entitiesToRemove.Add(e.children[i]);
                    }
                    entitiesToRemove.Add(e);
                    e.parent.RemoveChildren(e);
                    e.children.Clear();
                    foreach (Entity entity in entitiesToRemove)
                    {
                        entity.enabled = false;
                        entity.parent = DestroyedRoot;
                        for (int i = 0; i < entity.components.Count; i++)
                        {
                            Component c = entity.components[i];
                            c.enabled = false;
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
                        entity.components.Clear();

                    }
                }
            }
        }

        public static void Update()
        {

            UpdateECS();
            RootEntity.Update();
        }

        public static void FixedUpdate()
        {
            if (!HasStarted)
            {
                HasStarted = true;
            }
            UpdateECS();
            RootEntity.FixedUpdate();
        }

        public static void Dispose()
        {
            foreach (Entity e in RootEntity.children)
            {
                e.Destroy();
            }
        }

        public static void PrintHierarchy()
        {
            PrintEntity(RootEntity, 0);
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

    }
}
