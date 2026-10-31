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
