using System;
using UnityEngine;

// Play Intro once, loop Flight or Boss, then play out the selected Outro.
public sealed class DistrictScore : MonoBehaviour
{
    [Serializable] public class Span { public float start, end; }
    [Serializable] public class Chord { public float time; public string name; }
    [Serializable] public class Sections { public Span intro, body, peak, outro; }
    [Serializable] public class Cues { public float bpm, duration, mp3Lead; public float[] beats, downbeats, boundaries; public Chord[] chords; public Sections sections; }
    enum Part { Intro, Flight, Boss, Outro, Linear }
    sealed class Track
    {
        public AudioClip clip;
        public Cues cues;
        public bool adaptive;
        public Part First => adaptive ? Part.Intro : Part.Linear;
        Span Range(Part section) => section switch
        {
            Part.Intro => cues.sections.intro,
            Part.Flight => cues.sections.body,
            Part.Boss => cues.sections.peak,
            Part.Outro => cues.sections.outro,
            _ => null
        };
        public float Start(Part section) => Range(section)?.start ?? 0;
        // Some importers retain the MP3 priming samples. A gapless clip already
        // matches the analyzed duration (rounded to milliseconds).
        public float ClipLead => cues != null && cues.mp3Lead > 0 && cues.duration > 0 &&
            clip.length >= cues.duration + cues.mp3Lead - .001f ? cues.mp3Lead : 0;
        public float Length => clip.length - ClipLead;
        public float End(Part section) => Mathf.Min(Range(section)?.end ?? Length, Length);

        public string OpeningChord(Part section)
        {
            var chords = cues?.chords;
            if (chords == null) return null;
            float start = Start(section), end = End(section);
            int i = 0;
            while (i+1 < chords.Length && chords[i+1].time <= start) i++;
            // The IN marker may fall within a held chord or before the first pitched beat.
            for (; i < chords.Length && chords[i].time < end; i++)
                if (!string.IsNullOrEmpty(chords[i].name) && chords[i].name != "N") return chords[i].name;
            return null;
        }

        public float ExitPoint(float start, float end, double earliest, double deadline, string target)
        {
            double limit = Math.Min(end, Math.Max(earliest, deadline));
            if (target != null && cues?.chords != null)
                foreach (var chord in cues.chords)
                {
                    if (chord.time > limit) break;
                    if (chord.time > start && chord.time >= earliest && chord.name == target) return chord.time;
                }
            if (cues?.boundaries != null)
                foreach (float point in cues.boundaries)
                {
                    if (point > limit) break;
                    if (point > start && point >= earliest) return point;
                }
            if (end <= limit) return end;
            return LastPoint(cues?.downbeats) ?? LastPoint(cues?.beats) ?? (float)limit;

            float? LastPoint(float[] points)
            {
                if (points != null)
                    for (int i = points.Length-1; i >= 0 && points[i] >= earliest; i--)
                        if (points[i] <= limit && points[i] > start) return points[i];
                return null;
            }
        }
    }

    readonly MusicDeck[] decks = new MusicDeck[2];
    int index;
    Track track, following, pendingTrack;
    Part part, wanted, pendingPart;
    double cut = double.PositiveInfinity, earliestBoss, reuseAt, tailEnd;
    double requestDeadline, travelDeadline;
    const double OutroWait = .35;
    bool started, waitingForLoad;
    float bossTempo = 1;

