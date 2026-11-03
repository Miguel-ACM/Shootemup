using System;
using UnityEngine;

// The intro is contiguous with Flight in the recording. Fly through it once, then loop Flight.
public sealed class DistrictScore : MonoBehaviour
{
    [Serializable] public class Span { public float start, end; }
    [Serializable] public class Sections { public Span body, peak, outro; }
    [Serializable] public class Cues { public float bpm; public float[] boundaries; public Sections sections; }
    enum Part { Flight, Boss, Outro, Linear }
    sealed class Track
    {
        public AudioClip clip;
        public Cues cues;
        public Part First => cues == null ? Part.Linear : Part.Flight;
    }

    readonly MusicDeck[] decks = new MusicDeck[2];
    int index;
    Track track, following, pendingTrack;
    Part part, wanted, pendingPart;
    double cut = double.PositiveInfinity, earliestBoss, reuseAt, tailEnd;
    bool started, waitingForLoad;
    const double Lead = .1;
    public bool Active => track != null;
    public bool IsPlaying => Active && started && decks[index].Source.isPlaying;
    public bool WantsOutro => Active && wanted == Part.Outro;
    public string TrackName => following != null ? following.clip.name : track?.clip.name;
    public float BossLead
    {
        get
        {
            if (!started || wanted != Part.Boss || part == Part.Boss || !decks[index].Source.isPlaying) return 0;
            double arrival = cut;
            if (double.IsPositiveInfinity(cut))
            {
                foreach (float b in track.cues.boundaries)
                    if (b > decks[index].Offset && decks[index].ClockAt(b) >= earliestBoss)
                        return (float)Math.Max(0, decks[index].ClockAt(b) - AudioSettings.dspTime);
                return 0;
            }
            if (pendingPart != Part.Boss && pendingTrack != null)
            {
                // The flight loop wrapped inside the warning. Look one deck ahead for its first cadence.
                var next = decks[1-index];
                foreach (float b in track.cues.boundaries)
                    if (b > track.cues.sections.body.start && next.ClockAt(b) >= earliestBoss)
                    { arrival = next.ClockAt(b); break; }
            }
            return (float)Math.Max(0, arrival - AudioSettings.dspTime);
        }
    }
    public int Generation { get; private set; }

    void Awake()
    {
        for (int i = 0; i < decks.Length; i++)
        {
            var child = new GameObject("Music deck " + (i + 1));
            child.transform.SetParent(transform, false);
            decks[i] = child.AddComponent<MusicDeck>();
        }
    }

    public void Play(AudioClip clip, Cues cues)
    {
        var song = new Track { clip = clip, cues = cues };
        if (clip.loadState == AudioDataLoadState.Unloaded) clip.LoadAudioData();
        if (WantsOutro)
        {
            following = song;
            Replan();
            return;
        }
        Stop();
        track = song;
        wanted = song.First;
        TryStart();
    }

    public void Stop()
    {
        Generation++;
        foreach (var deck in decks)
        {
            deck.Source.Stop();
            deck.Source.clip = null;
        }
        started = false;
        reuseAt = 0;
        waitingForLoad = false;
        track = pendingTrack = following = null;
        cut = double.PositiveInfinity;
    }

    void TryStart()
    {
        if (track.clip.loadState == AudioDataLoadState.Failed) { Stop(); return; }
        if (track.clip.loadState != AudioDataLoadState.Loaded) return;
        part = track.First;
        started = true;
        decks[index].StartCue(track.clip, AudioSettings.dspTime + Lead, 0, 0, 0, 0, .04);
        Plan();
    }

    public void Boss(float warningSeconds)
    {
        if (!Active || track.cues == null || wanted == Part.Boss) return;
        wanted = Part.Boss;
        earliestBoss = AudioSettings.dspTime + warningSeconds;
        Replan();
    }

    public void Outro()
    {
        if (!Active || track.cues == null || wanted == Part.Outro) return;
        wanted = Part.Outro;
        Replan();
    }

