using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed partial class SpaceCitadelBoss : ArmoredHullBoss
{
    // The luminous disk and lensed horizon form one body, inside the shader's transparent quad.
    static readonly Vector2[] outline = { new(0,1.12f), new(.65f,.96f), new(.96f,.60f),
        new(2.65f,.34f), new(2.8f,0), new(2.65f,-.34f), new(.96f,-.60f), new(.65f,-.96f),
        new(0,-1.12f), new(-.65f,-.96f), new(-.96f,-.60f), new(-2.65f,-.34f), new(-2.8f,0),
        new(-2.65f,.34f), new(-.96f,.60f), new(-.65f,.96f) };
    protected override Vector2[] Outline => outline;
    public Transform CoreVisual { get; private set; }
    protected override void Configure()
    {
        hullScale = 1;
        health = 2500;
        attackInterval = .68f;
        nextAttack = 1.7f;
    }

    protected override void Start()
    {
        base.Start();
        GetComponent<CombatHullVisual>().enabled = false;
        var core = new GameObject("Dark event horizon");
        core.transform.SetParent(Hull, false);
        core.transform.localPosition = new Vector3(0, 0, -.38f);
        core.AddComponent<MeshFilter>().sharedMesh = IndustrialMeshFactory.CitadelReactor();
        var renderer = core.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = SpaceBlackHoleVisual.Core;
        renderer.sortingLayerName = "Enemy";
        CoreVisual = core.transform;
        PartHitFeedback.Bind(transform, false);
        firingCue.Register(CoreVisual, Vector3.back * .25f);
    }

    // The core's flung rings mix spinning crescents with arrows.
    CombatRound MoonShot(bool moon, Vector3 port, Vector2 axis, float speed)
        => moon ? (CombatRound)Fire<CrescentRound>(port, axis, speed, .19f) : Fire<NeedleRound>(port, axis, speed, .19f);

    protected override void PressureAttack() { }
    protected override void MoveBoss(float dt)
    {
        // Inward fronts are circles centred on the black hole: it holds still while one closes.
        if (closing > 0)
            return;
        station = new Vector3(Mathf.Sin(motionAge * .43f) * .82f, Camera.UpLimit - 3 + Mathf.Sin(motionAge * .31f) * .32f, 1);
        transform.position = Vector3.MoveTowards(transform.position, station, dt * 3.2f);
    }

    protected override void OnPhaseTwo()
    {
        // The phase break stops every front still closing in.
        closing = 0;
        CloseWell();
        volley = 0;
        nextAttack = age + 1.2f;
    }

    protected override void OnPhaseThree()
    {
        // The phase break stops every front still closing in.
        closing = 0;
        CloseWell();
    }

    protected override void Die()
    {
        CloseWell();
        base.Die();
    }
}
