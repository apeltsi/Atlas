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
        /// Note that under normal usage this entity should have NO CHILDREN as entities are only assigned as the child of this entity briefly before getting destroyed.<para />
        /// If this entity contains children, it might be a sign that there is a memory leak somewhere. 
        ///</summary>
        public static readonly Entity DestroyedRoot = new Entity("DESTROYED_ROOT", false);

        public static Window? window;
        private static ConcurrentQueue<Entity> removeQueue = new ConcurrentQueue<Entity>();
        private static ConcurrentQueue<Entity> addQueue = new ConcurrentQueue<Entity>();
        public static ConcurrentDictionary<Type, int> InstanceCount = new ConcurrentDictionary<Type, int>();

        public static bool HasStarted { get; set; }

        public static void AddEntity(Entity entity)
        {
            addQueue.Enqueue(entity);
        }

        public static void RemoveEntity(Entity entity)
        {
            removeQueue.Enqueue(entity);
        }

        static void UpdateECS()
        {
            while (addQueue.Count > 0)
            {
                Entity? e;
                addQueue.TryDequeue(out e);
                if (e != null)
                {
                    RootEntity.AddChildren(e);
                    e.Start();
                }
            }
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
                    foreach (Entity entity in entitiesToRemove)
                    {
                        entity.parent.RemoveChildren(entity);
                        for (int i = 0; i < entity.components.Count; i++)
                        {
                            entity.components[i].OnDisable();
                        }

                    }
                }
            }
        }

        public static void Start()
        {
            UpdateECS();
            if (window == null)
            {
                throw new NullReferenceException("ECS > No window is assigned! Cannot perform StartRender()");
            }
            HasStarted = true;
        }
        public static void Update()
        {

            UpdateECS();
            RootEntity.Update();
        }

        public static void FixedUpdate()
        {
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
            Console.WriteLine(String.Concat(Enumerable.Repeat("   ", layer)) + e.name + " - (" + e.children.Count + " children)");
            for (int i = 0; i < e.children.Count; i++)
            {
                PrintEntity(e.children[i], layer + 1);
            }
        }

    }
}
