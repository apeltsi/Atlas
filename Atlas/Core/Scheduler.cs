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
        private static Thread? _currentLocker = null;
        public static PriorityQueue<(Task, Thread), int> tickQueue = new PriorityQueue<(Task, Thread), int>();

        /// <summary>
        /// Checks if the current thread already has a lock on synced tick execution. 
        /// </summary>
        public static bool HasTick()
        {
            lock (runningLock)
            {
                if (_disableScheduling)
                {
                    return true;
                }
                else if (isRunning && _currentLocker != null && Thread.CurrentThread.ManagedThreadId == _currentLocker.ManagedThreadId)
                {
                    return true;
                }
                return false;
            }
        }
        
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
                    if (_currentLocker != null && Thread.CurrentThread.ManagedThreadId == _currentLocker.ManagedThreadId)
                    {
                        Debug.Error("Thread '" + (_currentLocker.Name ?? _currentLocker.ManagedThreadId.ToString()) + "' Tried to lock a thread from itself!");
                    }
                    tickQueue.Enqueue((t, Thread.CurrentThread), priority);
                }
                else
                {
                    isRunning = true;
                    t.Start();
                    _currentLocker = Thread.CurrentThread;
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
                var next = tickQueue.Dequeue();
                _currentLocker = next.Item2;
                next.Item1.Start();
            }
            else
            {
                _currentLocker = null;
            }
        }
    }
}