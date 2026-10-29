using UnityEngine;

// Scenery hazard: no enemy registry entry, reward or wave-clear requirement.
public sealed class VolcanicVent : MonoBehaviour
{
    public float phase;
    float readyAt = -1, next, charge, embers;
    ParticleSystem fire;
    public int Eruptions
    {
        get; private set;
    }

    void Start()
    {
        fire = PSXImpactEffects.Create(transform, "Crater fire", 80, true);
        var renderer = fire.GetComponent<ParticleSystemRenderer>();
        renderer.sortingLayerName = "Background";
        renderer.sortingOrder = -34;
    }

    void Update()
    {
        if (fire == null)
            return;
        float dt = Time.deltaTime;
        var p = transform.position;
        bool visible = p.y > Camera.DownLimit + 1.8f && p.y < Camera.UpLimit - 1.2f && Mathf.Abs(p.x) < 5.2f;
        bool combat = visible && !GameRules.playerDisabled && !CloudTransition.Busy && WavePreparation.Ready && LevelRun.Current == LevelId.Hell;
        foreach (var enemy in AEnemy.Live)
            if (enemy is DistrictBoss)
            {
                combat = false;
                break;
            }
        if (!combat)
        {
            readyAt = -1;
            charge = 0;
            return;
        }
        if (readyAt < 0)
        {
            readyAt = Time.time + WaveOrchestrer.OpeningBreather;
            next = readyAt + .8f + phase * .22f;
        }
        charge = Mathf.Clamp01(1 - (next - Time.time) / 1.15f);
        embers += dt * (3 + charge * 12);
        while (embers >= 1)
        {
            embers--;
            var e = new ParticleSystem.EmitParams();
            e.position = p + new Vector3(Random.Range(-.14f, .14f), Random.Range(-.12f, .12f), -1.02f);
            e.velocity = new Vector3(Random.Range(-.25f, .25f), .6f + charge * 1.1f, -.15f);
            float size = Random.Range(.09f, .18f) * (1 + charge * .35f);
            e.startSize3D = new Vector3(size, size * Mathf.Lerp(1.2f, 2f, charge), size);
            e.startLifetime = Mathf.Lerp(.55f, .75f, charge);
            e.startColor = Color.Lerp(new Color(.34f, .095f, .025f, .28f), new Color(.62f, .25f, .055f, .48f), charge);
            fire.Emit(e, 1);
        }
        if (Time.time < next || Time.time < readyAt)
            return;
        next = Time.time + 9.5f + phase;
        Eruptions++;
        var axis = CombatVisuals.Rotate(Vector2.down, Mathf.Sign(p.x) * -24);
        int count = 5;
        for (int i = 0; i < count; i++)
            CombatRound.Launch<SimpleRound>(p, CombatVisuals.Rotate(axis, (i - (count - 1) * .5f) * 25), 2.5f, .24f);
        SoundManager.PlaySound("enemyExplosion1", .055f);
    }
}
