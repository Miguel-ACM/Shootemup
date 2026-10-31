using System;
using System.IO;
using NUnit.Framework;
using System.Reflection;

public class ReviewRegressionTests
{
    [Test]
    public void FollowersDoNotHideOrbitalsFromSpacingUpdates()
    {
        var go = new UnityEngine.GameObject("Companion spacing test");
        go.SetActive(false);
        try
        {
            var ship = go.AddComponent<Shipcontroller>();
            var list = (System.Collections.Generic.List<ACompanion>)typeof(Shipcontroller)
                .GetField("companions", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ship);
            for (int i = 0; i < 4; i++)
            {
                var child = new UnityEngine.GameObject("Companion " + i);
                child.transform.SetParent(go.transform);
                var companion = child.AddComponent<ACompanion>();
                list.Add(companion);
                if (i < 2)
                    continue;
                typeof(ACompanion).GetField("type", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(companion, ACompanion.CompanionType.orbital);
                child.AddComponent<OrbitPlayer>().SetIndex(i - 1, i - 1);
            }
            typeof(Shipcontroller).GetMethod("RedistributeOrbitals", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ship, new object[] { 2 });
            var offset = typeof(OrbitPlayer).GetField("offset", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.AreEqual(.5f, offset.GetValue(list[2].GetComponent<OrbitPlayer>()));
            Assert.AreEqual(1f, offset.GetValue(list[3].GetComponent<OrbitPlayer>()));
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    [Test]
    public void BeamsFindAndDestroyPlasmaUsingItsAuthoredCollider()
    {
        var orb = CombatRound.Launch<PlasmaOrb>(UnityEngine.Vector3.up * 2, UnityEngine.Vector2.down, 0, .3f);
        Assert.IsNotNull(orb);
        try
        {
            UnityEngine.Physics2D.SyncTransforms();
            Assert.AreSame(orb, PlasmaOrb.FirstOnBeam(UnityEngine.Vector2.zero, UnityEngine.Vector2.up * 5, .06f, out _));
            Assert.IsNull(PlasmaOrb.FirstOnBeam(UnityEngine.Vector2.right * 2, new UnityEngine.Vector2(2, 5), .06f, out _));
            orb.Hit(59);
            Assert.IsTrue(orb.gameObject.activeSelf);
            orb.Hit(1);
            Assert.IsFalse(orb.gameObject.activeSelf);
            Assert.IsFalse(CombatRound.Live.Contains(orb));
        }
        finally { UnityEngine.Object.DestroyImmediate(orb.gameObject); }
    }

    [Test]
    public void HomingSkipsProtectedHullAndCore()
    {
        var hullObject = new UnityEngine.GameObject("Protected hull");
        var targetObject = new UnityEngine.GameObject("Eligible target");
        hullObject.SetActive(false);
        targetObject.SetActive(false);
        var hull = hullObject.AddComponent<SpaceCitadelBoss>();
        var core = hullObject.AddComponent<SpaceBossPart>();
        var target = targetObject.AddComponent<ThunderLanceBoss>();
        targetObject.transform.position = UnityEngine.Vector3.up * 2;
        AEnemy.Live.Add(hull);
        AEnemy.Live.Add(core);
        AEnemy.Live.Add(target);
        try { Assert.AreSame(target, CombatUpgrades.Nearest(UnityEngine.Vector3.zero, null, 5)); }
        finally
        {
            AEnemy.Live.Remove(hull);
            AEnemy.Live.Remove(core);
            AEnemy.Live.Remove(target);
            UnityEngine.Object.DestroyImmediate(hullObject);
            UnityEngine.Object.DestroyImmediate(targetObject);
        }
    }

    [Test]
    public void PhaseInvulnerabilityRejectsDirectHitsBeforeEffectsAndScore()
    {
        var go = new UnityEngine.GameObject("Invulnerability test");
        go.SetActive(false);
        try
        {
            var boss = go.AddComponent<ThunderLanceBoss>();
            typeof(DistrictBoss).GetField("phaseEntryUntil", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(boss, 1.5f);
            float health = boss.health;
            Assert.IsFalse(boss.CanReceiveDamage);
            Assert.AreEqual(0, CombatHit.Apply(boss, 10, UnityEngine.Vector3.zero, UnityEngine.Vector2.up, fire: true));
            Assert.AreEqual(health, boss.health);
            Assert.IsNull(go.GetComponent<BurningHull>());
            foreach (Type type in new[] { typeof(SpaceBossPart), typeof(HellCrabEye), typeof(DesertWormCore) })
            {
                var part = (AEnemy)go.AddComponent(type);
                Assert.IsFalse(part.CanReceiveDamage); // Detached parts cannot accept hits either.
                Assert.AreEqual(0, CombatHit.Apply(part, 10, UnityEngine.Vector3.zero, UnityEngine.Vector2.up));
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    [Test]
    public void ThunderEmitterStaysSuppressedForItsOwnersEntirePhaseBreak()
    {
        var go = new UnityEngine.GameObject("Thunder phase test");
        go.SetActive(false);
        try
        {
            var boss = go.AddComponent<ThunderLanceBoss>();
            var bot = go.AddComponent<ThunderLaserBot>().OwnedBy(boss);
            Assert.IsTrue(bot.EmissionAllowed);
            var until = typeof(DistrictBoss).GetField("phaseEntryUntil", BindingFlags.Instance | BindingFlags.NonPublic);
            until.SetValue(boss, 1.5f);
            Assert.IsFalse(bot.EmissionAllowed);
            until.SetValue(boss, 0f);
            Assert.IsTrue(bot.EmissionAllowed);
            bot.Powered = false;
            Assert.IsFalse(bot.EmissionAllowed);
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    [Test]
    public void MidFlightChallengeAddsFutureAccentWithoutReplayingEarlierShots()
    {
        var go = new UnityEngine.GameObject("Challenge test");
        go.SetActive(false);
        try
        {
            var flight = go.AddComponent<FormationFlight>();
            flight.duration = 12;
            flight.fireTimes = new[] { 1f, 3f, 5f, 7f };
            typeof(FormationFlight).GetField("clock", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(flight, 4f);
            typeof(FormationFlight).GetField("nextStrike", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(flight, 2);
            flight.MakeChallenging();
            CollectionAssert.AreEqual(new[] { 1f, 3f, 5f, 6f, 7f }, flight.fireTimes);
            Assert.IsFalse(flight.TakeFireBeat());
            flight.MakeChallenging();
            Assert.AreEqual(5, flight.fireTimes.Length);
            foreach (float time in new[] { 5f, 6f, 7f })
            {
                typeof(FormationFlight).GetField("clock", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(flight, time);
                Assert.IsTrue(flight.TakeFireBeat());
                Assert.IsFalse(flight.TakeFireBeat());
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    [Test]
    public void ExhaustedRewardsOnlyRepeatUnlockedItemsWithTheirRealRarity()
    {
        // Inject a tiny save in memory: this test must never load or alter the player's hangar.
        var dataField = typeof(MetaProgress).GetField("data", BindingFlags.Static | BindingFlags.NonPublic);
        object previous = dataField.GetValue(null);
        var data = Activator.CreateInstance(dataField.FieldType, true);
        dataField.FieldType.GetField("unlocked").SetValue(data, new System.Collections.Generic.List<string> { "Popcorn" });
        dataField.SetValue(null, data);
        var fields = new[] { "common", "rare", "epic", "legendary", "defaultPool" };
        var saved = new object[fields.Length];
        for (int i = 0; i < fields.Length; i++)
            saved[i] = typeof(ItemPool).GetField(fields[i], BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        try
        {
            ItemPool.ResetRun();
            var draw = typeof(ItemPool).GetMethod("GetItemStringFromPool", BindingFlags.Static | BindingFlags.NonPublic);
            for (int i = 0; i < 12; i++)
            {
                object[] args = { "common", null };
                Assert.AreEqual("Popcorn", draw.Invoke(null, args));
                Assert.AreEqual("common", args[1]);
            }
        }
        finally
        {
            for (int i = 0; i < fields.Length; i++)
                typeof(ItemPool).GetField(fields[i], BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, saved[i]);
            dataField.SetValue(null, previous);
        }
    }

    [Test]
    public void OverlappingWavesCreditLateKillsAndResolveInOrder()
    {
        var ledger = new WaveKillLedger();
        var results = new System.Collections.Generic.List<string>();
        Action<int, int> report = (total, killed) => results.Add(total + "/" + killed);
        ledger.Begin(1, 2);
        ledger.Resolve(1, true);
        ledger.HandOff(1);
        ledger.Begin(2, 1);
        ledger.Resolve(2, false);
        ledger.HandOff(2);
        ledger.Drain(report);
        Assert.IsEmpty(results);
        ledger.Resolve(1, true);
        ledger.Drain(report);
        CollectionAssert.AreEqual(new[] { "2/2", "1/0" }, results);
        ledger.Drain(report);
        Assert.AreEqual(2, results.Count);
    }

    [Test]
    public void SaveReplacementRetainsBackupAndRecoversInterruptedWrites()
    {
        string folder = Path.Combine(Path.GetTempPath(), "ShootemupSaveTest-" + Guid.NewGuid());
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, "hangar.json");
        try
        {
            AtomicSaveFile.Write(path, "first");
            AtomicSaveFile.Write(path, "second");
            Assert.AreEqual("first", File.ReadAllText(path + ".bak"));
            File.WriteAllText(path + ".tmp", "interrupted");
            Assert.AreEqual("second", AtomicSaveFile.Read(path, s => s != "corrupt"));
            File.WriteAllText(path, "corrupt");
            Assert.AreEqual("interrupted", AtomicSaveFile.Read(path, s => s != "corrupt"));
            File.WriteAllText(path + ".tmp", "corrupt");
            Assert.AreEqual("first", AtomicSaveFile.Read(path, s => s != "corrupt"));
        }
        finally { Directory.Delete(folder, true); }
    }
}
