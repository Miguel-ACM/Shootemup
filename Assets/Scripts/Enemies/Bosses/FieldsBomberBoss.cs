using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed partial class FieldsBomberBoss : DistrictBoss
{
    // Follow the actual wing cutouts, engine shoulders, barrels and long nose.
    static readonly Vector2[] outline ={new Vector2(-1.7f,.5f),new Vector2(-.58f,1.1f),new Vector2(0,.8f),new Vector2(.58f,1.1f),new Vector2(1.7f,.5f),
        new Vector2(1.48f,-.08f),new Vector2(1.52f,-.72f),new Vector2(1.28f,-.72f),new Vector2(1.28f,-.58f),new Vector2(.75f,-.9f),
        new Vector2(.45f,-.62f),new Vector2(.28f,-.95f),new Vector2(0,-1.13f),new Vector2(-.28f,-.95f),new Vector2(-.45f,-.62f),
        new Vector2(-.75f,-.9f),new Vector2(-1.28f,-.58f),new Vector2(-1.28f,-.72f),new Vector2(-1.52f,-.72f),new Vector2(-1.48f,-.08f)};
    PolygonCollider2D hullCollider;
    readonly Vector2[] collisionPoints = new Vector2[outline.Length];
    protected override void Start()
    {
        base.Start();
        var oldCollider = GetComponent<CircleCollider2D>();
        oldCollider.enabled = false;
        Destroy(oldCollider);
        hullCollider = gameObject.AddComponent<PolygonCollider2D>();
        hullCollider.isTrigger = true;
        UpdateHitbox();
    }

    void LateUpdate()
    {
        UpdateHitbox();
    }

    void UpdateHitbox()
    {
        if (hullCollider == null || Hull == null)
            return;
        for (int i = 0; i < outline.Length; i++)
            collisionPoints[i] = transform.InverseTransformPoint(Hull.TransformPoint(outline[i]));
        hullCollider.SetPath(0, collisionPoints);
    }

    static readonly Vector3[] bays ={new Vector3(-1.19f,-.24f,-.51f),new Vector3(-.97f,-.36f,-.51f),new Vector3(-.75f,-.48f,-.51f),
        new Vector3(.75f,-.48f,-.51f),new Vector3(.97f,-.36f,-.51f),new Vector3(1.19f,-.24f,-.51f)};
    Vector2 phraseAxis = Vector2.down;
    protected override void Configure()
    {
        hull = 23;
        hullScale = 1.4f;
        health = 1200;
        attackInterval = (.8f);
        shellTravelDistance = 4.6f;
    }

    protected override void MoveBoss(float dt)
    {
        station.x = Mathf.Sin(motionAge * .5f) * 2;
        base.MoveBoss(dt);
    }

    protected override void PressureAttack()
    {
    } // One score owns every battery and its rests.
    protected override void Attack()
    {
        if (Difficulty.Hard) { AttackHard(); return; }
        int beat = (volley - 1) % 12;
        if (beat == 0 || beat == 7)
            phraseAxis = Aim;
        if (beat < 2)
            for (int side = -1; side <= 1; side += 2)
            {
                var port = Hull.TransformPoint(new Vector3(side * 1.34f, .32f, -.36f));
                var axis = CombatVisuals.Rotate(Vector2.down, side * (28 + beat * 16));
                PSXImpactEffects.Muzzle(port, axis);
                PetalFan<SimpleRound>(port, axis, (phaseTwo ? 6 : 5), 22, 2.5f, .27f, .2f);
            }
        else if (beat == 3)
            FanFrom<BurstShell>((phaseTwo ? 3 : 2), 32, 3.1f, .46f, bays);
        else if (beat == 4)
            FanFrom<ScatterCarrier>((phaseTwo ? 2 : 1), 32, 2.6f, .33f, bays);
        else if (beat >= 7 && beat <= 9)
            for (int side = -1; side <= 1; side += 2)
            {
                var port = Hull.TransformPoint(new Vector3(side * 1.4f, -.68f, -.16f));
                var axis = CombatVisuals.Rotate(phraseAxis, side * (24 - (beat - 7) * 12));
                PSXImpactEffects.Muzzle(port, axis);
                PetalFan<NeedleRound>(port, axis, (phaseTwo ? 6 : 4), 19, 4.0f, .19f);
            }
        else if (beat == 10 && phaseTwo)
            PetalFan<SimpleRound>(Muzzle(4), Vector2.down, (7), 25, 1.8f, .33f, .22f);
        // Slow wing petals, a delivered bloom, then three committed ribbon answers.
    }

    protected override void PrepareFire(float seconds)
    {
        base.PrepareFire(seconds);
        firingCue.ClearWarning();
        int beat = volley % 12;
        if (beat < 2)
        {
            Cue(Muzzle(0), seconds);
            Cue(Muzzle(1), seconds);
        }
        else if (beat == 3 || beat == 4)
        {
            int count = beat == 3 ? (phaseTwo ? 3 : 2) : (phaseTwo ? 2 : 1);
            for (int i = 0; i < count; i++)
            {
                int port = count == 1 ? volley % bays.Length : Mathf.RoundToInt(i * (bays.Length - 1f) / (count - 1));
                Cue(Hull.TransformPoint(bays[port]), seconds);
            }
        }
        else if (beat == 10 && phaseTwo)
            Cue(Muzzle(4), seconds);
        else if (beat >= 7 && beat <= 9)
        {
            Cue(Muzzle(2), seconds);
            Cue(Muzzle(3), seconds);
        }
    }

    protected override void OnPhaseTwo()
    {
        volley = 0;
        nextAttack = age + 1;
    }
}
