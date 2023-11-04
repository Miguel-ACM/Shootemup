using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BGMManager : MonoBehaviour
{
    public static AudioClip greenlandsOST, stage1bossOST, stageCompleteOST;
    static AudioSource audioSource;
    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
       
        greenlandsOST = Resources.Load<AudioClip>("Music/greenlandsOST");
        stage1bossOST = Resources.Load<AudioClip>("Music/stage1bossOST");
        stageCompleteOST = Resources.Load<AudioClip>("Music/stageComplete");
        PlayBGM("greenlandsOST");
    }

    public static void PlayBGM(string bgm)
    {
        switch (bgm)
        {
            case "greenlandsOST":
                audioSource.clip = greenlandsOST;
                break;
            case "stage1bossOST":
                audioSource.clip = stage1bossOST;
                break;
            case "stageComplete":
                audioSource.clip = stageCompleteOST;
                break;
        }
        audioSource.volume = 0.1f;
        audioSource.Stop();
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

    public static void RestartSong()
    {
        audioSource.Stop();
        audioSource.Play();
    }


}
