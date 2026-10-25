using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BGMManager : MonoBehaviour
{
    static readonly Dictionary<string, AudioClip> tracks = new Dictionary<string, AudioClip>();
    static AudioSource audioSource;
    void Awake()
    {
        var sceneSource = GetComponent<AudioSource>();
        sceneSource.playOnAwake = false;
        sceneSource.Stop();
        if (audioSource == null)
        {
            // Keep the selected track playing through the cloud curtain and scene load.
            var music = new GameObject("Persistent level music");
            DontDestroyOnLoad(music);
            audioSource = music.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0;
            audioSource.priority = 1;
        }
    }

    void Start()
    {
        if (SceneManager.GetActiveScene().name == "Title") StopBGM();
        else if (!audioSource.isPlaying) PlayLevelMusic(LevelRun.Current);
    }

    public static void PlayLevelMusic(LevelId level)
    {
        PlayBGM(LevelTrack(level));
    }
    public static string LevelTrack(LevelId level)=>level==LevelId.Desert ? "fields" : level.ToString().ToLowerInvariant();

    public static void PlayBGM(string bgm)
    {
        if (audioSource == null) return;
        if (!tracks.TryGetValue(bgm, out var clip) || clip == null)
        {
            clip = Resources.Load<AudioClip>("Music/" + bgm);
            if (clip == null)
            {
                Debug.LogWarning("Missing music track: Music/" + bgm);
                return;
            }
            tracks[bgm] = clip;
        }
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.volume = 0.1f;
        audioSource.pitch = 1f;
        audioSource.loop = bgm != "stageComplete";
        audioSource.Play();
    }

    public static void StopBGM()
    {
        audioSource.Stop();
    }


    public static void SetLoop(bool loop)
    {
        audioSource.loop = loop;
    }


    public static IEnumerator FadeOutSong(float duration)
    {
        float currentTime = 0;
        float start = audioSource.volume;
        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(start, 0, currentTime / duration);
            yield return null;
        }
        audioSource.Stop();
        yield break;
    }

    public static IEnumerator FadeOutSongAndPlay(float duration, string newSong, float pitch = 1.0f)
    {
        float currentTime = 0;
        float start = audioSource.volume;
        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(start, 0, currentTime / duration);
            yield return null;
        }
        audioSource.pitch = 1f;
        PlayBGM(newSong);
        yield break;
    }

    public static void ChangePitch(float pitch)
    {
        audioSource.pitch = pitch;
    }

    public static void ChangeMood(int semitones)
{
    // Convert semitones to pitch multiplier
    float pitchMultiplier = Mathf.Pow(2f, semitones / 12f);

    audioSource.pitch = pitchMultiplier;
}
    public static void RestartSong()
    {
        audioSource.Stop();
        audioSource.Play();
    }


}
