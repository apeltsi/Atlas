namespace SolidCode.Atlas.Audio
{
    using System.Diagnostics;
    using OpenTK.Audio.OpenAL;
    using SolidCode.Atlas.Telescope;
    public static class AudioManager
    {
        internal static ALContext context;
        static ALDevice device;
        internal static object AudioLock = new Object();
        public static float MasterVolume
        {
            get
            {
                float vol = 0f;
                AL.GetListener(ALListenerf.Gain, out vol);
                if (!AL.IsExtensionPresent("AL_EXT_double"))
                {
                    SolidCode.Atlas.Telescope.Debug.Warning(LogCategory.Framework, "OpenAL Extension AL_EXT_double is not present.");
                }
                return vol;
            }
            set
            {
                AL.Listener(ALListenerf.Gain, value);
            }
        }
        internal static void InitializeAudio()
        {
            lock (AudioLock)
            {
                List<string> devices = ALC.GetString(AlcGetStringList.AllDevicesSpecifier);
                SolidCode.Atlas.Telescope.Debug.Log(LogCategory.Framework, "Available devices: ");
                for (int i = 0; i < devices.Count; i++)
                {
                    SolidCode.Atlas.Telescope.Debug.Log(LogCategory.Framework, " - " + devices[i]);
                }
                unsafe
                {
                    device = ALC.OpenDevice(null);
                    context = ALC.CreateContext(device, (int*)null);
                    bool valid = ALC.MakeContextCurrent(context);
                    if (!valid)
                    {
                        SolidCode.Atlas.Telescope.Debug.Error(LogCategory.Framework, "Failed to create OpenAL context!");
                    }
                    else
                    {
                        SolidCode.Atlas.Telescope.Debug.Log(LogCategory.Framework, "AudioManager active on " + ALC.GetString(device, AlcGetString.AllDevicesSpecifier) + "");
                    }
                }
            }
        }

        public static PlayingAudio? PlayOneShot(AudioTrack track, float volume = 1f)
        {
            if (track == null) return null;
            lock (AudioLock)
            {
                int source = AL.GenSource();
                AL.Source(source, ALSourcei.Buffer, track.Buffer);
                AL.Source(source, ALSourcef.Gain, volume);
                AL.SourcePlay(source);
                PlayingAudio p = new PlayingAudio(track, source);
                RemoveSource(source, (float)track.Duration);
                return p;
            }
        }
        static async void RemoveSource(int source, float wait)
        {
            await Task.Delay((int)(wait * 1000) + 50);
            AL.DeleteSource(source);
        }

        internal static void Dispose()
        {
            lock (AudioLock)
            {

                unsafe
                {
                    ALC.MakeContextCurrent(ALContext.Null);
                    ALC.DestroyContext(context);
                    ALC.CloseDevice(device);
                }
                SolidCode.Atlas.Telescope.Debug.Log(LogCategory.Framework, "AudioManager disposed");
            }
        }

        public class PlayingAudio
        {
            private Stopwatch stopwatch;
            private int source;
            public double Duration { get; protected set; }
            internal PlayingAudio(AudioTrack t, int source)
            {
                this.Duration = t.Duration;
                this.stopwatch = Stopwatch.StartNew();
                this.source = source;
            }

            public void Stop()
            {
                AL.SourceStop(this.source);
            }
        }
    }
}