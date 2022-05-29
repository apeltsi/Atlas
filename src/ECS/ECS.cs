namespace SolidCode.Caerus.ECS
{
    class EntityComponentSystem
    {
        List<Entity> entities = new List<Entity>();

        public EntityComponentSystem()
        {
        }

        public void AddEntity(Entity entity)
        {
            entities.Add(entity);
        }

        public void RemoveEntity(Entity entity)
        {
            entities.Remove(entity);
        }

        public void Start()
        {
            foreach (Entity e in entities)
            {
                if (e.enabled)
                    e.Start();
            }
        }
        public void Update()
        {
            foreach (Entity e in entities)
            {
                if (e.enabled)
                    e.Update();
            }
        }

        public void FixedUpdate()
        {
            foreach (Entity e in entities)
            {
                if (e.enabled)
                    e.FixedUpdate();
            }
        }

    }
}
