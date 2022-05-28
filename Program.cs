using SolidCode.Caerus.ECS;
namespace SolidCode.Caerus
{
    class Caerus
    {
        public const int updateFrequency = 50;
        public static void Main()
        {
            Debug.Log("Coeus starting up...");
            EntityComponentSystem ecs = new EntityComponentSystem();
            Entity e = new Entity("Hello World");
            e.AddComponent(new DefaultComponent());
            ecs.AddEntity(e);
            ecs.Start();
            StartFixedUpdateLoop(ecs);
        }

        public static void StartFixedUpdateLoop(EntityComponentSystem ecs)
        {
            Timer t = new System.Threading.Timer(o =>
            {
                ecs.FixedUpdate();

            }, null, 1000 / updateFrequency, 1000 / updateFrequency);
        }


    }
}