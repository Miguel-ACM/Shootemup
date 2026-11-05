using System.Collections;
using UnityEngine;

public sealed partial class VoidSovereignBoss
{
    IEnumerator Interlude(int chapter)
    {
        Hidden=true;InterludeElapsed=0;Hull.gameObject.SetActive(false);contact.enabled=false;
        float beatAt=.9f;int beat=0;
        // Wall-clock combat seconds, independent of difficulty/tempo. Pause and time stop suspend it.
        while(InterludeElapsed<15)
        {
            if(!GameRules.playerDisabled&&!WorldTimeStop.Frozen)
            {
                InterludeElapsed=Mathf.Min(15,InterludeElapsed+Time.deltaTime);
                DrawEdgeTell(chapter,beat,Mathf.Clamp01(1-(beatAt-InterludeElapsed)/.7f));
                if(InterludeElapsed>=beatAt&&InterludeElapsed<13.8f)
                {
                    PerimeterBarrage(chapter,beat++);
                    beatAt+=Difficulty.Rest(chapter==1?.86f:chapter==2?.70f:.62f);
                }
            }
            yield return null;
        }
        foreach(var tell in edgeTell)tell.enabled=false;
        CombatRound.ClearAll();
        transform.position=new Vector3(0,Camera.UpLimit-3.25f,1);
        perch=transform.position;
        SetForm();Hull.gameObject.SetActive(true);Hidden=false;
        PhaseBreak.Play(transform.position);
        float settle=0;
        while(settle<1.1f){if(!GameRules.playerDisabled&&!WorldTimeStop.Frozen)settle+=Time.deltaTime;yield return null;}
        contact.enabled=true;
    }
    void DrawEdgeTell(int chapter,int beat,float charge)
    {
        Rect r=CombatRound.Border;
        Vector3 P(float x,float y)=>new Vector3(x,y,-1.1f);
        var ink=LevelProjectilePalette.Accent(LevelId.Void);ink.a=.12f+charge*.62f;
        for(int i=0;i<4;i++)
        {
            var line=edgeTell[i];line.enabled=chapter==3||i/2==beat%2;
            line.startColor=line.endColor=ink;
            if(i<2){float y=i==0?r.yMax-.12f:r.yMin+.12f;line.SetPosition(0,P(r.xMin,y));line.SetPosition(1,P(r.xMax,y));}
            else {float x=i==2?r.xMin+.12f:r.xMax-.12f;line.SetPosition(0,P(x,r.yMin));line.SetPosition(1,P(x,r.yMax));}
        }
    }
    void PerimeterBarrage(int chapter,int beat)
    {
        Rect r=CombatRound.Border;int count=Dense(chapter==3?18:15);
        // Sources are the visibly charged margins while the creature is absent.
        for(int edge=0;edge<4;edge++)
        {
            if(chapter<3&&edge/2!=beat%2)continue;
            bool horizontal=edge<2;int sign=edge%2==0?1:-1;
            float gap=.5f+.28f*Mathf.Sin(beat*.67f+(horizontal?0:1.6f));
            for(int i=0;i<count;i++)
            {
                float u=(i+.5f)/count;
                if(Mathf.Abs(u-gap)<(chapter==3?.13f:.115f))continue;
                Vector3 p=horizontal?new Vector3(Mathf.Lerp(r.xMin+.2f,r.xMax-.2f,u),edge==0?r.yMax-.15f:r.yMin+.15f,1):
                    new Vector3(edge==2?r.xMin+.15f:r.xMax-.15f,Mathf.Lerp(r.yMin+.3f,r.yMax-.3f,u),1);
                // Never materialize a bullet on a pilot hugging an edge.
                if(CombatVisuals.Player!=null&&Vector2.Distance(p,CombatVisuals.Player.position)<1.6f)continue;
                Vector2 inward=horizontal?Vector2.down*sign:Vector2.right*sign;
                if(chapter==1)
                {
                    if(i%3==0)CombatRound.Launch<SuspendedRound>(p,inward,2.4f,.23f).FadeAt(6);
                    else CombatRound.Launch<NeedleRound>(p,CombatVisuals.Rotate(inward,sign*(beat%2==0?12:-12)),2.5f,.17f).Ramp(1.25f,1.0f,.7f).FadeAt(7);
                }
                else if(chapter==2)
                {
                    if(i%3==0)CombatRound.Launch<ReturningDisc>(p,CombatVisuals.Rotate(inward,(i%2==0?1:-1)*18),2.5f,.23f).FadeAt(5.5f);
                    else CombatRound.Launch<RicochetBolt>(p,inward,2.0f,.18f).Pause(.9f,.32f,CombatRound.Turn.Rotate,(i%2==0?1:-1)*38,1.6f).FadeAt(6);
                }
                else
                {
                    var axis=CombatVisuals.Rotate(inward,Mathf.Sin(u*6.28f+beat*.7f)*22);
                    if(i%7==0&&beat%3==0)CombatRound.Launch<PlasmaOrb>(p,axis,1.4f,.32f).FadeAt(6);
                    else if(i%3==0)CombatRound.Launch<SimpleRound>(p,axis,2.8f,.23f).Pause(1.1f,.3f,CombatRound.Turn.Rotate,sign*28,1.3f).FadeAt(6);
                    else CombatRound.Launch<NeedleRound>(p,axis,1.8f,.17f).Zigzag(1.1f,12).FadeAt(6.5f);
                }
            }
        }
    }
}
