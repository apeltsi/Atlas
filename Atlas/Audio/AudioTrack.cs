using NokitaKaze.WAVParser;
using Silk.NET.OpenAL;
using SolidCode.Atlas.AssetManagement;
using SolidCode.Atlas.Telescope;
namespace SolidCode.Atlas.Audio
{
    public class AudioTrack : Asset
    {
        public AudioTrack()
        {

        }

        public uint Buffer { get; protected set; }
        public double Duration { get; protected set; }

        private bool _isDisposed = false;
        public override void Dispose()
        {
            if (!_isDisposed)
            {
                Audio.ALApi.DeleteBuffer(this.Buffer);
                _isDisposed = true;
            }
        }

        public override void FromStreams(Stream[] stream, string name)
        {
            WAVParser parser = new WAVParser(stream[0]);
            SetAudioData(parser);
            this.IsValid = true;
        }

        private void SetAudioData(WAVParser parser)
        {
            Duration = parser.Duration.TotalSeconds;
            BufferFormat format = BufferFormat.Mono16;
            short[] samples;
            if (parser.ChannelCount == 2)
            {
                samples = new short[parser.SamplesCount * 2];
                format = BufferFormat.Stereo16;
                for (int i = 0; i < samples.Length; i += 2)
                {
                    // Lets convert the double samples to proper short samples
                    samples[i] = (short) (parser.Samples[0][i / 2] * (double) short.MaxValue);
                    samples[i + 1] = (short) (parser.Samples[1][i / 2] * (double) short.MaxValue);
                }
            }
            else
            {
                samples = new short[parser.SamplesCount];
                for (int i = 0; i < samples.Length; i++)
                {
                    samples[i] = (short) (parser.Samples[0][i] * (double) short.MaxValue);
                }
            }
            lock (Audio.AudioLock)
            {
                this.Buffer = Audio.ALApi.GenBuffer();
                Audio.ALApi.BufferData<short>(Buffer, format, samples, (int)parser.SampleRate);
            }
        }

        public override void Load(string path, string name)
        {
            try
            {
                WAVParser parser = new WAVParser(Path.Join(Atlas.AssetsDirectory, path + ".wav"));
                SetAudioData(parser);
                this.IsValid = true;
            }
            catch (Exception e)
            {
                SolidCode.Atlas.Debug.Error(LogCategory.Framework, "Error parsing audio: " + e.ToString());
            }
        }

        
        ~AudioTrack()
        {
            this.Dispose();
        }
    }
}