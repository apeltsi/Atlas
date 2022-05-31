namespace SolidCode.Caerus.ECS
{
    using System.Numerics;
    using SolidCode.Caerus.Components;
    using SolidCode.Caerus.Rendering;

    public class Entity
    {

        public string name = "Entity";
        public bool enabled = true;
        public List<Entity> children = new List<Entity>();
        public Entity? parent;
        public List<Component> components = new List<Component>();
        public List<RenderComponent> renderingComponents = new List<RenderComponent>();
        public Entity(string name, Vector2? position = null, Vector2? scale = null)
        {
            this.children = new List<Entity>();
            this.components = new List<Component>();
            this.name = name;
            Vector2 pos = Vector2.Zero;
            if (position != null)
            {
                pos = (Vector2)position;
            }
            Vector2 sca = Vector2.One;
            if (scale != null)
            {
                sca = (Vector2)scale;
            }
            AddComponent<Transform>();
        }

        public void SetParent(Entity e)
        {
            if (parent != null)
            {
                parent.children.Remove(this);
            }
            parent = e;
            parent.children.Add(this);
        }

        public void AddComponent<T>() where T : Component, new()
        {
            Component component = new T();
            component.entity = this;

            components.Add(component);
            if (typeof(RenderComponent).IsAssignableFrom(typeof(T)))
            {
                renderingComponents.Add((RenderComponent)component);
            }
        }
        public void RemoveComponent(Component component)
        {
            component.OnRemove();

            components.Remove(component);
            if (typeof(RenderComponent).IsAssignableFrom(component.GetType()))
            {
                renderingComponents.Remove((RenderComponent)component);
            }
        }
        public T? GetComponent<T>() where T : Component
        {
            foreach (Component c in components)
            {
                if (typeof(T) == c.GetType())
                {
                    return (T)c;
                }
            }
            return default(T);
        }
        public void Start()
        {
            foreach (Component component in components)
            {
                if (component.enabled)
                    component.Start();
            }
        }
        public void Update()
        {
            foreach (Component component in components)
            {
                if (component.enabled)
                    component.Update();
            }
        }

        public void FixedUpdate()
        {
            foreach (Component component in components)
            {
                if (component.enabled)
                    component.FixedUpdate();
            }
        }
        public List<Drawable> RenderStart()
        {
            List<Drawable> drawables = new List<Drawable>();
            foreach (RenderComponent component in renderingComponents)
            {
                Debug.Log("starting render");
                if (component.enabled)
                    drawables.AddRange(component.StartRender(Window._graphicsDevice));
            }

            return drawables;
        }

    }
}
