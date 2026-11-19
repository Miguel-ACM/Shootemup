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
    Vector3 restingScale;
    public bool Teleporting => teleporting;
    protected override bool PatternEmissionAllowed => !changingForm && !Hidden && !teleporting;
    public override bool CanBeTargeted => !changingForm && !Hidden && !teleporting && !submergedMemory && base.CanBeTargeted;
    public override bool CanReceiveDamage => !changingForm && !Hidden && !teleporting && !submergedMemory && base.CanReceiveDamage;

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
        restingScale = Hull.localScale;
        BuildFracture();
        perch = station;
    }

    static float RiftFloor(Rect arena) => arena.yMax - arena.height / 3f;

    Vector3 NextPerch()
    {
        var r = CombatRound.Border;
        float floor = RiftFloor(r);
        float ceiling = Mathf.Max(floor, r.yMax - 2.8f);
        var player = CombatVisuals.Player;
        Vector3 fallback = transform.position;
        float bestClearance = -1;
        for (int attempt = 0; attempt < Perches.Length; attempt++)
        {
            var p = Perches[teleportStep++ % Perches.Length];
            var target = new Vector3(Mathf.Lerp(r.xMin+2.4f,r.xMax-2.4f,(p.x+1)*.5f),
                Mathf.Lerp(floor,ceiling,p.y),1);
            float travel = Vector2.Distance(target,transform.position);
            float clearance = player == null ? travel : Vector2.Distance(target,player.position);
            if (clearance > 3.4f && travel > 2.2f) return target;
            if (clearance > bestClearance)
            {
                fallback = target;
                bestClearance = clearance;
            }
        }
        return fallback;
    }

    void Dissolve(float amount)
    {
        foreach (var part in anatomy)
        {
            if (!part.gameObject.activeInHierarchy) continue;
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
        Vector3 from = transform.position, to = NextPerch();
        Vector2 heading = ((Vector2)(to-from)).normalized;
        float side = heading.x >= 0 ? 1 : -1;
        Vector3 bend = new Vector3(-heading.y,heading.x) * side *
            Mathf.Min(1.45f,Vector2.Distance(from,to)*.25f);
        bend.y = Mathf.Min(bend.y, Mathf.Max(0, CombatRound.Border.yMax - 1.95f - Mathf.Max(from.y,to.y)));
        Quaternion initial = Hull.localRotation;
        bool released = false;
        departure.Burst(from,true);
        for (float t=0; t<1.65f;)
        {
            if (!GameRules.playerDisabled && !WorldTimeStop.Frozen) t+=Time.deltaTime;
            float wind = Mathf.SmoothStep(0,1,t/.37f);
            float dash = Mathf.SmoothStep(0,1,Mathf.InverseLerp(.38f,.99f,t));
            float land = Mathf.InverseLerp(.87f,1.54f,t);
            float recoil = Mathf.Sin(land*Mathf.PI*2)*Mathf.Exp(-2.8f*land);
            float tuck = wind*(1-Mathf.SmoothStep(0,1,land));
            Vector3 position = VoidRiftVisual.Crossing(from,to,bend,dash)
                + (Vector3)heading * (-.38f*wind*(1-dash)+.52f*recoil);
            position.y = Mathf.Max(position.y, RiftFloor(CombatRound.Border));
            transform.position = position;
            float streak = Mathf.Sin(dash*Mathf.PI);
            Hull.localRotation = Quaternion.Slerp(initial,Quaternion.identity,wind) *
                Quaternion.Euler(7*streak,side*(35*tuck-18*recoil),side*(-22*tuck-13*recoil));
            Hull.localScale = Vector3.Scale(restingScale,new Vector3(
                1-.70f*tuck+.22f*recoil,1+.38f*tuck-.14f*recoil,1));
            body.Fold = tuck;
            Dissolve(.70f*tuck);
            if (!released && t>=.94f)
            {
                released=true;
                arrival.Burst(to,false);
            }
            float pull = Mathf.Clamp01(t/.62f);
            departure.Shock(from,Mathf.Lerp(2.6f,.15f,pull),.66f*Mathf.Sin(pull*Mathf.PI));
            arrival.Shock(to,.2f+3.2f*land,Mathf.Sin(land*Mathf.PI)*.78f);
            departure.Bolt(from,to,bend,dash);
            departure.Pose(from,Mathf.Sin(Mathf.Clamp01(t/.8f)*Mathf.PI),t,false,-side*19);
            arrival.Pose(to,Mathf.Sin(Mathf.InverseLerp(.24f,1.65f,t)*Mathf.PI),t,true,side*21);
            yield return null;
        }
        transform.position = perch = to;
        flightStart = motionAge;
        CancelTeleport();
        contact.enabled = true;
    }

    void CancelTeleport()
    {
        teleporting=false;
        if (departure == null) return;
        departure.Hide();
        arrival.Hide();
        Dissolve(0);
        body.Fold = 0;
        Hull.localRotation = Quaternion.identity;
        Hull.localScale = restingScale;
    }
}
