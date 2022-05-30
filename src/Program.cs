using SolidCode.Caerus.ECS;

namespace SolidCode.Caerus
{
    using System.Timers;
    using SolidCode.Caerus.Rendering;

    class Caerus
    {
        public const string Version = "0.1.0a";

        public static int updateFrequency = 50;
        public static Timer? timer;
        static EntityComponentSystem? ecs;
        public static void Main()
        {
            Debug.StartLogs();
            Debug.Log("Coeus " + Version + " starting up...");
            ecs = new EntityComponentSystem();
            Entity e = new Entity("Hello World");
            e.AddComponent<DefaultComponent>();

            ecs.AddEntity(e);
            ecs.Start();
            StartFixedUpdateLoop(ecs);

            Debug.Log("Opening window");
            new Window();
        }

        public static void StartFixedUpdateLoop(EntityComponentSystem ecs)
        {
            Debug.Log("Starting fixed update loop");
            timer = new System.Timers.Timer(1000 / updateFrequency);
            timer.Elapsed += new System.Timers.ElapsedEventHandler(FixedUpdate);
            timer.AutoReset = true;
            timer.Start();
        }

        public static void FixedUpdate(object? sender, ElapsedEventArgs e)
        {
            if (ecs != null)
                ecs.FixedUpdate();
        }
    }
}