using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static AudioClip hit1, shot1, enemyExplosion1, hurt1, playerDead, bossWarning;
    public static AudioClip blankbullet;
    static AudioSource audioSource;
    // Pitch is per AudioSource and also bends one-shots that are still playing, so pitched sounds
    // get their own sources instead of retuning the shared one under every other sound.
    static AudioSource[] pitchedSources;
    static int nextPitchedSource = 0;
    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        pitchedSources = new AudioSource[4];
        for (int i = 0; i < pitchedSources.Length; i++)
        {
            AudioSource s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;
            s.spatialBlend = audioSource.spatialBlend;
            pitchedSources[i] = s;
        }
        hit1 = Resources.Load<AudioClip>("Sounds/hit1");
        shot1 = Resources.Load<AudioClip>("Sounds/shot1");
        enemyExplosion1 = Resources.Load<AudioClip>("Sounds/enemyExplosion1");
        hurt1 = Resources.Load<AudioClip>("Sounds/hurt1");
        playerDead = Resources.Load<AudioClip>("Sounds/playerDead");
        blankbullet = Resources.Load<AudioClip>("Sounds/blankbullet");
        bossWarning = Resources.Load<AudioClip>("Sounds/bossWarning");
    }

    static AudioClip GetClip(string clip)
    {
        switch (clip)
        {
            case "hit1": return hit1;
            case "shot1": return shot1;
            case "enemyExplosion1": return enemyExplosion1;
            case "hurt1": return hurt1;
            case "playerDead": return playerDead;
            case "blankbullet": return blankbullet;
            case "bossWarning": return bossWarning;
        }
        return null;
    }

    public static void PlaySoundPitch(string clip, float volume, float pitch)
    {
        AudioClip c = GetClip(clip);
        if (c == null)
            return;
        AudioSource source = audioSource;
        if (pitch != 1f)
        {
            source = pitchedSources[nextPitchedSource];
            nextPitchedSource = (nextPitchedSource + 1) % pitchedSources.Length;
        }
        source.pitch = pitch;
        source.PlayOneShot(c, volume);
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
