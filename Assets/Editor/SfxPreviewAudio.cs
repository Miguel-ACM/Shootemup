using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Editor audio preview avoids scene objects, listeners and entering Play mode.
public static class SfxPreviewAudio
{
    public sealed class Pcm
    {
        public float[] samples;
        public int channels, rate;
        public float Seconds => samples.Length / (float)(channels * rate);
    }
    static AudioClip playing;
    static Pcm unscaled;
    static float[] scaled;
    static readonly Type Utility = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
    static readonly MethodInfo PlayMethod = Utility?.GetMethod("PlayPreviewClip", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
        null, new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
    static readonly MethodInfo StopMethod = Utility?.GetMethod("StopAllPreviewClips", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
    public static bool Available => PlayMethod != null && StopMethod != null;

    public static Pcm FromClip(AudioClip clip)
    {
        if (clip == null) throw new IOException("No sound is assigned to this event yet.");
        if (clip.length > 60) throw new IOException("Use an SFX clip shorter than one minute.");
        clip.LoadAudioData();
        var data = new float[clip.samples * clip.channels];
        if (!clip.GetData(data, 0)) throw new IOException("This clip cannot be previewed. Set its import Load Type to Decompress On Load.");
        return new Pcm { samples = data, channels = clip.channels, rate = clip.frequency };
    }

    public static Pcm ReadWave(byte[] bytes)
    {
        using (var reader = new BinaryReader(new MemoryStream(bytes)))
        {
            if (bytes.Length < 44 || new string(reader.ReadChars(4)) != "RIFF") throw new IOException("Expected a RIFF WAV file.");
            reader.ReadUInt32();
            if (new string(reader.ReadChars(4)) != "WAVE") throw new IOException("Expected WAVE audio.");
            int format = 0, channels = 0, rate = 0, bits = 0, start = 0, length = 0;
            while (reader.BaseStream.Position + 8 <= bytes.Length)
            {
                string id = new string(reader.ReadChars(4));
                uint size = reader.ReadUInt32();
                long next = reader.BaseStream.Position + size;
                if (next > bytes.Length) throw new IOException("Truncated WAV chunk.");
                if (id == "fmt " && size >= 16)
                {
                    format = reader.ReadUInt16(); channels = reader.ReadUInt16(); rate = reader.ReadInt32();
                    reader.ReadUInt32(); reader.ReadUInt16(); bits = reader.ReadUInt16();
                    if (format == 65534 && size >= 40)
                    {
                        reader.ReadUInt16(); reader.ReadUInt16(); reader.ReadUInt32();
                        format = reader.ReadUInt16(); // Extensible PCM / IEEE float sub-format GUID.
                    }
                }
                else if (id == "data") { start = (int)reader.BaseStream.Position; length = (int)size; }
                reader.BaseStream.Position = next + (size & 1);
            }
            if ((format != 1 && format != 3) || channels < 1 || channels > 8 || rate < 8000 || rate > 192000 ||
                !(bits == 8 || bits == 16 || bits == 24 || bits == 32) || (format == 3 && bits != 32) ||
                length == 0 || length % (channels * (bits / 8)) != 0 || length / (double)(channels * (bits / 8) * rate) > 60)
                throw new IOException("Unsupported WAV format (use PCM or float, up to 60 seconds).");
            var data = new float[length / (bits / 8)];
            reader.BaseStream.Position = start;
            for (int i = 0; i < data.Length; i++)
            {
                float value;
                if (format == 3) value = reader.ReadSingle();
                else if (bits == 8) value = (reader.ReadByte() - 128) / 128f;
                else if (bits == 16) value = reader.ReadInt16() / 32768f;
                else if (bits == 24)
                {
                    int sample = reader.ReadByte() | reader.ReadByte() << 8 | reader.ReadByte() << 16;
                    value = (sample << 8 >> 8) / 8388608f;
                }
                else value = reader.ReadInt32() / 2147483648f;
                if (!float.IsFinite(value)) throw new IOException("Audio contains invalid sample values.");
                data[i] = value;
            }
            return new Pcm { samples = data, channels = channels, rate = rate };
        }
    }

    public static Pcm Render(Pcm source, float gain, float pitch, bool repeat, bool loop, float rate)
    {
        int sourceFrames = source.samples.Length / source.channels;
        int frames = Mathf.Max(1, Mathf.CeilToInt(sourceFrames / Mathf.Clamp(pitch, .1f, 3)));
        int total = repeat || loop ? Mathf.CeilToInt(6 * source.rate) + (loop ? 0 : frames) : frames;
        var result = new float[total * source.channels];
        int interval = loop ? frames : Mathf.Max(1, Mathf.RoundToInt(source.rate / Mathf.Clamp(rate, 1, 20)));
        int end = repeat || loop ? source.rate * 6 : 1;
        for (int offset = 0; offset < end; offset += interval)
        {
            for (int f = 0; f < frames && offset + f < total; f++)
            {
                float position = Mathf.Min(f * pitch, sourceFrames - 1);
                int a = (int)position, b = Math.Min(a + 1, sourceFrames - 1);
                for (int c = 0; c < source.channels; c++)
                    result[(offset + f) * source.channels + c] += Mathf.Lerp(source.samples[a * source.channels + c], source.samples[b * source.channels + c], position - a) * gain;
            }
            if (!repeat && !loop) break;
        }
        return new Pcm { samples = result, channels = source.channels, rate = source.rate };
    }

    public static float Peak(Pcm pcm)
    {
        float peak = 0;
        foreach (float value in pcm.samples) peak = Mathf.Max(peak, Mathf.Abs(value));
        return peak;
    }
    public static void Play(Pcm pcm, float gain = 1)
    {
        if (!Available) throw new NotSupportedException("Unity's audio preview API is unavailable in this editor version.");
        Stop();
        playing = AudioClip.Create("SFX Preview", pcm.samples.Length / pcm.channels, pcm.channels, pcm.rate, false);
        playing.hideFlags = HideFlags.HideAndDontSave;
        unscaled = pcm;
        scaled = new float[pcm.samples.Length];
        SetGain(gain);
        PlayMethod.Invoke(null, new object[] { playing, 0, false });
        // Unity's editor preview reinitializes a generated clip when it starts playback.
        // Refill that native buffer after starting; later gain edits update the same buffer.
        playing.SetData(scaled, 0);
    }
    public static void SetGain(float gain)
    {
        if (playing == null || unscaled == null) return;
        for (int i = 0; i < scaled.Length; i++) scaled[i] = unscaled.samples[i] * gain;
        playing.SetData(scaled, 0);
    }
    public static void Stop()
    {
        if (playing == null) return;
        StopMethod?.Invoke(null, null);
        UnityEngine.Object.DestroyImmediate(playing);
        playing = null;
        unscaled = null;
        scaled = null;
    }
}
