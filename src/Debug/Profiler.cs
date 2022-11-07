#if DEBUG
using System.Diagnostics;

namespace SolidCode.Atlas
{
    public static class Profiler
    {
        public enum FrameTimeType
        {
            Scripting,
            PreRender,
            Rendering
        }
        private static float[] curTimes = new float[3];
        private static float[] allTimes = new float[3] { 0, 0, 0 };
        private static int frames = 0;
        private static Stopwatch watch = new Stopwatch();
        private static FrameTimeType curType;
        private static float[] cachedTimes = new float[3];

        public static void StartTimer(FrameTimeType p)
        {
            watch.Reset();
            watch.Start();
            curType = p;

            /*HttpKeepAlivePingPolicy this is async code, i would like to express this as async 
            ooooooooooooooo
            "hej dehä e min note som ja skrev nu hit på denhä coola keyboarden som du ha byggt fö den va noo aika cool yeyeyeyyeyeyee, o jae tyvärr hardstuck bronze i valorant )):::: men de e ok
            ja rankaa ännu upp en daa"
            "nu gjodde ja en ny note fö ja kände fö de o de e så satisfying att skriva på denhä ja sku liksom ilomielin gö skolarbete på denhä, honestly me denhä o en odentli gamingchair sku ja va radiant"
            "holah9olahola 12345678 hehehehhehe de e så rolit o skriva på denhä hihihihiihi de går ba lite långsamt men de e helt okej o ljude e nice!!!!!"
            "ja visste oxå hu man gö kommentaree i kodning ::::DDDD lol vitsi de e störande att man int kan gö en sånhän :D i discord fö de rättaa de o de e jobbit ))):<"
            "ja fösökte fixa de men de funka int ugh"*/
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
                float[] avgTimes = new float[3];
                for (int i = 0; i < allTimes.Length; i++)
                {
                    avgTimes[i] = allTimes[i] / frames;
                }
                cachedTimes = avgTimes;
                frames = 0;
                allTimes = new float[3];
                return avgTimes;
            }
        }
    }
}
#endif