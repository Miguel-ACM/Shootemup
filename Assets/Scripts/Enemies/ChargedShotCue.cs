using UnityEngine;
public sealed class ChargedShotCue : MonoBehaviour
{
    public static PlayerRound Apply(PlayerRound shot,float charge)
    {
        if(shot==null || charge<1.35f)return shot;
        var sr=ItemSpark.Sprite(shot.transform,"Capacitor wake","needle",new Color(.5f,.9f,.7f),.65f);
        sr.transform.localPosition=new Vector3(0,-.65f,0);sr.sortingOrder=15;
        shot.gameObject.AddComponent<ChargedShotCue>().wake=sr;return shot;
    }
    SpriteRenderer wake;
    float phase;
    void Update(){if(WorldTimeStop.Frozen)return;phase+=Time.deltaTime;wake.color=new Color(1,1,1,.55f+.2f*Mathf.Sin(phase*28));}
}
