using UnityEngine;

// Space citadel: the only district built on circles, and every circle moves.
// Shield generators launch an orrery, two breathing rings turning against each
// other as they sink, so their gaps meet and part. Turrets fling comets: tight
// spinning rings that travel at the player as one body. The core keeps an
// eclipse of nested counter-rotating rings, and once exposed it spins up a
// galaxy whose stars slingshot off one after another along spiral arms.
public sealed partial class SpaceCitadelBoss
{
    Vector3 Core => parts[4] != null ? parts[4].transform.position : transform.position;
    Vector3 GeneratorPort(int i) => parts[i].transform.TransformPoint(new Vector3(0, -.3f, -.3f));
    Vector3 TurretPort(int i) => parts[i].transform.TransformPoint(new Vector3(i == 2 ? -.16f : .16f, -.695f, -.08f));
    protected override void Attack()
    {
        if (parts[0] == null)
            return;
        int beat = (volley - 1) % 12;
        if (beat == 0)
        {
            if (CoreExposed)
                Galaxy(1);
            else
                for (int i = 0; i < 2; i++)
                    if (parts[i].health > 0)
                        Orrery(GeneratorPort(i), i == 0 ? 1 : -1);
        }
        else if (beat == 2 && phaseTwo)
            Galaxy(-1);
        else if (beat == 4)
        {
            centralLaser = Fire<PulseLaser>(Muzzle(), Vector2.down, 0);
            if (centralLaser != null)
            {
                centralLaser.Bind(Hull, WeaponPort());
                centralLaser.warning = 1.3f;
                centralLaser.activeTime = phaseTwo ? 1.8f : 1.5f;
                centralLaser.width = phaseTwo ? .42f : .34f;
            }
        }
        else if (beat == 6)
        {
            for (int i = 2; i < 4; i++)
                if (parts[i].health > 0)
                    Comet(TurretPort(i), i == 2 ? 1 : -1);
            if (phaseTwo)
                Comet(Core, volley / 12 % 2 == 0 ? 1 : -1);
        }
        else if (beat == 9)
            Eclipse();
    }

    // A breathing ring with a three-bead gap, turning one way while it sinks.
    void Orrery(Vector3 port, int sign)
    {
        int beads = Dense(24);
        int gap = (volley * 5) % beads;
        for (int i = 0; i < beads; i++)
        {
            if ((i - gap + beads) % beads < 3)
                continue;
            var bead = Fire<SimpleRound>(port, Vector2.down, 1.0f, .18f);
            if (bead != null)
                bead.Orbit(1.25f, i * 360f / beads, sign * 45, 11, 0, 0, 0, .9f).Breathe(.5f, .35f);
        }
    }

    // A tight fast-spinning ring that travels at the player as one body.
    void Comet(Vector3 port, int sign)
    {
        int beads = Dense(12);
        var aim = CombatVisuals.Aim(port);
        for (int i = 0; i < beads; i++)
        {
            var bead = Fire<SimpleRound>(port, aim, 1.9f, .2f);
            if (bead != null)
                bead.Orbit(.8f, i * 360f / beads, sign * 150, 10, 0, 0, 0, .4f);
        }
    }

    // Stars wound round the core on two arms, slung off on the tangent one after another.
    void Galaxy(int sign)
    {
        int stars = Dense(phaseTwo ? 40 : 32);
        for (int i = 0; i < stars; i++)
        {
            var star = Fire<SimpleRound>(Core, Vector2.down, 0, .19f);
            if (star != null)
                star.Orbit(1.0f + (i % 2) * .5f, i * 720f / stars, sign * 85, 9, 0, 0, 0, .5f).Sling(.9f + i * .055f, .75f);
        }
    }

    // Nested rings turning against each other; their gaps line up only now and then.
    void Eclipse()
    {
        int inner = Dense(18), outer = Dense(28);
        int innerGap = (volley * 3) % inner, outerGap = (volley * 7) % outer;
        for (int i = 0; i < inner; i++)
        {
            if ((i - innerGap + inner) % inner < 3)
                continue;
            var bead = Fire<SimpleRound>(Core, Vector2.down, .8f, .18f);
            if (bead != null)
                bead.Orbit(1.1f, i * 360f / inner, 38, 12, 0, 0, 0, .8f);
        }
        for (int i = 0; i < outer; i++)
        {
            if ((i - outerGap + outer) % outer < 4)
                continue;
            var bead = Fire<SimpleRound>(Core, Vector2.down, .8f, .2f);
            if (bead != null)
                bead.Orbit(2.0f, i * 360f / outer, -30, 12, 0, 0, 0, 1.1f).Breathe(.3f, .25f);
        }
    }

    protected override void PrepareScore(float seconds)
    {
        if (parts[0] == null)
            return;
        int beat = volley % 12;
        if (beat == 0)
        {
            if (CoreExposed)
                Cue(Core, seconds);
            else
                for (int i = 0; i < 2; i++)
                    if (parts[i].health > 0)
                        Cue(GeneratorPort(i), seconds);
        }
        else if (beat == 2 && phaseTwo || beat == 9)
            Cue(Core, seconds);
        else if (beat == 4)
            Cue(Muzzle(), seconds);
        else if (beat == 6)
        {
            for (int i = 2; i < 4; i++)
                if (parts[i].health > 0)
                    Cue(TurretPort(i), seconds);
            if (phaseTwo)
                Cue(Core, seconds);
        }
    }
}