    void Replan()
    {
        // Audio already committed to the device must land. Its successor observes the latest request.
        double now = AudioSettings.dspTime;
        if (started && now >= reuseAt && cut > now + Lead) Plan();
    }

    float SectionEnd()
    {
        if (part == Part.Flight) return track.cues.sections.body.end;
        if (part == Part.Boss) return track.cues.sections.peak.end;
        return track.clip.length;
    }

    void Plan()
    {
        MusicDeck active = decks[index], next = decks[1-index];
        next.Source.Stop();
        float end = Mathf.Min(SectionEnd(), track.clip.length);
        float offset = 0;
        pendingTrack = track;
        pendingPart = part;
        if (part == Part.Outro) pendingTrack = null;
        if (part == Part.Boss) offset = track.cues.sections.peak.start;
        if (part == Part.Flight) offset = track.cues.sections.body.start;

        bool travel = part == Part.Outro && following != null && following.clip.loadState == AudioDataLoadState.Loaded;
        waitingForLoad = part == Part.Outro && following != null && !travel;
        if (wanted != part || travel)
        {
            double after = AudioSettings.dspTime + Lead;
            if (wanted == Part.Boss) after = Math.Max(after, earliestBoss);
            if (travel) after = Math.Max(after, active.StartAt + 8 * 60 / track.cues.bpm);
            foreach (float boundary in track.cues.boundaries)
            {
                if (boundary <= active.Offset || boundary > end + .002f || active.ClockAt(boundary) < after) continue;
                end = Mathf.Min(end, boundary);
                if (travel)
                {
                    pendingTrack = following;
                    pendingPart = following.First;
                    offset = 0;
                }
                else
                {
                    pendingPart = wanted;
                    offset = wanted == Part.Boss ? track.cues.sections.peak.start : track.cues.sections.outro.start;
                }
                break;
            }
        }
        cut = active.ClockAt(end);
        double fade = track.cues == null ? .5 : Math.Clamp(120 / track.cues.bpm, .48, 1);
        // Keep playing the outgoing tail behind the incoming downbeat; reclaim its deck after the fade.
        double overlap = pendingTrack == null ? 0 : Math.Min(fade, Math.Max(0, track.clip.length - end));
        if (overlap < .04) overlap = 0;
        tailEnd = cut + overlap;
        active.EndAt(tailEnd, overlap > 0 ? overlap : fade);
        if (pendingTrack == null) return;
        float from = pendingTrack == track ? active.MixAt(cut) : 0;
        float to = pendingPart == Part.Boss ? 1 : 0;
        float beats = to > from ? 1 : 8;
        float ramp = from == to ? 0 : Mathf.Min(beats * 60/track.cues.bpm, pendingTrack.clip.length - offset);
        next.StartCue(pendingTrack.clip, cut, offset, from, to, ramp, overlap > 0 ? overlap : fade);
    }

    void Update()
    {
        if (!Active) return;
        if (!started) { TryStart(); return; }
        double now = AudioSettings.dspTime;
        if (now >= cut)
        {
            if (pendingTrack == null)
            {
                var nextSong = following;
                Stop();
                if (nextSong != null) { track = nextSong; wanted = nextSong.First; TryStart(); }
                return;
            }
            index = 1-index;
            if (pendingTrack == following)
            {
                following = null;
                wanted = pendingPart;
                Generation++;
            }
            track = pendingTrack;
            part = pendingPart;
            cut = double.PositiveInfinity;
            reuseAt = tailEnd + .015;
            waitingForLoad = false;
        }
        if (now >= reuseAt && (double.IsPositiveInfinity(cut) ||
            (waitingForLoad && following.clip.loadState == AudioDataLoadState.Loaded && now + Lead < cut)))
            Plan();
    }

    public void Volume(float volume)
    {
        foreach (var deck in decks) deck.Source.volume = volume;
    }
}
