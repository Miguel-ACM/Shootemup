using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BGMManager : MonoBehaviour
{
    const float BaseVolume = .1f;
    public const string HangarTrack = "hangar";
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
        if (SceneManager.GetActiveScene().name == "Title" || HangarHub.Active)
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

    static void Play(string id, bool district)
    {
        if (score == null) return;
        var clip = Resources.Load<AudioClip>("Music/" + id);
        if (clip == null)
        {
            // New districts await Miguel's tracks; never carry the previous district's song into them.
            if (id == "heaven" || id == "void") { score.Stop(); return; }
            Debug.LogWarning("Missing music track: " + id); return;
        }
        if (!maps.TryGetValue(id, out var cues))
        {
            var data = Resources.Load<TextAsset>("MusicAnalysis/" + id);
            if (data != null) cues = JsonUtility.FromJson<DistrictScore.Cues>(data.text);
            if (cues == null) Debug.LogWarning("Missing music cue sheet: " + id);
            maps[id] = cues;
        }
        fade = 1;
        ApplyVolume();
        score.Play(clip, cues, district);
    }

    public static void QueueBoss(float warningSeconds)
    {
        if (score != null) score.Boss(warningSeconds);
    }
    public static float BossLead => score != null ? score.BossLead : 0;
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
