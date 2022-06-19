using SolidCode.Caerus.Rendering;

namespace SolidCode.Caerus.ECS
{
    public class EntityComponentSystem
    {
        List<Entity> rootEntities = new List<Entity>();
        public Window? window;

        public void AddEntity(Entity entity)
        {
            // FIXME(amos): Adding of entities should only happen when all threads are idle. This is done to avoid a race-condition
            rootEntities.Add(entity);
        }

        public void RemoveEntity(Entity entity)
        {
            // FIXME(amos): Removing of entities should only happen when all threads are idle. This is done to avoid a race-condition
            rootEntities.Remove(entity);
        }

        public void Start()
        {
            foreach (Entity e in rootEntities)
            {
                if (e.enabled)
                    e.Start();
            }
            List<Drawable> drawables = new List<Drawable>();
            try
            {
                foreach (Entity e in rootEntities)
                {
                    if (e.enabled)
                        drawables.AddRange(e.RenderStart());
                }
            }
            catch (Exception e)
            {
                Debug.Error(LogCategories.ECS, e.ToString());
            }
            if (window == null)
            {
                throw new NullReferenceException("ECS > No window is assigned! Cannot perform StartRender()");
            }
            window.AddDrawables(drawables);
        }
        public void Update()
        {
            foreach (Entity e in rootEntities)
            {
                if (e.enabled)
                    e.Update();
            }
        }

        public void FixedUpdate()
        {
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
