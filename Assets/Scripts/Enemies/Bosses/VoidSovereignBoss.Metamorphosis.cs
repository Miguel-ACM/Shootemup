using System.Collections;
using UnityEngine;

public sealed partial class VoidSovereignBoss
{
    const int BladeCount = 10, SeamPoints = 8;
    readonly Transform[] fragments = new Transform[BladeCount];
    readonly LineRenderer[] seams = new LineRenderer[BladeCount];
    readonly Vector3[] seamPoints = new Vector3[SeamPoints];
    Transform fracture;
    bool changingForm;
    float flightStart;
    Quaternion foldBasis;

    void BuildFracture()
    {
        fracture = new GameObject("Fractured mantle").transform;
        fracture.SetParent(transform,false);
        var crystal = new MaterialPropertyBlock();
        crystal.SetFloat("_Crystal",1);
        Color color = LevelProjectilePalette.Accent(LevelId.Void);
        for (int i=0; i<fragments.Length; i++)
        {
            var plate = new GameObject("Mantle blade").transform;
            plate.SetParent(fracture,false);
            plate.gameObject.AddComponent<MeshFilter>().sharedMesh = IndustrialMeshFactory.VoidSplinter();
            var renderer = plate.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = Body;
            renderer.sortingLayerName = "Enemy";
            renderer.SetPropertyBlock(crystal);
            fragments[i] = plate;
            var seam = seams[i] = CombatVisuals.Line(fracture,color,.07f);
            seam.sortingLayerName = "Enemy";
            seam.sortingOrder = -1;
            seam.useWorldSpace = false;
            seam.positionCount = SeamPoints;
            seam.startWidth = .012f;
            seam.endWidth = .075f;
            seam.startColor = new Color(color.r,color.g,color.b,0);
            seam.endColor = Color.Lerp(color,Color.white,.28f);
        }
        fracture.gameObject.SetActive(false);
    }

    static Vector3 BladePosition(float t,int i,float turn)
    {
        bool inner = (i & 1) != 0;
        float radius = (.55f + 2.1f*Mathf.Sin(t*Mathf.PI) + .45f*t) * (inner ? .75f : 1.15f);
        float a = i*(2*Mathf.PI/BladeCount) + turn*t*2.55f;
        return new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius*.76f,-.50f-.38f*Mathf.Sin(a)-(inner?.18f:0));
    }

    // The blades lift from the hull, describe a turning iris in depth, then close
    // edge-on. The incoming body unfolds through the same aperture in reverse.
    void Fold(float closed, bool opening)
    {
        float t = Mathf.Clamp01(closed);
        float bloom = Mathf.Sin(t * Mathf.PI);
        float hand = (CurrentPhase % 2 == 0 ? 1 : -1) * (opening ? -1 : 1);
        float size = Mathf.Pow(Mathf.Max(0,bloom),.65f);
        fracture.gameObject.SetActive(t > .002f && t < .998f);
        for (int i=0; i<fragments.Length; i++)
        {
            bool inner = (i & 1) != 0;
            float turn = (inner ? -.78f : 1) * hand;
            float a = i * (2*Mathf.PI/fragments.Length) + turn*t*2.55f;
            var radial = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            var plate = fragments[i];
            plate.localPosition = BladePosition(t,i,turn);
            for (int j=0; j<SeamPoints; j++)
            {
                float delay = (1-j/(SeamPoints-1f))*.17f*bloom;
                seamPoints[j] = BladePosition(Mathf.Clamp01(t + (opening ? delay : -delay)),i,turn);
            }
            seams[i].SetPositions(seamPoints);
            plate.localRotation = Quaternion.Euler(16*radial.y,turn*(t*120-32),
                a*Mathf.Rad2Deg-90 + turn*(24+47*bloom));
            plate.localScale = Vector3.one * size * (inner ? .77f : 1.07f);
        }
        body.Fold = t;
        float punch = opening ? .16f*bloom*Mathf.Cos(t*2*Mathf.PI) : 0;
        Hull.localScale = Vector3.Scale(restingScale,new Vector3(
            Mathf.Lerp(1,.045f,t)+punch, Mathf.Lerp(1,.42f,t)+.36f*bloom, 1));
        Hull.localRotation = foldBasis * Quaternion.Euler(8*bloom,hand*42*t,hand*31*t);
        Dissolve(t);
    }

    void UnfoldAt(Vector3 destination)
    {
        Dissolve(0);
        transform.position = perch = destination;
        Hull.localRotation = Quaternion.identity;
        SetForm();
        Hull.gameObject.SetActive(true);
        foldBasis = Hull.localRotation;
        Fold(1,true);
        arrival.Burst(destination,false);
    }

    IEnumerator Unfold(float seconds)
    {
        for (float t=0; t<1;)
        {
            if (!GameRules.playerDisabled && !WorldTimeStop.Frozen)
                t = Mathf.Min(1,t+Time.deltaTime/seconds);
            Fold(1-Mathf.SmoothStep(0,1,t),true);
            yield return null;
        }
        arrival.Hide();
        flightStart = motionAge;
        changingForm = false;
        contact.enabled = true;
    }

    IEnumerator ChangeForm()
    {
        Vector3 start = transform.position;
        Vector3 end = new Vector3(0,Camera.UpLimit-3.25f,1);
        foldBasis = Hull.localRotation;
        for (float t=0; t<1;)
        {
            if (!GameRules.playerDisabled && !WorldTimeStop.Frozen)
                t = Mathf.Min(1,t+Time.deltaTime/.68f);
            float turn = Mathf.SmoothStep(0,1,t);
            transform.position = Vector3.Lerp(start,end,turn) + Vector3.up*(.38f*Mathf.Sin(turn*Mathf.PI));
            Fold(turn,false);
            yield return null;
        }
        UnfoldAt(end);
        yield return Unfold(.76f);
    }
}
