#if DEBUG
using System.Diagnostics;

namespace SolidCode.Atlas.Telescope;
public static class Profiler
{
 
    

    public class TickType
    {
        private TickType(string value) { Value = value; }
        public string Value { get; private set; }
        public static readonly TickType Update = new TickType("Update");
        public static readonly TickType Tick = new TickType("Tick");

        public override string ToString()
        {
            return Value;
        }
    }

    private static Dictionary<TickType, Dictionary<string, float>> curTimes = new();
    private static Dictionary<TickType, Dictionary<string, float>> allTimes = new();
    private static Dictionary<TickType,int> frames = new ();
    private static Dictionary<TickType, Stopwatch> watches = new();

    public static void StartTimer(TickType tickType)
    {
        if(DebugServer.Connections == 0) return;
        if (!watches.ContainsKey(tickType))
        {
            watches.Add(tickType, new Stopwatch());
        }
        watches[tickType].Restart();
    }
    public static void EndTimer(TickType tickType, string p)
    {
        if(DebugServer.Connections == 0 || !watches.ContainsKey(tickType)) return;
        Stopwatch sw = watches[tickType];
        sw.Stop();
        if (!curTimes.ContainsKey(tickType))
        {
            curTimes.Add(tickType, new ());
        }

        if (!curTimes[tickType].ContainsKey(p))
        {
            curTimes[tickType][p] = (float)sw.Elapsed.TotalMilliseconds;
        }
        else
        {
            curTimes[tickType][p] += (float)sw.Elapsed.TotalMilliseconds;
        }
    }

    public static void SubmitTimes(TickType tickType)
    {
        if(DebugServer.Connections == 0) return;
        if (!allTimes.ContainsKey(tickType))
        {
            allTimes[tickType] = new Dictionary<string, float>();
        }

        if (!curTimes.ContainsKey(tickType)) return;
        
        
        foreach (var time in curTimes[tickType])
        {
            if(!allTimes[tickType].ContainsKey(time.Key))
                allTimes[tickType][time.Key] = time.Value;
            else
                allTimes[tickType][time.Key] += time.Value;
        }

        curTimes = new();

        frames.TryAdd(tickType, 0);
        frames[tickType]++;
    }

    public static Dictionary<string, Dictionary<string, float>> GetAverageTimes()
    {
        Dictionary<string, Dictionary<string, float>> allAverages = new();
        foreach (var tickTimes in allTimes)
        {
            Dictionary<string, float> avgTimes = new();
            foreach (var times in tickTimes.Value)
            {
                avgTimes[times.Key] = times.Value / frames[tickTimes.Key];
            }

            allAverages[tickTimes.Key.ToString()] = avgTimes;
        }

        allTimes.Clear();
        frames.Clear();
        return allAverages;
    }
}

#endif