using System;
using UnityEngine;

// Each deck has its own source and filter, so only the recording receives the bass lift.
[RequireComponent(typeof(AudioSource))]
public sealed class MusicDeck : MonoBehaviour
{
    public const float BassDb = 4.5f;
    public static float BossBassDb(string track) => track == "tunnel" ? 5.5f : BassDb;
    const double ShelfHz = 145;
    public AudioSource Source { get; private set; }
    public double StartAt { get; private set; }
    public float Offset => offset;
    public float Tempo { get; private set; } = 1;
    public BossKick Kick { private get; set; }
    public MusicDeck OtherDeck { private get; set; }
    float[] kickBeats;
    volatile float kickVolume = 1;
    float offset, from, to, seconds;
    double fadeIn;
    readonly double[] z1 = new double[8], z2 = new double[8];
    double b0, b1, b2, a1, a2;
    int sampleRate;

    // Publish an immutable envelope to the audio thread. Filter memory belongs to that thread.
    sealed class Envelope
    {
        public double start, stop, fadeIn, fadeOut, kickCut;
        public float from, to, duration, offset, tempo;
        public float[] beats;
        public double b0, b1, b2, a1, a2;
    }
    volatile Envelope envelope;
    Envelope previous;

    void Awake()
    {
        Source = GetComponent<AudioSource>();
        Source.playOnAwake = false;
        Source.spatialBlend = 0;
        Source.priority = 1;
        Source.ignoreListenerVolume = true;
        sampleRate = Math.Max(8000, AudioSettings.outputSampleRate);
        SetBass(BassDb);
    }

    void SetBass(float decibels)
    {
        // RBJ low shelf, unity shelf slope. Dry/wet interpolation releases to a true bypass.
        double a = Math.Pow(10, decibels / 40.0);
        double w = 2 * Math.PI * ShelfHz / sampleRate;
        double c = Math.Cos(w), s = Math.Sqrt(2 * a) * Math.Sin(w);
        double norm = (a+1) + (a-1)*c + s;
        b0 = a*((a+1) - (a-1)*c + s)/norm;
        b1 = 2*a*((a-1) - (a+1)*c)/norm;
        b2 = a*((a+1) - (a-1)*c - s)/norm;
        a1 = -2*((a-1) + (a+1)*c)/norm;
        a2 = ((a+1) + (a-1)*c - s)/norm;
    }

    public void StartCue(AudioClip clip, double at, float position, float mixFrom, float mixTo, float ramp, double fade,
        float[] beats = null, float clipLead = 0, float tempo = 1)
    {
        Stop();
        SetBass(BossBassDb(clip.name));
        Tempo = Source.pitch = tempo;
        StartAt = at;
        offset = position;
        from = mixFrom;
        to = mixTo;
        seconds = ramp;
        fadeIn = fade;
        kickBeats = beats;
        Source.clip = clip;
        Source.loop = false;
        Source.timeSamples = Mathf.Clamp(Mathf.RoundToInt((position + clipLead) * clip.frequency), 0, clip.samples - 1);
        SetEnvelope(double.PositiveInfinity, .01, double.PositiveInfinity);
        Source.PlayScheduled(at);
    }

    void SetEnvelope(double end, double fadeOut, double cut)
    {
        envelope = new Envelope { start = StartAt, stop = end, from = from, to = to, duration = seconds,
            fadeIn = fadeIn, fadeOut = fadeOut, offset = offset, beats = kickBeats, tempo = Tempo, kickCut = cut,
            b0 = b0, b1 = b1, b2 = b2, a1 = a1, a2 = a2 };
    }

    public void EndAt(double at, double fade, double cut)
    {
        SetEnvelope(at, fade, cut);
        Source.SetScheduledEndTime(at);
    }

    public float MixAt(double at) => seconds <= 0 ? to : Mathf.Lerp(from, to, (float)(at - StartAt)/seconds);
    public double ClockAt(float position) => StartAt + (position - offset) / Tempo;

    public float PositionAt(double at) => offset + (float)((at - StartAt) * Tempo);

    public void Stop()
    {
        envelope = null;
        Source.Stop();
    }

    public void SetVolume(float volume)
    {
        Source.volume = volume;
        // The source fader has already scaled the recording when this filter adds the kick.
        kickVolume = volume;
    }

    void OnAudioFilterRead(float[] samples, int channels)
    {
        var env = envelope;
        if (env == null || channels > z1.Length) return;
        if (previous == null || previous.start != env.start)
        {
            Array.Clear(z1, 0, z1.Length);
            Array.Clear(z2, 0, z2.Length);
        }
        previous = env;
        double blockAt = AudioSettings.dspTime, now = blockAt, step = 1.0 / sampleRate;
        var other = OtherDeck?.envelope;
        if (env.beats == null && other?.beats == null && env.from == 0 && env.to == 0 && now >= env.start + env.fadeIn &&
            now + samples.Length*step/channels <= env.stop - env.fadeOut) return;
        for (int i = 0; i < samples.Length; now += step)
        {
            double t = env.duration <= 0 ? 1 : Math.Clamp((now - env.start)/env.duration, 0, 1);
            double mix = env.from + t*(env.to-env.from);
            double gain = Math.Clamp(Math.Min((now - env.start)/env.fadeIn, (env.stop - now)/env.fadeOut), 0, 1);
            for (int ch = 0; ch < channels; ch++, i++)
            {
                // Coefficients travel with the immutable cue, so changing tracks
                // cannot alter a block already being processed by the audio thread.
                double dry = samples[i], wet = env.b0*dry + z1[ch];
                z1[ch] = env.b1*dry - env.a1*wet + z2[ch];
                z2[ch] = env.b2*dry - env.a2*wet;
                samples[i] = (float)(gain * (dry + mix*(wet-dry)));
            }
        }
        // During a crossfade both recordings feel the same kick. Only its owner adds the drum.
        if (other?.beats != null && kickVolume > 0)
            Kick?.Mix(samples, channels, sampleRate, blockAt, other.beats, other.start, other.offset, other.kickCut, 0, other.tempo);
        if (env.beats != null && kickVolume > 0)
            Kick?.Mix(samples, channels, sampleRate, blockAt, env.beats, env.start, env.offset, env.kickCut, kickVolume, env.tempo);
    }
}
