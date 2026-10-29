using UnityEngine;
using System.Collections;

// Space citadel: gravity is the weapon. Every round still leaves the station's own
// generators, turrets or core.
// Phase one (shield generators up)
//   Gravity Well: a well opens in the middle of the arena. The generators fire arrow
//     streams sideways and the well bends them into arcs, loops and slingshots; the
//     turrets lob slow orbs that fall into orbit around it.
//   Supernova: the core throws rings of arrows outward; each ring stops, holds, then
//     collapses back through the core and bursts out the other side. The centre
//     laser cuts the middle at the end.
// Phase two (core exposed)
//   Event Horizon: the well drifts after the player while the core winds four spiral
//     arms of arrows that bend into it.
//   Binary Collapse: two supernova rings at once, counter-rotating offsets, with the
//     laser between them.
public sealed partial class SpaceCitadelBoss
{
    Vector3 Core => parts[4] != null ? parts[4].transform.position : transform.position;
    Vector3 GeneratorPort(int i) => parts[i].transform.TransformPoint(new Vector3(0, -.3f, -.3f));
    Vector3 TurretPort(int i) => parts[i].transform.TransformPoint(new Vector3(i == 2 ? -.16f : .16f, -.695f, -.08f));
    Transform well;
    LineRenderer horizon;
    static float Middle => (Camera.UpLimit + Camera.DownLimit) * .5f;
    protected override void Attack()
    {
        if (parts[0] != null)
            OpenCards();
    }

    protected override IEnumerator Cards()
    {
        while (true)
        {
            if (!phaseTwo)
            {
                yield return GravityWell();
                yield return Wait(.8f);
                yield return Supernova(false);
                yield return Wait(.8f);
            }
            else
            {
                yield return EventHorizon();
                yield return Wait(.7f);
                yield return Supernova(true);
                yield return Wait(.7f);
            }
        }
    }

    // A visible well: a pulsing ring with a dark core, where the pull is centred.
    void OpenWell(Vector2 at)
    {
        if (well == null)
        {
            well = new GameObject("Gravity well").transform;
            horizon = CombatVisuals.Line(well, LevelProjectilePalette.Accent(LevelId.Space), .06f);
            ItemSpark.Sprite(well, "Well core", "standard", LevelProjectilePalette.Accent(LevelId.Space), .6f);
        }
        well.gameObject.SetActive(true);
        well.position = new Vector3(at.x, at.y, -.4f);
    }

    void CloseWell()
    {
        if (well != null)
            well.gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        if (completing || health <= 0)
            CloseWell();
        if (well != null && well.gameObject.activeSelf)
            CombatVisuals.Ring(horizon, well.position, .55f + .12f * Mathf.Sin(Time.time * 6));
    }

    IEnumerator GravityWell()
    {
        var point = new Vector2(0, Middle + .6f);
        OpenWell(point);
        yield return Wait(.6f);
        yield return Every(8, .12f, n =>
        {
            for (int i = 0; i < 2; i++)
                if (parts[i].health > 0)
                {
                    int side = i == 0 ? -1 : 1;
                    var axis = CombatVisuals.Rotate(Vector2.right * side, -side * (n % 6) * 12);
                    Fire<NeedleRound>(GeneratorPort(i), axis, 3.6f, .19f).Attract(point, 9).LivesFor(9);
                }
            if (n % 8 == 4)
                for (int i = 2; i < 4; i++)
                    if (parts[i].health > 0)
                        for (int k = -1; k <= 1; k++)
                            Fire<SimpleRound>(TurretPort(i), CombatVisuals.Rotate(Vector2.down, (i == 2 ? 50 : -50) + k * 16), 2.0f, .26f).Attract(point, 7).LivesFor(10);
        });
        CloseWell();
    }

    // Rings thrown out that stop, hold, then fall back through the core and burst out beyond it.
    void SupernovaRing(int beads, float radius, float offset)
    {
        float travel = .9f;
        float speed = radius / (travel - .175f);
        for (int i = 0; i < beads; i++)
            Fire<NeedleRound>(Core, CombatVisuals.Rotate(Vector2.down, offset + i * 360f / beads), WorldSpeed(speed), .19f)
                .Pause(travel, .7f, CombatRound.Turn.Rotate, 180, 2.6f / speed)
                .LivesFor(9);
    }

    IEnumerator Supernova(bool binary)
    {
        int beads = Dense(binary ? 26 : 30);
        for (int n = 0; n < 5; n++)
        {
            yield return Charge(Hull, WeaponPort(), .35f);
            SupernovaRing(beads, 3.4f + (n % 2) * .8f, n * 6);
            if (binary)
                SupernovaRing(beads, 2.2f, -n * 6 + 180f / beads);
            yield return Wait(Rest(1.1f));
        }
        Laser();
        yield return Wait(2.2f);
    }

    IEnumerator EventHorizon()
    {
        Vector2 point = new Vector2(0, Middle);
        OpenWell(point);
        yield return Wait(.5f);
        int steps = Dense(110);
        for (int i = 0; i < steps; i++)
        {
            // The well drifts after the player, slowly enough to outrun.
            if (player != null)
                point = Vector2.MoveTowards(point, new Vector2(player.position.x, Mathf.Max(player.position.y + 2.5f, Camera.DownLimit + 3)), .025f);
            well.position = new Vector3(point.x, point.y, -.4f);
            for (int arm = 0; arm < 4; arm++)
                Fire<NeedleRound>(Core, CombatVisuals.Rotate(Vector2.down, arm * 90 + i * 9), 3.2f, .19f).Attract(point, 8).LivesFor(9);
            yield return Wait(Rest(.075f));
        }
        CloseWell();
    }

    void Laser()
    {
        centralLaser = Fire<PulseLaser>(Muzzle(), Vector2.down, 0);
        if (centralLaser == null)
            return;
        centralLaser.Bind(Hull, WeaponPort());
        centralLaser.warning = 1.1f;
        centralLaser.activeTime = phaseTwo ? 1.8f : 1.4f;
        centralLaser.width = phaseTwo ? .42f : .34f;
    }

    void OnDestroy()
    {
        if (well != null)
            Destroy(well.gameObject);
    }
}
