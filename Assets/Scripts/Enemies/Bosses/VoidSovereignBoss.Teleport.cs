using System.Collections;
using UnityEngine;

public sealed partial class VoidSovereignBoss
{
    VoidRiftVisual departure, arrival;
    bool teleporting;
    int teleportStep;
    Vector3 perch;
    MaterialPropertyBlock riftInk;
    MeshRenderer[] anatomy;
    public bool Teleporting => teleporting;
    protected override bool PatternEmissionAllowed => !Hidden && !teleporting;
    public override bool CanBeTargeted => !Hidden && !teleporting && base.CanBeTargeted;
    public override bool CanReceiveDamage => !Hidden && !teleporting && base.CanReceiveDamage;

    static readonly Vector2[] Perches = {
        new Vector2(-.78f,.76f), new Vector2(.78f,.64f), new Vector2(-.70f,.32f),
        new Vector2(.70f,.86f), new Vector2(-.38f,.88f), new Vector2(.68f,.29f),
        new Vector2(-.78f,.62f), new Vector2(0,.54f)
    };

    void BuildRifts()
    {
        riftInk=new MaterialPropertyBlock();
        // Parent them outside the hideable hull; no object creation on a teleport beat.
        departure = VoidRiftVisual.Create(transform);
        arrival = VoidRiftVisual.Create(transform);
        anatomy = Hull.GetComponentsInChildren<MeshRenderer>(true);
        perch = station;
    }

    Vector3 NextPerch()
    {
        var r = CombatRound.Border;
        for (int attempt = 0; attempt < Perches.Length; attempt++)
        {
            var p = Perches[teleportStep++ % Perches.Length];
            var target = new Vector3(Mathf.Lerp(r.xMin+2.4f,r.xMax-2.4f,(p.x+1)*.5f),
                Mathf.Lerp(r.yMin+3.2f,r.yMax-2.8f,p.y),1);
            if ((CombatVisuals.Player == null || Vector2.Distance(target,CombatVisuals.Player.position)>3.4f) &&
                Vector2.Distance(target,transform.position)>2.2f) return target;
        }
        return new Vector3(0,r.yMax-3.1f,1);
    }

    void Dissolve(float amount)
    {
        foreach (var part in anatomy)
        {
            part.GetPropertyBlock(riftInk);
            riftInk.SetFloat("_Rift",amount);
            part.SetPropertyBlock(riftInk);
        }
    }

    IEnumerator Teleport()
    {
        teleporting = true;
        contact.enabled = false;
        firingCue.ClearWarning();
        var from = transform.position;
        var to = NextPerch();
        // Both ends are visible before the body moves. The arrival gets a complete
        // breathing beat before any projectile or physical contact can resume.
        for (float t=0;t<1.65f;)
        {
            if (!GameRules.playerDisabled && !WorldTimeStop.Frozen) t+=Time.deltaTime;
            float leave=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/.55f));
            float enter=Mathf.SmoothStep(0,1,Mathf.Clamp01((t-.72f)/.48f));
            if(t<.66f) Dissolve(leave);
            else
            {
                transform.position=to;
                Dissolve(1-enter);
            }
            departure.Pose(from,Mathf.Sin(Mathf.Clamp01(t/.88f)*Mathf.PI),t,false);
            arrival.Pose(to,Mathf.Sin(Mathf.Clamp01((t-.14f)/1.51f)*Mathf.PI),t,true);
            yield return null;
        }
        perch=to;
        Dissolve(0);
        departure.Hide();arrival.Hide();
        contact.enabled=true;
        teleporting=false;
    }

    void CancelTeleport()
    {
        teleporting=false;
        if(departure!=null){departure.Hide();arrival.Hide();Dissolve(0);}
    }
}
