namespace SolidCode.Atlas
{

    public static class TickScheduler
    {
        private static bool _disableScheduling = false;
        public static bool DisableScheduling 
        {
            get => _disableScheduling;
            set {
                Debug.Warning("Disabling the TickScheduler will likely lead to stability issues");
                _disableScheduling = value;
            }
        }
        private static object runningLock = new Object();
        private static bool isRunning = false;
        public static PriorityQueue<Task, int> tickQueue = new PriorityQueue<Task, int>();
        /// <summary>
        /// Returns a Task which will complete when no synced (normal) ECS threads are executing
        /// </summary>
        /// <param name="priority"></param>
        /// <returns></returns>
        public static Task RequestTick(int priority = 0)
        {
            Task t = new Task(TaskAction);
            lock (runningLock)
            {
                if (isRunning && !DisableScheduling)
                {
                    tickQueue.Enqueue(t, priority);
                }
                else
                {
                    isRunning = true;
                    t.Start();
                }
            }
            return t;
        }

        private static void TaskAction()
        {

        }

        /// <summary>
        /// Tells the TickScheduler that the current thread is done executing, allowing the next thread in the queue to run
        /// </summary>
        public static void FreeThreads()
        {
            lock (runningLock)
            {
                isRunning = false;
                RunNextInQueue();
            }
        }

        private static void RunNextInQueue()
        {
            if (tickQueue.Count > 0)
            {
                isRunning = true;
                tickQueue.Dequeue().Start();
            }
        }
    }
}