
namespace SolidCode.Caerus
{
    using System.Timers;
    using SolidCode.Caerus.Rendering;
    using SolidCode.Caerus.ECS;

    public enum LogCategories
    {
        General,
        Framework,
        Rendering,
        ECS
    }

    class Caerus
    {
        public static string? DataDirectory = Path.Join(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "data" + Path.DirectorySeparatorChar);
        public static string? ShaderDirectory = Path.Join(DataDirectory, "shaders" + Path.DirectorySeparatorChar);

        public const string Version = "0.1.0a";

        public static int updateFrequency = 50;
        public static Timer timer;
        static EntityComponentSystem ecs;
        public static void Main()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            Debug.StartLogs("General", "Framework", "Rendering", "ECS");
            Debug.Log(LogCategories.Framework, "Coeus " + Version + " starting up...");
            ecs = new EntityComponentSystem();
            Entity e = new Entity("Hello World");
            e.AddComponent<DefaultComponent>();
            e.AddComponent<DebugRenderer>();

            ecs.AddEntity(e);

            Debug.Log(LogCategories.Framework, "Opening window");

            Debug.Log(LogCategories.Framework, "Core framework functionalities started within " + watch.ElapsedMilliseconds + "ms");

            Window w = new Window();
            ecs.window = w;
            ecs.Start();
            StartFixedUpdateLoop(ecs);
            Debug.Log(LogCategories.Rendering, "Starting renderloop");
            try
            {
                w.StartRenderLoop();

            }
            catch (Exception ex)
            {
                Debug.Error(ex.ToString());
            }
            watch.Stop();
            Debug.Log(LogCategories.Framework, "Caerus shutting down after " + (Math.Round(watch.ElapsedMilliseconds / 100f) / 10) + "s...");
        }

        public static void StartFixedUpdateLoop(EntityComponentSystem ecs)
        {
            Debug.Log(LogCategories.Framework, "Starting fixed update loop");
            timer = new System.Timers.Timer(1000 / updateFrequency);
            timer.Elapsed += new System.Timers.ElapsedEventHandler(FixedUpdate);
            timer.AutoReset = true;
            timer.Start();
        }

        public static void FixedUpdate(object sender, ElapsedEventArgs e)
        {
            if (ecs != null)
                ecs.FixedUpdate();
        }
    }
}