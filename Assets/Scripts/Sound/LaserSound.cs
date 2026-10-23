using UnityEngine;

// Beam tails need sustained audio, not a repeated gunshot every damage tick.
public sealed class LaserSound : MonoBehaviour
{
    static AudioClip chargeClip, ignitionClip, sustainClip, releaseClip;
    static float lastCharge=-10,lastIgnition=-10,lastRelease=-10;
    static int sustaining;
    AudioSource transient, bed;
    bool ignited, cooling, counted, friendly;
    public bool Sustaining=>bed!=null && bed.isPlaying;
    public static int SustainingCount=>sustaining;
    void Prepare()
    {
        if(chargeClip==null)
        {
            chargeClip=Resources.Load<AudioClip>("Sounds/Beam/charge");
            ignitionClip=Resources.Load<AudioClip>("Sounds/Beam/ignite");
            sustainClip=Resources.Load<AudioClip>("Sounds/Beam/sustain");
            releaseClip=Resources.Load<AudioClip>("Sounds/Beam/release");
        }
        if(transient!=null)return;
        var manager=FindAnyObjectByType<SoundManager>();
        var routed=manager!=null ? manager.GetComponent<AudioSource>() : null;
        transient=gameObject.AddComponent<AudioSource>();bed=gameObject.AddComponent<AudioSource>();
        foreach(var source in new[]{transient,bed})
        {
            source.playOnAwake=false;source.spatialBlend=0;source.priority=100;
            if(routed!=null)source.outputAudioMixerGroup=routed.outputAudioMixerGroup;
        }
        bed.loop=true;bed.clip=sustainClip;
    }
    public void Charge(float seconds,bool player=false)
    {
        Prepare();Stop();friendly=player;ignited=cooling=false;
        if(Time.time-lastCharge<.11f || AudioListener.pause)return;
        lastCharge=Time.time;transient.clip=chargeClip;
        transient.pitch=Mathf.Clamp(.8f/Mathf.Max(.1f,seconds),.5f,2f);
        transient.volume=player ? .16f : .20f;transient.Play();
    }
    public void Drive(float power,bool firing)
    {
        if(transient==null)return;
        if(firing && !ignited)
        {
            ignited=true;transient.Stop();
            if(Time.time-lastIgnition>.075f)
            {
                lastIgnition=Time.time;transient.pitch=friendly ? 1.12f : 1;
                transient.volume=friendly ? .18f : .24f;transient.PlayOneShot(ignitionClip);
            }
            if(sustainClip!=null)
            {
                bed.pitch=friendly ? 1.12f : 1;bed.time=(((GetEntityId().GetHashCode()&0x7fffffff)%7)*.071f)%sustainClip.length;
                bed.Play();counted=true;sustaining++;
            }
        }
        if(!firing && ignited && !cooling)
        {
            cooling=true;
            if(Time.time-lastRelease>.075f)
            {lastRelease=Time.time;transient.pitch=1;transient.volume=.14f;transient.PlayOneShot(releaseClip);}
        }
        // The emitter can cool faster on later loops. Fade the release with that
        // same envelope so pooling never cuts an audible sample in mid-waveform.
        if(cooling)transient.volume=.14f*Mathf.Clamp01(power/.4f);
        bed.volume=(friendly ? .11f : .15f)*Mathf.Clamp01(power)/Mathf.Sqrt(Mathf.Max(1,sustaining));
        if(cooling && power<=.005f)StopBed();
    }
    void StopBed(){if(bed!=null)bed.Stop();if(counted){counted=false;sustaining=Mathf.Max(0,sustaining-1);}}
    public void Stop(){StopBed();if(transient!=null)transient.Stop();}
    void OnDisable(){Stop();}
    void OnDestroy(){Stop();}
}
