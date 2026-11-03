using System;
using UnityEngine;

// Place the mono one-shot directly in the deck's output buffer. The audio clock keeps
// the hits on their measured beats even when a gameplay frame runs late.
public sealed class BossKick
{
    readonly float[] pcm;
    readonly int rate;

    BossKick(float[] pcm, int rate)
    {
        this.pcm = pcm;
        this.rate = rate;
    }

    public static BossKick Load()
    {
        var clip = Resources.Load<AudioClip>("Sounds/BossKick");
        if (clip == null || clip.channels != 1) return null;
        var samples = new float[clip.samples];
        return clip.GetData(samples, 0) ? new BossKick(samples, clip.frequency) : null;
    }

    public void Mix(float[] output, int channels, int outputRate, double blockAt,
        float[] beats, double startAt, float offset, double cut, float volume)
    {
        int frames = output.Length / channels;
        double blockEnd = blockAt + (double)frames/outputRate;
        if (blockEnd <= startAt || blockAt >= cut) return;
        double fileTime = blockAt - startAt + offset;
        float earliest = (float)Math.Max(offset, fileTime - (double)pcm.Length/rate);
        int beat = Array.BinarySearch(beats, earliest);
        if (beat < 0) beat = ~beat;
        double step = (double)rate/outputRate;
        for (; beat < beats.Length; beat++)
        {
            double hit = startAt + ((double)beats[beat] - offset);
            if (hit >= blockEnd || hit >= cut) break;
            int first = Math.Max(0, (int)Math.Ceiling((hit-blockAt)*outputRate));
            int last = frames;
            if (cut < blockEnd) last = Math.Max(0, (int)Math.Ceiling((cut-blockAt)*outputRate));
            double sample = Math.Max(0, (blockAt-hit)*rate + first*step);
            for (int frame = first; frame < last && sample < pcm.Length-1; frame++, sample += step)
            {
                int i = (int)sample;
                float value = volume * (pcm[i] + (pcm[i+1]-pcm[i])*(float)(sample-i));
                for (int ch = 0; ch < channels; ch++) output[frame*channels+ch] += value;
            }
        }
    }
}
