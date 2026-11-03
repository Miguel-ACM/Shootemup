using System;
using UnityEngine;

// Play Intro once, loop Flight or Boss, then play out the selected Outro.
public sealed class DistrictScore : MonoBehaviour
{
    [Serializable] public class Span { public float start, end; }
    [Serializable] public class Sections { public Span intro, body, peak, outro; }
    [Serializable] public class Cues { public float bpm; public float[] boundaries; public Sections sections; }
    enum Part { Intro, Flight, Boss, Outro, Linear }
    sealed class Track
    {
        public AudioClip clip;
        public Cues cues;
        public Part First => cues == null ? Part.Linear : Part.Intro;
        Span Range(Part section) => section switch
        {
            Part.Intro => cues.sections.intro,
            Part.Flight => cues.sections.body,
            Part.Boss => cues.sections.peak,
            Part.Outro => cues.sections.outro,
            _ => null
        };
        public float Start(Part section) => Range(section)?.start ?? 0;
        public float End(Part section) => Mathf.Min(Range(section)?.end ?? clip.length, clip.length);

        // A hand-picked bar ending can fall between the detected phrase boundaries.
        public float ExitPoint(float start, float end, double earliest)
        {
            foreach (float boundary in cues.boundaries)
                if (boundary > start && boundary >= earliest && boundary < end) return boundary;
            return end;
        }
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
            double now = AudioSettings.dspTime;
            if (pendingPart == Part.Boss && !double.IsPositiveInfinity(cut))
                return (float)Math.Max(0, cut - now);
            double flightAt = cut;
            if (double.IsPositiveInfinity(cut))
            {
                var active = decks[index];
                double after = Math.Max(earliestBoss, Math.Max(now, reuseAt) + Lead);
                float end = track.End(part);
                if (after <= active.ClockAt(end))
                {
                    float point = track.ExitPoint(active.Offset, end, active.Offset + after - active.StartAt);
                    return (float)Math.Max(0, active.ClockAt(point) - now);
                }
                flightAt = active.ClockAt(end);
            }
            // A one-bar Flight may repeat several times during the three-second warning.
            float start = track.Start(Part.Flight), finish = track.End(Part.Flight);
            double length = finish - start;
            if (length <= 0) return 0;
            flightAt += Math.Max(0, Math.Ceiling((earliestBoss - flightAt) / length) - 1) * length;
            float exit = track.ExitPoint(start, finish, start + Math.Max(0, earliestBoss - flightAt));
            return (float)Math.Max(0, flightAt + exit - start - now);
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
        decks[index].StartCue(track.clip, AudioSettings.dspTime + Lead, track.Start(part), 0, 0, 0, .04);
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

    void Plan()
    {
        MusicDeck active = decks[index], next = decks[1-index];
        next.Source.Stop();
        float end = track.End(part);
        pendingTrack = part == Part.Outro ? null : track;
        pendingPart = part == Part.Intro ? Part.Flight : part;

        bool travel = part == Part.Outro && following != null && following.clip.loadState == AudioDataLoadState.Loaded;
        waitingForLoad = part == Part.Outro && following != null && !travel;
        if (wanted != part || travel)
        {
            double after = AudioSettings.dspTime + Lead;
            if (wanted == Part.Boss) after = Math.Max(after, earliestBoss);
            if (travel) after = Math.Max(after, Math.Min(active.ClockAt(end), active.StartAt + 8 * 60 / track.cues.bpm));
            if (after <= active.ClockAt(end))
            {
                end = track.ExitPoint(active.Offset, end, active.Offset + after - active.StartAt);
                pendingTrack = travel ? following : track;
                pendingPart = travel ? following.First : wanted;
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
        float offset = pendingTrack.Start(pendingPart);
        float from = pendingTrack == track ? active.MixAt(cut) : 0;
        float to = pendingPart == Part.Boss ? 1 : 0;
        float beats = to > from ? 1 : 8;
        float ramp = from == to ? 0 : Mathf.Min(beats * 60/track.cues.bpm, pendingTrack.End(pendingPart) - offset);
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
            if (wanted == part) wanted = pendingPart;
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
