using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static AudioClip hit1, shot1, enemyExplosion1, hurt1, playerDead, bossWarning;
    public static AudioClip blankbullet;
    static AudioSource audioSource;
    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        hit1 = Resources.Load<AudioClip>("Sounds/hit1");
        shot1 = Resources.Load<AudioClip>("Sounds/shot1");
        enemyExplosion1 = Resources.Load<AudioClip>("Sounds/enemyExplosion1");
        hurt1 = Resources.Load<AudioClip>("Sounds/hurt1");
        playerDead = Resources.Load<AudioClip>("Sounds/playerDead");
        blankbullet = Resources.Load<AudioClip>("Sounds/blankbullet");
        bossWarning = Resources.Load<AudioClip>("Sounds/bossWarning");
    }

    public static void PlaySoundPitch(string clip, float volume, float pitch)
    {
        audioSource.pitch = pitch;
        switch (clip)
        {
            case "hit1":
                audioSource.PlayOneShot(hit1, volume);
                break;
            case "shot1":
                audioSource.PlayOneShot(shot1, volume);
                break;
            case "enemyExplosion1":
                audioSource.PlayOneShot(enemyExplosion1, volume);
                break;
            case "hurt1":
                audioSource.PlayOneShot(hurt1, volume);
                break;
            case "playerDead":
                audioSource.PlayOneShot(playerDead, volume);
                break;
            case "blankbullet":
                audioSource.PlayOneShot(blankbullet, volume);
                break;
            case "bossWarning":
                audioSource.PlayOneShot(bossWarning, volume);
                break;
        }
    }

    public static void PlaySoundRandomPitch(string clip, float volume, float[] pitchRange)
    {
        PlaySoundPitch(clip, volume, Random.Range(pitchRange[0], pitchRange[1]));
    }

    public static void PlaySound(string clip, float volume)
    {
        PlaySoundPitch(clip, volume, 1f);
    }
}
