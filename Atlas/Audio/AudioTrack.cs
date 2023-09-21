using NokitaKaze.WAVParser;
using Silk.NET.OpenAL;
using SolidCode.Atlas.AssetManagement;

namespace SolidCode.Atlas.Audio;

public class AudioTrack : Asset
{
    private bool _isDisposed;

    public uint Buffer { get; protected set; }
    public double Duration { get; protected set; }

    public override void Dispose()
    {
        if (!_isDisposed && Atlas.AudioEnabled)
        {
            Audio.ALApi.DeleteBuffer(Buffer);
            _isDisposed = true;
        }
    }

    public override void FromStreams(Stream[] stream, string name)
    {
        var parser = new WAVParser(stream[0]);
        SetAudioData(parser);
        IsValid = true;
    }

    private void SetAudioData(WAVParser parser)
    {
        Duration = parser.Duration.TotalSeconds;
        var format = BufferFormat.Mono16;
        short[] samples;
        if (parser.ChannelCount == 2)
        {
            samples = new short[parser.SamplesCount * 2];
            format = BufferFormat.Stereo16;
            for (var i = 0; i < samples.Length; i += 2)
            {
                // Lets convert the double samples to proper short samples
                samples[i] = (short)(parser.Samples[0][i / 2] * short.MaxValue);
                samples[i + 1] = (short)(parser.Samples[1][i / 2] * short.MaxValue);
            }
        }
        else
        {
            samples = new short[parser.SamplesCount];
            for (var i = 0; i < samples.Length; i++) samples[i] = (short)(parser.Samples[0][i] * short.MaxValue);
        }

        if (!Atlas.AudioEnabled)
            return;
        lock (Audio.AudioLock)
        {
            Buffer = Audio.ALApi.GenBuffer();
            Audio.ALApi.BufferData(Buffer, format, samples, (int)parser.SampleRate);
        }
    }

    public override void Load(string path, string name)
    {
        try
        {
            var parser = new WAVParser(Path.Join(Atlas.AssetsDirectory, "assets", path + ".wav"));
            SetAudioData(parser);
            IsValid = true;
        }
        catch (Exception e)
        {
            Debug.Error(LogCategory.Framework, "Error parsing audio: " + e);
        }
    }


    ~AudioTrack()
    {
        Dispose();
    }
}