#if DEBUG
using System.Collections.Concurrent;
using System.Diagnostics;

namespace SolidCode.Atlas.Telescope;

public static class Profiler
{
    private static readonly ConcurrentDictionary<TickType, Dictionary<string, float>> curTimes = new();
    private static readonly ConcurrentDictionary<TickType, Dictionary<string, float>> allTimes = new();
    private static readonly ConcurrentDictionary<TickType, int> frames = new();
    private static readonly ConcurrentDictionary<TickType, Stopwatch> watches = new();

    public static void StartTimer(TickType tickType)
    {
        if (DebugServer.Connections == 0) return;
        try
        {
            if (!watches.ContainsKey(tickType)) watches.TryAdd(tickType, new Stopwatch());

            watches[tickType].Restart();
        }
        catch (Exception e)
        {
            // ignored
        }
    }

    public static void EndTimer(TickType tickType, string p)
    {
        if (DebugServer.Connections == 0 || !watches.ContainsKey(tickType)) return;
        try
        {
            var sw = watches[tickType];
            sw.Stop();
            if (!curTimes.ContainsKey(tickType)) curTimes.TryAdd(tickType, new Dictionary<string, float>());

            if (!curTimes[tickType].ContainsKey(p))
                curTimes[tickType][p] = (float)sw.Elapsed.TotalMilliseconds;
            else
                curTimes[tickType][p] += (float)sw.Elapsed.TotalMilliseconds;
        }
        catch (Exception e)
        {
            // ignored
        }
    }

    public static void SubmitTimes(TickType tickType)
    {
        if (DebugServer.Connections == 0) return;
        try
        {
            if (!allTimes.ContainsKey(tickType)) allTimes[tickType] = new Dictionary<string, float>();

            if (!curTimes.ContainsKey(tickType)) return;


            foreach (var time in curTimes[tickType])
            {
                if (!allTimes.ContainsKey(tickType)) break;
                if (!allTimes[tickType].ContainsKey(time.Key))
                    allTimes[tickType][time.Key] = time.Value;
                else
                    allTimes[tickType][time.Key] += time.Value;
            }

            curTimes[tickType].Clear();
            ;

            frames.TryAdd(tickType, 0);
            frames[tickType]++;
        }
        catch (Exception e)
        {
            // ignored
        }
    }

    public static Dictionary<string, Dictionary<string, float>> GetAverageTimes()
    {
        try
        {
            Dictionary<string, Dictionary<string, float>> allAverages = new();
            foreach (var tickTimes in allTimes)
            {
                Dictionary<string, float> avgTimes = new();
                foreach (var times in tickTimes.Value) avgTimes[times.Key] = times.Value / frames[tickTimes.Key];

                allAverages[tickTimes.Key.ToString()] = avgTimes;
            }

            allTimes.Clear();
            frames.Clear();
            return allAverages;
        }
        catch (Exception e)
        {
            // ignored
            return new Dictionary<string, Dictionary<string, float>>();
        }
    }

    public class TickType
    {
        public static readonly TickType Update = new("Update");
        public static readonly TickType Tick = new("Tick");

        private TickType(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public override string ToString()
        {
            return Value;
        }
    }
}

#endif