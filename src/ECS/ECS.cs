using SolidCode.Caerus.Rendering;

namespace SolidCode.Caerus.ECS
{
    public class EntityComponentSystem
    {
        List<Entity> rootEntities = new List<Entity>();
        public Window? window;
        List<Entity> removeList = new List<Entity>();
        List<Entity> addList = new List<Entity>();


        public bool HasStarted { get; protected set; }
        public EntityComponentSystem()
        {
            HasStarted = false;
        }

        public void AddEntity(Entity entity)
        {
            addList.Add(entity);
        }

        public void RemoveEntity(Entity entity)
        {
            removeList.Add(entity);
            for (int i = 0; i < entity.children.Count; i++)
            {
                RemoveEntity(entity.children[i]);
            }
        }

        void UpdateECS()
        {
            List<Entity> a = new List<Entity>(addList);
            foreach (Entity e in a)
            {
                rootEntities.Add(e);
                addList.Remove(e);
                e.Start();
            }
            List<Entity> r = new List<Entity>(removeList);
            removeList.Clear();
            foreach (Entity e in r)
            {
                rootEntities.Remove(e);
                removeList.Remove(e);
                for (int i = 0; i < e.components.Count; i++)
                {
                    e.components[i].OnDisable();
                }
            }
        }

        public void Start()
        {
            UpdateECS();
            if (window == null)
            {
                throw new NullReferenceException("ECS > No window is assigned! Cannot perform StartRender()");
            }
            HasStarted = true;
        }
        public void Update()
        {
            UpdateECS();
            foreach (Entity e in rootEntities)
            {
                if (e.enabled)
                    e.Update();
            }
        }

        public void FixedUpdate()
        {
            UpdateECS();
            foreach (Entity e in rootEntities)
            {
                if (e.enabled)
                    e.FixedUpdate();
            }
        }

        public void Dispose()
        {
            foreach (Entity e in rootEntities)
            {
                e.Destroy();
            }
            rootEntities.Clear();
        }

    }
}
