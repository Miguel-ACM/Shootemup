using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public sealed class DamageContractProbe : AEnemy
{
    public bool blocked;
    public override bool CanReceiveDamage => !blocked && base.CanReceiveDamage;
    public override void damage(float amount) { health -= amount * .5f; }
}

public class RefactorTests
{
    [Test]
    public void EquipmentPressIsConsumedBeforeWeaponSubscribers()
    {
        _ = GameInput.Actions;
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var previous = GameInput.EquipmentSelection;
        int selections = 0, shots = 0;
        bool consume = true;
        System.Action<InputAction.CallbackContext> fire = _ => shots++;
        GameInput.PrimaryPressed += fire;
        GameInput.EquipmentSelection = (active, slot) => { selections++; return consume; };
        try
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Z, Key.LeftShift));
            InputSystem.Update(InputUpdateType.Dynamic);
            Assert.AreEqual(1, selections);
            Assert.AreEqual(0, shots);
            Assert.IsTrue(GameInput.Snapshot.Primary);
            Assert.IsTrue(GameInput.Snapshot.Focus);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.Update(InputUpdateType.Dynamic);
            consume = false;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Z));
            InputSystem.Update(InputUpdateType.Dynamic);
            Assert.AreEqual(2, selections);
            Assert.AreEqual(1, shots);
            Assert.IsFalse(GameInput.Snapshot.Focus);
        }
        finally
        {
            GameInput.PrimaryPressed -= fire;
            GameInput.EquipmentSelection = previous;
            InputSystem.RemoveDevice(keyboard);
        }
    }

    [Test]
    public void CatalogsCoverEveryConcreteItemAndEveryAuthoredActor()
    {
        var keys = new System.Collections.Generic.HashSet<string>();
        foreach (var item in ItemCatalog.All)
        {
            Assert.IsTrue(keys.Add(item.Key));
            Assert.AreSame(item, ItemCatalog.Get(item.Id));
            Assert.AreEqual(item.Key, item.ComponentType.Name);
        }
        foreach (var type in typeof(AItem).Assembly.GetTypes())
            if (!type.IsAbstract && typeof(AItem).IsAssignableFrom(type))
                Assert.IsTrue(keys.Contains(type.Name), "Unregistered item " + type.Name);
        Assert.AreEqual(66, EnemyCatalog.WaveTypes.Length);
        Assert.AreEqual(76, EnemyCatalog.All.Length);
        foreach (var actor in EnemyCatalog.All)
        {
            Assert.AreSame(actor.ComponentType, EnemyCatalog.Resolve(actor.Key));
            Assert.AreSame(actor, EnemyCatalog.Get(actor.Id));
        }
        foreach (var entry in EnemyExpansion.All)
            Assert.AreEqual(entry.hull, (int)EnemyCatalog.Get(entry.type).Hull);
        Assert.AreEqual("common", ItemCatalog.Get(ItemId.TwerpItem).RarityKey);
        Assert.AreEqual("rare", ItemCatalog.Get(ItemId.AMoonItem).RarityKey);
    }

    [Test]
    public void PhaseStateUsesOneClockAndAnObjectiveSpecificMeter()
    {
        var phase = new BossPhaseState { Phase = 2 };
        phase.BeginBreak(10, 1.5f, .74f);
        Assert.IsTrue(phase.Transitioning(11.49f));
        Assert.IsFalse(phase.Transitioning(11.5f));
        Assert.AreEqual(1, phase.Fraction(.74f, .25f), .0001f);
        Assert.AreEqual(.5f, phase.Fraction(.495f, .25f), .0001f);
        Assert.AreEqual(0, phase.Fraction(.25f, .25f), .0001f);
    }

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
