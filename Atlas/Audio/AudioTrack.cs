using NokitaKaze.WAVParser;
using OpenTK.Audio.OpenAL;
using SolidCode.Atlas.AssetManagement;
using SolidCode.Atlas.Telescope;
namespace SolidCode.Atlas.Audio
{
    public class AudioTrack : Asset
    {
        public AudioTrack()
        {
            Debug.Log(LogCategory.Framework, "Generating Buffer");

        }

        public int Buffer { get; protected set; }
        public double Duration { get; protected set; }

        public override void Dispose()
        {
            AL.DeleteBuffer(this.Buffer);
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
            ALFormat format = ALFormat.MonoDoubleExt;
            double[] samples;
            if (parser.ChannelCount == 2)
            {
                samples = new double[parser.SamplesCount * 2];
                format = ALFormat.StereoDoubleExt;
                for (int i = 0; i < samples.Length; i += 2)
                {
                    samples[i] = parser.Samples[0][i / 2];
                    samples[i + 1] = parser.Samples[1][i / 2];
                }
            }
            else
            {
                samples = parser.Samples[0].ToArray();
            }
            lock (AudioManager.AudioLock)
            {
                this.Buffer = AL.GenBuffer();
                AL.BufferData<double>(Buffer, format, samples, (int)parser.SampleRate);
            }
        }

        public override void Load(string path, string name)
        {
            try
            {
                WAVParser parser = new WAVParser(Path.Join(Atlas.AssetsDirectory, path));
                SetAudioData(parser);
                this.IsValid = true;
            }
            catch (Exception e)
            {
                Debug.Error("Error parsing audio: " + e.ToString());
            }
        }

        internal static class WavHelper
        {
            internal static bool readWav(byte[] _bytes, out float[] L, out float[] R)
            {
                L = R = null;

                try
                {
                    using (MemoryStream ms = new MemoryStream(_bytes))
                    {
                        BinaryReader reader = new BinaryReader(ms);

                        // chunk 0
                        int chunkID = reader.ReadInt32();
                        int fileSize = reader.ReadInt32();
                        int riffType = reader.ReadInt32();


                        // chunk 1
                        int fmtID = reader.ReadInt32();
                        int fmtSize = reader.ReadInt32(); // bytes for this chunk (expect 16 or 18)

                        // 16 bytes coming...
                        int fmtCode = reader.ReadInt16();
                        int channels = reader.ReadInt16();
                        int sampleRate = reader.ReadInt32();
                        int byteRate = reader.ReadInt32();
                        int fmtBlockAlign = reader.ReadInt16();
                        int bitDepth = reader.ReadInt16();

                        if (fmtSize == 18)
                        {
                            // Read any extra values
                            int fmtExtraSize = reader.ReadInt16();
                            reader.ReadBytes(fmtExtraSize);
                        }

                        // chunk 2
                        int dataID = reader.ReadInt32();
                        int bytes = reader.ReadInt32();

                        // DATA!
                        byte[] byteArray = reader.ReadBytes(bytes);

                        int bytesForSamp = bitDepth / 8;
                        int nValues = bytes / bytesForSamp;


                        float[] asFloat = null;

                        switch (bitDepth)
                        {
                            case 64:
                                double[]
                                    asDouble = new double[nValues];
                                System.Buffer.BlockCopy(byteArray, 0, asDouble, 0, bytes);
                                asFloat = Array.ConvertAll(asDouble, e => (float)e);
                                break;
                            case 32:
                                asFloat = new float[nValues];
                                System.Buffer.BlockCopy(byteArray, 0, asFloat, 0, bytes);
                                break;
                            case 16:
                                Int16[]
                                    asInt16 = new Int16[nValues];
                                System.Buffer.BlockCopy(byteArray, 0, asInt16, 0, bytes);
                                asFloat = Array.ConvertAll(asInt16, e => e / (float)(Int16.MaxValue + 1));
                                break;
                            case 8:
                                byte[]
                                    asBytes = new byte[nValues];
                                System.Buffer.BlockCopy(byteArray, 0, asBytes, 0, bytes);
                                asFloat = Array.ConvertAll(asBytes, e => (e / (float)(sbyte.MaxValue + 1)) - 1f);
                                break;
                            default:
                                return false;
                        }
                        switch (channels)
                        {
                            case 1:
                                L = asFloat;
                                R = null;
                                return true;
                            case 2:
                                // de-interleave
                                int nSamps = nValues / 2;
                                L = new float[nSamps];
                                R = new float[nSamps];
                                for (int s = 0, v = 0; s < nSamps; s++)
                                {
                                    L[s] = asFloat[v++];
                                    R[s] = asFloat[v++];
                                }
                                return true;
                            default:
                                return false;
                        }
                    }
                }
                catch
                {
                    Debug.Log("Failed to load");
                    return false;
                }

                return false;
            }
        }
    }
}