    public void Phase(int phase) => bossTempo = 1 + .10f * Mathf.Clamp(phase - 1, 0, 4);
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
            string target = track.OpeningChord(Part.Boss);
            double flightAt = cut;
            if (double.IsPositiveInfinity(cut))
            {
                var active = decks[index];
                double after = Math.Max(earliestBoss, Math.Max(now, reuseAt) + Lead);
                float end = track.End(part);
                if (after <= active.ClockAt(end))
                {
                    float point = track.ExitPoint(active.Offset, end, active.PositionAt(after),
                        active.PositionAt(requestDeadline), target);
                    return (float)Math.Max(0, active.ClockAt(point) - now);
                }
                flightAt = active.ClockAt(end);
            }
            // A one-bar Flight may repeat several times during the three-second warning.
            float start = track.Start(Part.Flight), finish = track.End(Part.Flight);
            double length = finish - start;
            if (length <= 0) return 0;
            flightAt += Math.Max(0, Math.Ceiling((earliestBoss - flightAt) / length) - 1) * length;
            float exit = track.ExitPoint(start, finish, start + Math.Max(0, earliestBoss - flightAt),
                start + requestDeadline - flightAt, target);
            return (float)Math.Max(0, flightAt + exit - start - now);
        }
    }
    public int Generation { get; private set; }

    /// <summary>Seconds from now to the nearest analysed beat of the playing section (negative
    /// once it has passed), or NaN when no beat map is playing.</summary>
    public float BeatOffset()
    {
        var beats = track?.cues?.beats;
        if (!IsPlaying || beats == null || beats.Length == 0)
            return float.NaN;
        var deck = decks[index];
        float now = deck.PositionAt(AudioSettings.dspTime);
        int next = Array.BinarySearch(beats, now);
        if (next < 0)
            next = ~next;
        float after = next < beats.Length ? beats[next] - now : float.PositiveInfinity;
        float before = next > 0 ? beats[next - 1] - now : float.NegativeInfinity;
        return (after < -before ? after : before) / deck.Tempo;
    }

    void Awake()
    {
        var kick = BossKick.Load();
        for (int i = 0; i < decks.Length; i++)
        {
            var child = new GameObject("Music deck " + (i + 1));
            child.transform.SetParent(transform, false);
            decks[i] = child.AddComponent<MusicDeck>();
            decks[i].Kick = kick;
        }
        decks[0].OtherDeck = decks[1];
        decks[1].OtherDeck = decks[0];
    }

    public void Play(AudioClip clip, Cues cues, bool adaptive = true)
    {
        var song = new Track { clip = clip, cues = cues, adaptive = adaptive && cues != null };
        if (clip.loadState == AudioDataLoadState.Unloaded) clip.LoadAudioData();
        if (WantsOutro)
        {
            following = song;
            travelDeadline = AudioSettings.dspTime + .45;
            Replan();
            return;
        }
        Stop();
        track = song;
        wanted = song.First;
        TryStart();
    }

    public double PlayLinearAccent(AudioClip clip, Cues cues, float accent, float secondsToAccent)
    {
        Stop();
        track = new Track { clip = clip, cues = cues, adaptive = false };
        wanted = Part.Linear;
        double now = AudioSettings.dspTime;
        double accentAt = now + Math.Max(Lead, secondsToAccent);
        double startAt = Math.Max(now + Lead, accentAt - accent);
        // When there isn't room for the whole lead-in, seek past its silent samples.
        float position = Mathf.Max(0, accent - (float)(accentAt - startAt));
        StartFirstCue(startAt, position);
        return accentAt;
    }

    public void Stop()
    {
        Generation++;
        bossTempo = 1;
        foreach (var deck in decks)
        {
            deck.Stop();
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
        StartFirstCue(AudioSettings.dspTime + Lead, track.Start(track.First));
    }

    void StartFirstCue(double startAt, float position)
    {
        part = track.First;
        started = true;
        decks[index].StartCue(track.clip, startAt, position, 0, 0, 0, .04,
            clipLead: track.ClipLead);
        Plan();
    }

    public void Boss(float warningSeconds)
    {
        if (!Active || !track.adaptive || wanted == Part.Boss) return;
        wanted = Part.Boss;
        double now = AudioSettings.dspTime;
        earliestBoss = now + warningSeconds;
        requestDeadline = earliestBoss + .35;
        Replan();
    }

    public void Outro()
    {
        if (!Active || !track.adaptive || wanted == Part.Outro) return;
        wanted = Part.Outro;
        requestDeadline = AudioSettings.dspTime + OutroWait;
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
        next.Stop();
        float end = track.End(part);
        pendingTrack = part == Part.Outro ? null : track;
        pendingPart = part == Part.Intro ? Part.Flight : part;

        bool travel = following != null && following.clip.loadState == AudioDataLoadState.Loaded;
        waitingForLoad = following != null && !travel;
        if (wanted != part || travel)
        {
            double after = AudioSettings.dspTime + Lead;
            if (wanted == Part.Boss) after = Math.Max(after, earliestBoss);
            if (after <= active.ClockAt(end))
            {
                pendingTrack = travel ? following : track;
                pendingPart = travel ? following.First : wanted;
                double deadline = travel ? travelDeadline : requestDeadline;
                end = track.ExitPoint(active.Offset, end, active.PositionAt(after),
                    active.PositionAt(deadline), pendingTrack.OpeningChord(pendingPart));
            }
        }
        cut = active.ClockAt(end);
        bool enteringOutro = pendingPart == Part.Outro && part != Part.Outro;
        double fade = enteringOutro ? .24 : travel || pendingPart == Part.Boss && part != Part.Boss ? .35 : track.adaptive ? Math.Clamp(120 / track.cues.bpm, .48, 1) : .5;
        // Keep playing the outgoing tail behind the incoming downbeat; reclaim its deck after the fade.
        double overlap = pendingTrack == null ? 0 : Math.Min(fade, Math.Max(0, (track.Length - end) / active.Tempo));
        if (overlap < .04) overlap = 0;
        tailEnd = cut + overlap;
        active.EndAt(tailEnd, overlap > 0 ? overlap : fade, cut);
        if (pendingTrack == null) return;
        float offset = pendingTrack.Start(pendingPart);
        float from = pendingTrack == track ? active.MixAt(cut) : 0;
        float to = pendingPart == Part.Boss ? 1 : 0;
        float beats = enteringOutro || to > from ? 1 : 8;
        float ramp = from == to ? 0 : Mathf.Min(beats * 60/track.cues.bpm, pendingTrack.End(pendingPart) - offset);
        next.StartCue(pendingTrack.clip, cut, offset, from, to, ramp, overlap > 0 ? overlap : fade,
            pendingPart == Part.Boss ? pendingTrack.cues.beats : null, pendingTrack.ClipLead, pendingPart == Part.Boss ? bossTempo : 1);
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
        if (part == Part.Boss && wanted == Part.Boss && following == null && now >= reuseAt &&
            !Mathf.Approximately(decks[index].Tempo, bossTempo) && now + Lead + .12 < cut)
        {
            var old = decks[index];
            double at = now + Lead;
            const double blend = .045;
            old.EndAt(at + blend, blend, at);
            index = 1 - index;
            decks[index].StartCue(track.clip, at, old.PositionAt(at), 1, 1, 0, blend,
                track.cues.beats, track.ClipLead, bossTempo);
            cut = double.PositiveInfinity;
            reuseAt = at + blend + .015;
        }
        if (now >= reuseAt && (double.IsPositiveInfinity(cut) ||
            (waitingForLoad && following.clip.loadState == AudioDataLoadState.Loaded && now + Lead < cut)))
            Plan();
    }

    public void Volume(float volume)
    {
        foreach (var deck in decks) deck.SetVolume(volume);
    }
}
