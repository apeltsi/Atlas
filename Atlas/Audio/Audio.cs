using System.Diagnostics;
using Silk.NET.OpenAL;

namespace SolidCode.Atlas.Audio;

/// <summary>
/// Settings for how the audio should be played
/// </summary>
public struct PlaybackSettings
{
    /// <summary>
    /// Volume of the track
    /// </summary>
    public float? Volume;

    /// <summary>
    /// The pitch of the track
    /// </summary>
    public float? Pitch;

    /// <summary>
    /// Creates a new playback settings with the given volume
    /// </summary>
    /// <param name="volume"></param>
    public PlaybackSettings(float volume)
    {
        Volume = volume;
        Pitch = 1f;
    }

    /// <summary>
    /// Creates a new playback settings with the given volume and pitch
    /// </summary>
    /// <param name="volume">The volume of the track</param>
    /// <param name="pitch">The pitch of the track</param>
    public PlaybackSettings(float volume, float pitch)
    {
        Volume = volume;
        Pitch = pitch;
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

/// <summary>
/// The audio system.
/// </summary>
public static class Audio
{
    internal static ALContext ALC;
    internal static AL ALApi;
    internal static object AudioLock = new();
    private static bool _isDisposed;
    private static readonly List<uint> _sources = new();
    private static unsafe Context* _context;
    private static unsafe Device* _device;

    public static float MasterVolume
    {
        get
        {
            var vol = 0f;
            ALApi.GetListenerProperty(ListenerFloat.Gain, out vol);

            return vol;
        }
        set => ALApi.SetListenerProperty(ListenerFloat.Gain, value);
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
            Telescope.Debug.Log(LogCategory.Framework, "Error while initializing AudioManager: " + e);
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
    /// <param name="settings">The settings</param>
    /// <returns>The currently playing audio as <c>PlayingAudio</c> if successful</returns>
    public static PlayingAudio? Play(AudioTrack track, PlaybackSettings settings = new())
    {
        if (track == null) return null;
        settings.SetDefaults();
        if (!Atlas.AudioEnabled)
            return new PlayingAudio(track, 0, settings.Pitch!.Value);

        lock (AudioLock)
        {
            if (_isDisposed)
                return null!;
            var source = ALApi.GenSource();
            ALApi.SetSourceProperty(source, SourceInteger.Buffer, track.Buffer);
            ALApi.SetSourceProperty(source, SourceFloat.Gain, settings.Volume!.Value);
            ALApi.SetSourceProperty(source, SourceFloat.Pitch, settings.Pitch!.Value);
            ALApi.SourcePlay(source);
            _sources.Add(source);
            var p = new PlayingAudio(track, source, settings.Pitch!.Value);
            RemoveSource(source, (float)track.Duration);
            return p;
        }
    }

    private static async void RemoveSource(uint source, float wait)
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
            foreach (var source in _sources) ALApi.DeleteSource(source);
            Telescope.Debug.Log(LogCategory.Framework, "All audio sources disposed");
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
            Telescope.Debug.Log(LogCategory.Framework, "AudioManager disposed");
        }
    }

    public class PlayingAudio
    {
        private readonly uint source;
        private readonly Stopwatch stopwatch;

        internal PlayingAudio(AudioTrack t, uint source, float pitch)
        {
            Duration = t.Duration / pitch;
            stopwatch = Stopwatch.StartNew();
            this.source = source;
        }

        public double Duration { get; protected set; }
        public double TimePlayed => stopwatch.Elapsed.TotalSeconds;

        public void Stop()
        {
            if (Atlas.AudioEnabled)
                ALApi.SourceStop(source);
        }
    }
}