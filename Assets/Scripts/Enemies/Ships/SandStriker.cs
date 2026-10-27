using System.Collections;
using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class SandStriker : DistrictEnemy
{
    bool bursting;
    protected override void Configure()
    {
        hull = 31;
        hullScale = .52f;
        health = 31;
        attackInterval = (2.0f);
        Mover<Slalom>();
    }

    protected override void Attack()
    {
        if (!bursting)
            StartCoroutine(Burst());
    }

    IEnumerator Burst()
    {
        bursting = true;
        Vector2 locked = Aim;
        // One aiming decision, then six closely packed rounds; strafing can escape the entire stream.
        for (int i = 0; i < (5); i++)
        {
            if (GameRules.playerDisabled)
                break;
            if (player == null || Vector2.Distance(player.position, transform.position) > 1.8f)
                Fire<NeedleRound>(Hull.TransformPoint(new Vector3(0, -1.1f, -.13f)), CombatVisuals.Rotate(locked, i % 2 == 0 ? -1.1f : 1.1f), 5.6f, .19f);
            yield return new WaitForSeconds((.11f) / LevelRun.EnemyTempoScale);
        }
        bursting = false;
    }
}
public static partial class IndustrialMeshFactory
{
    static void BuildSandStriker(Builder b)
    {
        BevelArmor(b, new[] { V(0, -1.18f), V(-.22f, -.42f), V(-.24f, .59f), V(0, .83f), V(.24f, .59f), V(.22f, -.42f) }, .34f, 13);
        foreach (int s in new[] { -1, 1 })
        {
            BevelArmor(b, new[] { V(s * .18f, -.18f), V(s * .87f, .3f), V(s * .76f, .65f), V(s * .36f, .42f), V(s * .16f, .61f) }, .17f, 6, -.05f);
            BevelArmor(b, new[] { V(s * .3f, .4f), V(s * .47f, .7f), V(s * .51f, .13f), V(s * .38f, -.32f) }, .15f, 13, -.18f);
            b.Box(new Vector3(s * .38f, .62f, 0), new Vector3(.22f, .22f, .2f), 11, 6);
            b.Box(new Vector3(s * .59f, .37f, -.18f), new Vector3(.07f, .23f, .025f), 1, 6);
        }
        BevelArmor(b, new[] { V(-.12f, .34f), V(.12f, .34f), V(.10f, -.16f), V(0, -.35f), V(-.10f, -.16f) }, .12f, 11, -.25f);
        b.Box(new Vector3(0, -.07f, -.34f), new Vector3(.065f, .27f, .03f), 3, 11);
        b.Box(new Vector3(0, -.89f, -.12f), new Vector3(.12f, .42f, .14f), 11, 6);
    }
}
