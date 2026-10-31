using NUnit.Framework;
using UnityEngine;

public sealed class DamageContractProbe : AEnemy
{
    public bool blocked;
    public override bool CanReceiveDamage => !blocked && base.CanReceiveDamage;
    public override void damage(float amount) { health -= amount * .5f; }
}

public class RefactorTests
{
    [Test]
    public void RunPlanKeepsAuthoredOrderAndChallengeCountsOnlyOrdinaryWaves()
    {
        var plan = new RunPlan();
        var authored = DistrictWaves.Sequence();
        plan.Load(authored);
        CollectionAssert.AreEqual(authored, plan.Export());
        Assert.AreEqual(10, plan.RemainingCombat(0));
        Assert.AreEqual(RunSlotKind.Combat, plan.Following(4).Kind);
        plan.Insert(5, RunSlotKind.Pact);
        Assert.AreEqual(RunSlotKind.Pact, plan[5].Kind);
        var challenge = new ChallengeProgress();
        challenge.Start(true);
        Assert.IsFalse(challenge.FinishWave());
        challenge.BeginWave(false);
        Assert.IsFalse(challenge.FinishWave());
        Assert.AreEqual(2, challenge.Remaining);
        challenge.BeginWave(true);
        Assert.IsFalse(challenge.FinishWave());
        challenge.BeginWave(true);
        Assert.IsTrue(challenge.FinishWave());
        Assert.IsFalse(challenge.FinishWave());
    }

    [Test]
    public void DamageResultsSeparateRejectionArmourAndOverkill()
    {
        var go = new GameObject("Damage contract");
        go.SetActive(false);
        try
        {
            var enemy = go.AddComponent<DamageContractProbe>();
            enemy.health = 10;
            var hit = enemy.ReceiveDamage(6);
            Assert.IsTrue(hit.Accepted);
            Assert.AreEqual(6, hit.Requested);
            Assert.AreEqual(3, hit.Applied);
            enemy.blocked = true;
            Assert.IsFalse(CombatHit.Tick(enemy, 100).Accepted);
            Assert.AreEqual(7, enemy.health);
            enemy.blocked = false;
            Assert.AreEqual(7, enemy.ReceiveDamage(100).Applied);
            Assert.IsFalse(enemy.ReceiveDamage(1).Accepted);
        }
        finally { Object.DestroyImmediate(go); }
    }
}
