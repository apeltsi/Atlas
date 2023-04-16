#if DEBUG
using System.Diagnostics;

namespace SolidCode.Atlas.Telescope;
public static class Profiler
{
    public enum FrameTimeType
    {
        Waiting,
        Scripting,
        PreRender,
        Rendering
    }
    private static float[] curTimes = new float[4];
    private static float[] allTimes = new float[4] { 0, 0, 0, 0 };
    private static int frames = 0;
    private static Stopwatch watch = new Stopwatch();
    private static FrameTimeType curType;
    private static float[] cachedTimes = new float[3];

    public static void StartTimer(FrameTimeType p)
    {
        watch.Reset();
        watch.Start();
        curType = p;
    }
    public static void EndTimer()
    {
        watch.Stop();
        curTimes[(int)curType] = watch.ElapsedMilliseconds;
    }

    public static void SubmitTimes()
    {
        for (int i = 0; i < curTimes.Length; i++)
        {
            allTimes[i] += curTimes[i];
        }
        frames++;
    }

    public static float[] GetAverageTimes()
    {
        if (frames < 30)
        {
            return cachedTimes;
        }
        else
        {
            float[] avgTimes = new float[4];
            for (int i = 0; i < allTimes.Length; i++)
            {
                avgTimes[i] = allTimes[i] / frames;
            }
            cachedTimes = avgTimes;
            frames = 0;
            allTimes = new float[4];
            return avgTimes;
        }
    }
}

#endif