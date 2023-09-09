namespace SolidCode.Atlas.Audio
{
    using System.Diagnostics;
    using Silk.NET.OpenAL;
    
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
        internal static ALContext ALC;
        internal static AL ALApi;
        internal static object AudioLock = new Object();
        private static bool _isDisposed = false;
        private static List<uint> _sources = new List<uint>();
        private static unsafe Context* _context;
        private static unsafe Device* _device;
        public static float MasterVolume
        {
            get
            {
                
                float vol = 0f;
                ALApi.GetListenerProperty(ListenerFloat.Gain, out vol);
                
                return vol;
            }
            set
            {
                ALApi.SetListenerProperty(ListenerFloat.Gain, value);
            }
        }
        internal static void InitializeAudio()
        {
            try
            {
                _isDisposed = false;
                lock (AudioLock)
                {
                    ALC = ALContext.GetApi();
                    ALApi = AL.GetApi();
                    unsafe
                    {
                        _device = ALC.OpenDevice("");
                        if (_device == null)
                        {
                            Console.WriteLine("Could not create device");
                            return;
                        }


                        _context = ALC.CreateContext(_device, null);
                        ALC.MakeContextCurrent(_context);
                        ALApi.GetError();
                    }
                }

                Telescope.Debug.Log(LogCategory.Framework, "AudioManager initialized");
            }
            catch (Exception e)
            {
                Telescope.Debug.Log(LogCategory.Framework, "Error while initializing AudioManager: " + e.ToString());
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
            if (!Atlas.AudioEnabled)
                return new PlayingAudio(track, 0, settings.Pitch!.Value);

            lock (AudioLock)
            {
                if (_isDisposed)
                    return null!;
                uint source = ALApi.GenSource();
                ALApi.SetSourceProperty(source, SourceInteger.Buffer, track.Buffer);
                ALApi.SetSourceProperty(source, SourceFloat.Gain, settings.Volume!.Value);
                ALApi.SetSourceProperty(source, SourceFloat.Pitch, settings.Pitch!.Value);
                ALApi.SourcePlay(source);
                _sources.Add(source);
                PlayingAudio p = new PlayingAudio(track, source, settings.Pitch!.Value);
                RemoveSource(source, (float)track.Duration);
                return p;
            }
        }
        static async void RemoveSource(uint source, float wait)
        {
            await Task.Delay((int)(wait * 1000) + 50);
            lock (AudioLock)
            {
                if (!_isDisposed)
                {
                    _sources.Remove(source);
                    ALApi.DeleteSource(source);
                }
            }
        }

        internal static void DisposeAllSources()
        {
            lock (AudioLock)
            {
                _isDisposed = true;
                foreach (uint source in _sources)
                {
                    ALApi.DeleteSource(source);
                }
                SolidCode.Atlas.Telescope.Debug.Log(LogCategory.Framework, "All audio sources disposed");
            }
        }
        
        internal static void Dispose()
        {
            lock (AudioLock)
            {
                _isDisposed = true;
                unsafe
                {
                    ALC.DestroyContext(_context);
                    ALC.CloseDevice(_device);
                }
                ALApi.Dispose();
                ALC.Dispose();
                SolidCode.Atlas.Telescope.Debug.Log(LogCategory.Framework, "AudioManager disposed");
            }
        }

        public class PlayingAudio
        {
            private Stopwatch stopwatch;
            private uint source;
            public double Duration { get; protected set; }
            public double TimePlayed => stopwatch.Elapsed.TotalSeconds;
            internal PlayingAudio(AudioTrack t, uint source, float pitch)
            {
                this.Duration = t.Duration / pitch;
                this.stopwatch = Stopwatch.StartNew();
                this.source = source;
            }

            public void Stop()
            {
                if(Atlas.AudioEnabled)
                    ALApi.SourceStop(this.source);
            }
        }
    }
}