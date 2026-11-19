using UnityEngine;

public sealed class SporeBell : DistrictEnemy
{
    protected override void Configure() { health = 57; hullScale = .61f; }
    protected override void Attack()
    {
        if (transform.position.y > Camera.UpLimit - 3.8f && SporeCloud.Count < 2)
            SporeCloud.Drop(Muzzle(), volley * 2.39f + transform.position.x);
    }
}

public sealed class CapRicochet : DistrictEnemy
{
    Vector2 velocity;
    bool drifting;
    protected override void Configure() { health = 41; hullScale = .44f; }
    protected override void Attack()
    {
        int n = Dense(velocity.x > 0 ? 7 : 5);
        Vector2 inward = new Vector2(velocity.x * .13f, -.9f);
        for (int i = 0; i < n; i++)
            Fire<SimpleRound>(Muzzle(), CombatVisuals.Rotate(inward, (i - (n - 1) * .5f) * 18), 3.05f, .20f, i == 0)
                .Ramp(1.25f, .75f, .18f);
    }

    protected override void Update()
    {
        if (GameRules.playerDisabled || FrozenHull.Holds(gameObject) || WorldTimeStop.Frozen)
            return;
        float dt = Time.deltaTime * LevelRun.EnemyTempoScale;
        age += dt;
        if (!drifting)
        {
            if (movement is FormationFlight flight && age < .88f)
                return;
            Vector2 tangent = movement is FormationFlight still ? still.Tangent : Vector2.down;
            float side = Mathf.Abs(tangent.x) > .20f ? Mathf.Sign(tangent.x) : transform.position.x > 0 ? -1 : 1;
            velocity = new Vector2(side * 7.0f, -2.45f);
            if (movement != null)
                movement.enabled = false;
            drifting = true;
            UseTone(HostileTone.Bright);
        }
        Vector3 p = transform.position;
        float edge = Mathf.Max(.65f, Camera.RightLimit - .48f);
        p.x = Mathf.Clamp(p.x, -edge, edge);
        float wall = Mathf.Sign(velocity.x) * edge;
        float seconds = Mathf.Abs((wall - p.x) / velocity.x);
        bool visible = p.y < Camera.UpLimit - .42f && p.y > Camera.DownLimit + .65f;
        if (visible && seconds < .31f)
            Cue(Muzzle(), seconds + .03f);
        p += (Vector3)(velocity * dt * ChillMovement);
        if (Mathf.Abs(p.x) >= edge)
        {
            p.x = 2 * wall - p.x;
            velocity.x = -velocity.x;
            transform.position = p;
            if (visible) Attack();
        }
        transform.position = p;
        if (p.y < Camera.DownLimit - 1.4f || age > 18)
            Depart();
    }
}

public sealed class MyceliumLancer : DistrictEnemy
{
    protected override void Configure() { health = 59; hullScale = .56f; }
    protected override void Attack()
    {
        int n = Dense(8);
        for (int i = 0; i < n; i++)
        {
            float lane = i - (n - 1) * .5f;
            Fire<NeedleRound>(Muzzle(), CombatVisuals.Rotate(Aim, lane * 10), 3.0f + Mathf.Abs(lane) * .35f, .18f, i == 0)
                .Pause(.78f, .25f, CombatRound.Turn.Down, 0, 4.6f);
        }
    }
}

public sealed class PaperKite : DistrictEnemy
{
    protected override void Configure() { health = 42; hullScale = .48f; }
    protected override void Attack()
    {
        int folds = Dense(3);
        for (int side = -1; side <= 1; side += 2)
            for (int i = 0; i < folds; i++)
                Fire<CrescentRound>(Muzzle(), CombatVisuals.Rotate(Aim, side * (14 + 12 * i)),
                    3.45f + i * .22f, .23f, side < 0 && i == 0).Sway(side * .34f, .54f);
    }
}

public sealed class CrescentDancer : DistrictEnemy
{
    protected override void Configure() { health = 67; hullScale = .62f; }
    protected override void Attack()
    {
        int n = Dense(9);
        for (int i = 0; i < n; i++)
        {
            float lane = i - (n - 1) * .5f;
            Fire<NeedleRound>(Muzzle(i % 2), CombatVisuals.Rotate(Vector2.down, lane * 22), 2.66f, .25f, i < 2)
                .Pause(.65f, .33f, CombatRound.Turn.Rotate, -lane * 10, 4.35f).ShowTurn();
        }
    }
}

public sealed class PaintedMask : DistrictEnemy
{
    protected override void Configure() { health = 108; hullScale = .76f; }
    protected override void Attack()
    {
        int n = Dense(8);
        for (int i = 0; i < n; i++)
        {
            float angle = (i - (n - 1) * .5f) * 23;
            Fire<StarRound>(Muzzle(), CombatVisuals.Rotate(Vector2.up, angle), 4.7f, .26f, i == 0).Bounce(2);
        }
    }
}

public sealed class MushroomChampion : DistrictMiniboss
{
    readonly Transform[] cases = new Transform[2];
    float opening;

