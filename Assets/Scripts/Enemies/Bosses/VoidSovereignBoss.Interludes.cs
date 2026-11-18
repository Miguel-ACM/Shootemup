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
                DrawEdgeTell(chapter,Mathf.Clamp01(InterludeElapsed/.9f));
                if(InterludeElapsed>=beatAt&&InterludeElapsed<13.8f)
                {
                    PerimeterBarrage(chapter,beat++);
                    beatAt+=Rest(chapter==1?.49f:chapter==2?.47f:.46f);
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
    // One side owns the entire disappearance. The next disappearance takes the
    // next side clockwise; old rounds are cleared before that new phrase begins.
    static int SourceEdge(int chapter)=>(chapter-1)%4;
    static Vector2 Inward(int edge)=>edge==0?Vector2.down:edge==1?Vector2.left:edge==2?Vector2.up:Vector2.right;
    static Vector3 Source(int edge,int side)
    {
        Rect r=CombatRound.Border;float u=side<0?.27f:.73f;
        if(edge==0||edge==2)return new Vector3(Mathf.Lerp(r.xMin,r.xMax,u),edge==0?r.yMax-.3f:r.yMin+.3f,1);
        return new Vector3(edge==1?r.xMax-.3f:r.xMin+.3f,Mathf.Lerp(r.yMin,r.yMax,u),1);
    }
    void DrawEdgeTell(int chapter,float charge)
    {
        var ink=LevelProjectilePalette.Accent(LevelId.Void);ink.a=.12f+charge*.62f;
        int edge=SourceEdge(chapter);Vector2 inward=Inward(edge);
        Vector3 tangent=new Vector3(-inward.y,inward.x,0);
        for(int i=0;i<4;i++)
        {
            // Two small chevrons identify the exact mouths, rather than lighting
            // unrelated margins. Both mouths always belong to the same edge.
            var line=edgeTell[i];line.enabled=true;
            line.startColor=line.endColor=ink;
            Vector3 p=Source(edge,i<2?-1:1);p.z=-1.1f;
            line.SetPosition(0,p+tangent*(i%2==0?-.6f:.6f));
            line.SetPosition(1,p+(Vector3)inward*(.2f+charge*.45f));
        }
    }
    // Tones (Miguel, 9 November 2026), a different split for each disappearance:
    //   Funeral fans: the left mouth fans bright, the right mouth dark.
    //   Scissor brocade: whole waves alternate bright and dark.
    //   Unmaking: fast crests bright, slow troughs dark, so the scallops read.
    // Giant crowns and aimed or slow replies take the tone opposite their wave.
    static HostileTone WaveTone(int chapter,int beat,int side,float speed)
        =>chapter==1?(side<0?HostileTone.Bright:HostileTone.Dark):
          chapter==2?(beat%2==0?HostileTone.Bright:HostileTone.Dark):
          speed>=3.0f?HostileTone.Bright:HostileTone.Dark;
    void PerimeterBarrage(int chapter,int beat)
    {
        if(beat%6==5)return;
        Weave(chapter,beat);
        CombatRound.LaunchTone=HostileTone.Bright;
    }
    void Weave(int chapter,int beat)
    {
        int edge=SourceEdge(chapter),count=Dense(chapter==3?17:15);
        bool crown=beat%4==0;
        Vector2 inward=Inward(edge);
        for(int side=-1;side<=1;side+=2)
        {
            Vector3 p=Source(edge,side);
            if(CombatVisuals.Player!=null&&Vector2.Distance(p,CombatVisuals.Player.position)<1.6f)continue;
            // Unmaking's crests carry both tones, so its punctuating moon is dark.
            // Void fires nothing aimed (Miguel, 10 November 2026): a straight inward moon.
            CombatRound.LaunchTone=chapter==3?HostileTone.Dark:Other(WaveTone(chapter,beat,side,0));
            if(beat%7==0)
                CombatRound.Launch<CrescentRound>(p,inward,2.8f,.22f).LivesFor(12);
            for(int i=0;i<count;i++)
            {
                float u=Across(i,count);
                // A continuous slit travels with the fabric, not a random omitted round.
                float seam=.34f*Mathf.Sin(beat*.19f);
                if(Mathf.Abs(u-seam)<.24f||CrownSpacer(i,count,crown))continue;
                if(chapter==1)
                {
                    // Funeral fans: mirrored peacock feathers open as one moving wavefront.
                    float angle=side*(u*57+Mathf.Sin(beat*.16f)*22);
                    var shade=WaveTone(chapter,beat,side,0);
                    CombatRound.LaunchTone=crown&&i==count-1?Other(shade):shade;
                    WovenShot(i,crown&&i==count-1,chapter,p,CombatVisuals.Rotate(inward,angle),
                        3.0f+.9f*(1-u*u)).Ramp(1.25f,1.1f,1).FadeAt(crown&&i==count-1?5.8f:9);
                }
                else if(chapter==2)
                {
                    // Scissor brocade: all strands hinge together, preserving the ordered comb.
                    float angle=side*(u*51+24*Mathf.Sin(beat*.21f));
                    var shade=WaveTone(chapter,beat,side,0);
                    CombatRound.LaunchTone=crown&&i==count-1?Other(shade):shade;
                    WovenShot(i,crown&&i==count-1,chapter,p,CombatVisuals.Rotate(inward,angle),
                        2.7f+.6f*(1-u*u))
                        .Pause(.85f,.20f,CombatRound.Turn.Rotate,-side*26,1.2f).FadeAt(crown&&i==count-1?5.8f:9);
                    CombatRound.LaunchTone=Other(shade);
                    if(!crown&&beat%3==0&&i==count-1)
                        CombatRound.Launch<SimpleRound>(p,CombatVisuals.Rotate(inward,angle),1.65f,.22f).FadeAt(8);
                }
                else
                {
                    // Unmaking: opposed, scalloped fronts interleave fast crests and slow troughs.
                    float angle=side*(u*64+18*Mathf.Sin(beat*.2f));
                    float speed=3.0f+.75f*Mathf.Cos(u*Mathf.PI*2+beat*.22f);
                    var shade=WaveTone(chapter,beat,side,speed);
                    CombatRound.LaunchTone=crown&&i==count-1?Other(shade):shade;
                    WovenShot(i,crown&&i==count-1,chapter,p,CombatVisuals.Rotate(inward,angle),speed)
                        .Sway(.16f,.5f).Ramp(1.3f,1,1).FadeAt(crown&&i==count-1?5.8f:9);
                }
            }
        }
    }
    static CombatRound WovenShot(int lane,bool giant,int chapter,Vector3 p,Vector2 axis,float speed)
    {
        if(giant)
            return CombatRound.Launch<GiantOrb>(p,axis,speed*.9f,chapter==3?.72f:.62f);
        if(lane%4==2)
            return CombatRound.Launch<ChevronRound>(p,axis,speed,.19f);
        return lane%4==0?(CombatRound)CombatRound.Launch<SimpleRound>(p,axis,speed,.19f):CombatRound.Launch<NeedleRound>(p,axis,speed,.17f);
    }
}
