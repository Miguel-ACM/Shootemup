using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BGMManager : MonoBehaviour
{
    const float BaseVolume = .1f;
    public const string HangarTrack = "hangar";
    const float HangarTitleChord = .868f; // The first full chord attack in the hangar recording.
    static DistrictScore score;
    static readonly Dictionary<string, DistrictScore.Cues> maps = new Dictionary<string, DistrictScore.Cues>();
    static float fade = 1;
    public static bool Playing => score != null && score.IsPlaying;
    public static bool OutroQueued => score != null && score.WantsOutro;

    void Awake()
    {
        var sceneSource = GetComponent<AudioSource>();
        sceneSource.playOnAwake = false;
        sceneSource.Stop();
        if (score != null) return;
        var music = new GameObject("Persistent level music");
        DontDestroyOnLoad(music);
        score = music.AddComponent<DistrictScore>();
        fade = 1;
        ApplyVolume();
    }

    void Start()
    {
        if (SceneManager.GetActiveScene().name == "Title")
            return; // The title director cues the music after the tunnel is ready.
        if (HangarHub.Active)
            PlayHangarMusic();
        else if (!score.Active)
            PlayLevelMusic(LevelRun.Current);
    }

    public static string LevelTrack(LevelId level) => level.ToString().ToLowerInvariant();
    public static void PlayLevelMusic(LevelId level) => Play(LevelTrack(level), true);

    public static void PlayHangarMusic()
    {
        if (score != null && score.TrackName != HangarTrack) Play(HangarTrack, false);
    }

    public static AudioClip PrepareTitleMusic()
    {
        StopBGM();
        var clip = Resources.Load<AudioClip>("Music/" + HangarTrack);
        if (clip != null && clip.loadState == AudioDataLoadState.Unloaded)
            clip.LoadAudioData();
        return clip;
    }

    public static double PlayTitleMusic(AudioClip clip, float secondsToJoin)
    {
        if (score == null || clip == null || clip.loadState != AudioDataLoadState.Loaded)
            return double.NaN;
        fade = 1;
        ApplyVolume();
        var cues = LoadCues(HangarTrack);
        if (AudioListener.pause || AudioSettings.dspTime <= 0)
        {
            score.Play(clip, cues, false);
            return double.NaN;
        }
        return score.PlayLinearAccent(clip, cues, HangarTitleChord, secondsToJoin);
    }

    static void Play(string id, bool district)
    {
        if (score == null) return;
        var clip = Resources.Load<AudioClip>("Music/" + id);
        if (clip == null)
        {
            // New districts await Miguel's tracks; never carry the previous district's song into them.
            if (id == "heaven" || id == "void" || id == "sewers" || id == "core" || id == "mushroom" || id == "dream") { score.Stop(); return; }
            Debug.LogWarning("Missing music track: " + id); return;
        }
        fade = 1;
        ApplyVolume();
        score.Play(clip, LoadCues(id), district);
    }

    static DistrictScore.Cues LoadCues(string id)
    {
        if (!maps.TryGetValue(id, out var cues))
        {
            var data = Resources.Load<TextAsset>("MusicAnalysis/" + id);
            if (data != null) cues = JsonUtility.FromJson<DistrictScore.Cues>(data.text);
            if (cues == null) Debug.LogWarning("Missing music cue sheet: " + id);
            maps[id] = cues;
        }
        return cues;
    }

    public static void QueueBoss(float warningSeconds)
    {
        if (score != null) score.Boss(warningSeconds);
    }
    public static float BossLead => score != null ? score.BossLead : 0;
    /// <summary>Seconds to the nearest beat of the playing music, or NaN without a beat map.</summary>
    public static float BeatOffset => score != null ? score.BeatOffset() : float.NaN;
    public static void QueueOutro()
    {
        if (score != null) score.Outro();
    }
    public static void ApplyVolume()
    {
        if (score != null) score.Volume(BaseVolume * GameOptions.MusicVolume * fade);
    }
    public static void StopBGM()
    {
        if (score != null) score.Stop();
    }
    public static IEnumerator FadeOutSong(float duration)
    {
        if (score == null) yield break;
        int generation = score.Generation;
        for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime)
        {
            if (score == null || score.Generation != generation) yield break;
            fade = 1 - elapsed / duration;
            ApplyVolume();
            yield return null;
        }
        if (score != null && score.Generation == generation) score.Stop();
    }
}
