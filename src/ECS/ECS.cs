﻿using System.Collections.Concurrent;
using System.Reflection;
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

        public class ECSElement
        {
            public string name { get; set; }
            public ECSComponent[] components { get; set; }

            public ECSElement[] children { get; set; }

            public ECSElement(string name, Component[] components, ECSElement[] children)
            {
                this.name = name;
                List<ECSComponent> ecscomponents = new List<ECSComponent>();
                for (int i = 0; i < components.Length; i++)
                {
                    ecscomponents.Add(new ECSComponent(components[i]));
                }
                this.components = ecscomponents.ToArray();
                this.children = children;
            }
        }

        public class ECSComponent
        {
            public string name { get; set; }
            public ECSComponentField[] fields { get; set; }

            public ECSComponent(Component c)
            {
                this.name = c.GetType().Name;
                List<ECSComponentField> fields = new List<ECSComponentField>();
                for (int i = 0; i < c.GetType().GetFields().Length; i++)
                {
                    FieldInfo field = c.GetType().GetFields()[i];
                    if (Attribute.IsDefined(field, typeof(HideInInspector)))
                    {
                        continue;
                    }
                    object? fieldValue = field.GetValue(c);

                    if (fieldValue == null || fieldValue.ToString() == null)
                    {
                        fields.Add(new ECSComponentField(field.Name, "Null", field.FieldType.ToString()));
                    }
                    else
                    {
                        string? fieldValueStr = fieldValue.ToString();
                        if (fieldValueStr != null)
                        {
                            fields.Add(new ECSComponentField(field.Name, fieldValueStr, field.FieldType.ToString()));
                        }
                        else
                        {
                            fields.Add(new ECSComponentField(field.Name, "Null", field.FieldType.ToString()));
                        }
                    }
                }
                this.fields = fields.ToArray();
            }
        }

        public class ECSComponentField
        {
            public string name { get; set; }
            public string value { get; set; }
            public string type { get; set; }

            public ECSComponentField(string name, string value, string type)
            {
                this.name = name;
                this.value = value;
                this.type = type;
            }
        }

        public static ECSElement GetECSHierarchy()
        {
            return GetEntityECSElement(RootEntity);
        }

        static ECSElement GetEntityECSElement(Entity e)
        {
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
            return new ECSElement(e.name, components.ToArray(), children.ToArray());
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