    internal void BindShell()
    {
        Hull.GetComponent<MeshFilter>().sharedMesh = IndustrialMeshFactory.StagBody();
        for (int i = 0; i < cases.Length; i++)
        {
            int side = i * 2 - 1;
            var plate = new GameObject(side < 0 ? "Port wing case" : "Starboard wing case");
            plate.transform.SetParent(Hull, false);
            plate.transform.localPosition = IndustrialMeshFactory.StagHinge(side);
            plate.AddComponent<MeshFilter>().sharedMesh = IndustrialMeshFactory.StagCase(side);
            var skin = plate.AddComponent<MeshRenderer>();
            skin.sharedMaterial = IndustrialMeshFactory.StagPaint;
            skin.sortingLayerName = "Enemy";
            cases[i] = plate.transform;
            firingCue.PlacePort(i, IndustrialMeshFactory.StagMuzzle(side), true, false);
        }
    }

    protected override void Update()
    {
        float previous = age;
        base.Update();
        float dt = age - previous;
        if (dt <= 0) return;
        float charge = Engaged && !Retreating ? Mathf.Clamp01(1 - (nextAttack - age) / .58f) : 0;
        opening = Mathf.MoveTowards(opening, charge, dt * (charge > opening ? 3 : 4.8f));
        for (int i = 0; i < cases.Length; i++)
        {
            int side = 2 * i - 1;
            float lift = 2 + 1.6f * Mathf.Sin(age * 4) + 24 * opening;
            cases[i].localRotation = Quaternion.Euler(0, side * lift, -side * opening * 21);
        }
    }

    protected override void Configure() { health = 485; hullScale = 1.04f; attackInterval = 1.38f; nextAttack = 1.1f; }
    protected override Vector3 Position(float t) =>
        new Vector3(2.65f * Mathf.Sin(t * 1.48f), Camera.UpLimit - 3.5f + .8f * Mathf.Cos(t * 2.96f), 1);

    protected override void Attack()
    {
        int move = (volley - 1) % 3;
        for (int s = -1; s <= 1; s += 2)
        {
            var port = Muzzle(s < 0 ? 0 : 1);
            if (move == 0)
                MirrorFan(port, new Vector2(s * .74f, -.75f), Dense(7), 15, 4.5f);
            else
            {
                int count = Dense(move == 1 ? 7 : 5);
                for (int i = 0; i < count; i++)
                    Fire<SimpleRound>(port, CombatVisuals.Rotate(Vector2.down, s * (24 + i * 13)), 3.1f, move == 1 ? .21f : .29f)
                        .Bounce(move == 1 ? 1 : 2, true);
            }
        }
    }
}

public sealed class DreamChampion : DistrictMiniboss
{
    Transform sails, pendulum;
    float swing;

    internal void RigSails()
    {
        Hull.GetComponent<MeshFilter>().sharedMesh = IndustrialMeshFactory.BarqueHull();
        Transform Fit(string label, Mesh mesh, Vector3 pivot)
        {
            var part = new GameObject(label);
            part.transform.SetParent(Hull, false);
            part.transform.localPosition = pivot;
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            var surface = part.AddComponent<MeshRenderer>();
            surface.sharedMaterial = IndustrialMeshFactory.BarquePaint;
            surface.sortingLayerName = "Enemy";
            return part.transform;
        }
        sails = Fit("Celestial sails", IndustrialMeshFactory.BarqueSails(), Vector3.zero);
        pendulum = Fit("Escapement", IndustrialMeshFactory.BarquePendulum(), IndustrialMeshFactory.BarquePivot);
        for (int i = 0; i < 2; i++)
            firingCue.PlacePort(i, IndustrialMeshFactory.BarqueLantern(2*i-1), true, false);
    }

    protected override void Update()
    {
        float previous = age;
        base.Update();
        float dt = age - previous;
        if (dt <= 0) return;
        float windup = Engaged && !Retreating ? Mathf.Clamp01(1 - (nextAttack - age) / .55f) : 0;
        // The weight catches on alternating sides of the escapement before each release.
        float target = Mathf.Lerp(22 * Mathf.Sin(age * 4.6f), (volley % 2 == 0 ? 37 : -37), windup);
        swing = Mathf.MoveTowards(swing, target, 160 * dt);
        pendulum.localRotation = Quaternion.Euler(0, 0, swing);
        sails.localRotation = Quaternion.Euler(0, 5 * Mathf.Sin(age * 2.6f) + 9 * windup, 0);
    }

    protected override void Configure() { health = 618; hullScale = .96f; attackInterval = 1.27f; nextAttack = 1.25f; }
    protected override Vector3 Position(float t) =>
        new Vector3(2.8f * Mathf.Sin(t * 1.01f), Camera.UpLimit - 3.75f + .92f * Mathf.Cos(t * 2.02f), 1);

    protected override void Attack()
    {
        int move = (volley - 1) % 3;
        int n = Dense(move == 0 ? 9 : 7);
        for (int s = -1; s <= 1; s += 2)
            for (int i = 0; i < n; i++)
            {
                float lane = i - (n - 1) * .5f;
                Vector3 lip = Muzzle(s < 0 ? 0 : 1);
                Vector2 direction = CombatVisuals.Rotate(move == 1 ? Vector2.up : Vector2.down, s * (22 + lane * 17));
                if (move == 2)
                    Fire<NeedleRound>(lip, direction, 3.65f, .24f, i == 0)
                        .Pause(.74f, .42f, CombatRound.Turn.Rotate, s * 33, 4.8f).ShowTurn();
                else if (move == 1)
                    Fire<StarRound>(lip, direction, 3.65f, .24f, i == 0).Bounce(2);
                else
                    Fire<CrescentRound>(lip, direction, 3.65f, .24f, i == 0);
            }
    }
}
