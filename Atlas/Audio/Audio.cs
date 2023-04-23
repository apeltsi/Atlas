namespace SolidCode.Atlas.Audio
{
    using System.Diagnostics;
    using OpenTK.Audio.OpenAL;
    
    public struct PlaybackSettings
    {
        public float? Volume;
        public float? Pitch;
        public PlaybackSettings(float volume)
        {
            this.Volume = volume;
            this.Pitch = 1f;
        }
        public PlaybackSettings(float volume, float pitch)
        {
            this.Volume = volume;
            this.Pitch = pitch;
        }

        internal void SetDefaults()
        {
            if (Volume == null) Volume = 1f;
            if (Pitch == null) Pitch = 1f;
        }

        public override string ToString()
        {
            return $"Volume: {Volume}, Pitch {Pitch}";
        }
    }
    
    public static class Audio
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

        /// <summary>
        /// Plays the given track once, with the given volume
        /// </summary>
        /// <param name="track">The track to play</param>
        /// <param name="volume">The volume</param>
        /// <returns>The currently playing audio as <c>PlayingAudio</c> if successful</returns>
        public static PlayingAudio? Play(AudioTrack track, float volume)
        {
            return Play(track, new PlaybackSettings(volume));
        }
        /// <summary>
        /// Plays the given track once, with <c>PlaybackSettings</c> if provided.
        /// </summary>
        /// <param name="track">The track to play</param>
        /// <param name="settings">The settings </param>
        /// <returns>The currently playing audio as <c>PlayingAudio</c> if successful</returns>
        public static PlayingAudio? Play(AudioTrack track, PlaybackSettings settings = new PlaybackSettings())
        {
            if (track == null) return null;
            settings.SetDefaults();
            lock (AudioLock)
            {
                int source = AL.GenSource();
                AL.Source(source, ALSourcei.Buffer, track.Buffer);
                AL.Source(source, ALSourcef.Gain, settings.Volume!.Value);
                AL.Source(source, ALSourcef.Pitch, settings.Pitch!.Value);
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