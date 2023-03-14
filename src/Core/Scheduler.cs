namespace SolidCode.Atlas
{

    public static class TickScheduler
    {
        private static object runningLock = new Object();
        private static bool isRunning = false;
        public static PriorityQueue<Task, int> tickQueue = new PriorityQueue<Task, int>();
        public static void RequestTick(Task t, int priority = 0)
        {

            lock (runningLock)
            {
                if (isRunning)
                {
                    tickQueue.Enqueue(t, priority);
                }
                else
                {
                    isRunning = true;
                    t.Start();
                }
            }
        }

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