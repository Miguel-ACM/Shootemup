using UnityEngine;

// Lighthouse: a warm beam turns slowly around the ship, one full turn every three seconds.
// Hostile rounds caught in its light are erased and enemies it crosses are scorched. A second
// copy adds a second beam on the opposite side.
[UnityEngine.Scripting.Preserve]
public sealed class Lighthouse : AItem
{
    void Awake()
    {
        SetData("Lighthouse", "Keep the light on", "A beam sweeps around you, erasing fire and burning enemies.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.Lighthouse);
        if (s.GetComponent<LighthouseBeam>() == null)
            s.gameObject.AddComponent<LighthouseBeam>();
        base.OnPickup();
    }
}

public sealed class LighthouseBeam : MonoBehaviour
{
    // The beam reaches 70% farther than its first 3.4 units (Miguel, 15 November 2026).
    const float Length = 5.8f, BaseWidth = .08f, TipWidth = .62f, TurnSpeed = 120, TickRate = .1f, TickPower = .8f;
    static readonly Color Light = new Color(1, .93f, .68f);
    readonly LineRenderer[] beams = new LineRenderer[2];
    readonly System.Collections.Generic.List<CombatRound> caught = new System.Collections.Generic.List<CombatRound>();
    readonly System.Collections.Generic.List<AEnemy> lit = new System.Collections.Generic.List<AEnemy>();
    Shipcontroller ship;
    float angle = 90, tick;

    static int Copies => CombatUpgrades.Current != null ? CombatUpgrades.Current.Count(UpgradeKind.Lighthouse) : 0;

    void Start()
    {
        ship = GetComponent<Shipcontroller>();
        for (int i = 0; i < beams.Length; i++)
        {
            var beam = CombatVisuals.Line(null, Light, 1);
            beam.name = "Lighthouse beam";
            beam.transform.SetParent(transform, false);
            beam.useWorldSpace = true;
            beam.sortingLayerName = "Player";
            beam.sortingOrder = -3;
            beam.widthCurve = AnimationCurve.Linear(0, BaseWidth, 1, TipWidth);
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Light, 1) },
                new[] { new GradientAlphaKey(.5f, 0), new GradientAlphaKey(.22f, .6f), new GradientAlphaKey(0, 1) });
            beam.colorGradient = gradient;
            beams[i] = beam;
        }
    }

    void OnDestroy()
    {
        foreach (var beam in beams)
            if (beam != null)
                Destroy(beam.gameObject);
    }

    void Update()
    {
        int copies = Copies;
        bool shining = copies > 0 && ship != null && ship.health > 0 && GameClock.ActivePlay;
        for (int i = 0; i < beams.Length; i++)
            beams[i].enabled = shining && i < copies;
        if (!shining)
            return;
        float dt = GameClock.PlayerDelta;
        angle = Mathf.Repeat(angle - TurnSpeed * dt, 360);
        tick -= dt;
        bool scorch = tick <= 0;
        if (scorch)
            tick = TickRate;
        Vector2 from = transform.position;
        for (int i = 0; i < Mathf.Min(copies, beams.Length); i++)
        {
            var direction = (Vector2)(Quaternion.Euler(0, 0, angle + i * 180) * Vector3.right);
            var to = from + direction * Length;
            beams[i].positionCount = 2;
            beams[i].SetPosition(0, new Vector3(from.x, from.y, -.2f));
            beams[i].SetPosition(1, new Vector3(to.x, to.y, -.2f));
            Erase(from, direction);
            if (scorch)
                Scorch(from, to);
        }
    }

    // Rounds inside the widening cone of light vanish.
    void Erase(Vector2 from, Vector2 direction)
    {
        caught.Clear();
        foreach (var round in CombatRound.Live)
        {
            if (!BulletReflection.CanReflect(round))
                continue;
            var offset = (Vector2)round.transform.position - from;
            float along = Vector2.Dot(offset, direction);
            if (along < .2f || along > Length)
                continue;
            float half = Mathf.Lerp(BaseWidth, TipWidth, along / Length) * .5f;
            if (Mathf.Abs(offset.x * direction.y - offset.y * direction.x) <= half)
                caught.Add(round);
        }
        foreach (var round in caught)
        {
            ItemSpark.Emit(round.transform.position, Light, 1, .05f);
            round.Vanish();
        }
        if (caught.Count > 0)
            RunInventory.Ping(UpgradeKind.Lighthouse);
    }

    void Scorch(Vector2 from, Vector2 to)
    {
        lit.Clear();
        foreach (var enemy in AEnemy.Live)
            if (enemy != null && enemy.CanBeTargeted && enemy.CanReceiveDamage && BeamContact.Touches(enemy, from, to, .2f, out _))
                lit.Add(enemy);
        foreach (var enemy in lit)
            if (enemy != null && BeamContact.Touches(enemy, from, to, .2f, out var point))
                CombatHit.Apply(enemy, GameRules.playerShootDamage * TickPower, point, (point - from).normalized, generation: 1);
    }
}
