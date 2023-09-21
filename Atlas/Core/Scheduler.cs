namespace SolidCode.Atlas;

public static class TickScheduler
{
    private static bool _disableScheduling;
    private static readonly object _runningLock = new();
    private static bool _isRunning;
    private static Thread? _currentLocker;

    private static int
        _currentLocks; // Incremented when a thread tries to schedule multiple times when already having been allotted a tick

    private static readonly PriorityQueue<(Task, Thread), int> _tickQueue = new();

    /// <summary>
    /// NOT RECOMMENDED: Disables the TickScheduler, allowing all threads to run at once. This will likely lead to
    /// stability issues.
    /// </summary>
    public static bool DisableScheduling
    {
        get => _disableScheduling;
        set
        {
            Debug.Warning("Disabling the TickScheduler will likely lead to stability issues");
            _disableScheduling = value;
        }
    }

    /// <summary>
    /// Checks if the current thread already has a lock on synced tick execution.
    /// </summary>
    public static bool HasTick()
    {
        lock (_runningLock)
        {
            if (_disableScheduling)
                return true;
            if (_isRunning && _currentLocker != null &&
                Thread.CurrentThread.ManagedThreadId == _currentLocker.ManagedThreadId) return true;
            return false;
        }
    }

    /// <summary>
    /// Returns a Task which will complete when no synced (normal) ECS threads are executing
    /// </summary>
    /// <param name="priority">Priority of the task</param>
    /// <returns>A task that will complete once the previous tick task has completed.</returns>
    public static Task RequestTick(int priority = 0)
    {
        var t = new Task(TaskAction);
        lock (_runningLock)
        {
            if (_isRunning && !DisableScheduling)
            {
                if (_currentLocker != null && Thread.CurrentThread.ManagedThreadId == _currentLocker.ManagedThreadId)
                {
                    _currentLocks++;
                    t.Start();
                    return t;
                }

                _tickQueue.Enqueue((t, Thread.CurrentThread), priority);
            }
            else
            {
                _isRunning = true;
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
        lock (_runningLock)
        {
            if (_currentLocks > 0) _currentLocks--;
            _isRunning = false;
            RunNextInQueue();
        }
    }

    private static void RunNextInQueue()
    {
        if (_tickQueue.Count > 0)
        {
            _isRunning = true;
            var next = _tickQueue.Dequeue();
            _currentLocker = next.Item2;
            next.Item1.Start();
        }
        else
        {
            _currentLocker = null;
        }
    }
}