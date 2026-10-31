using System;
using System.IO;
using NUnit.Framework;
using System.Reflection;

public class ReviewRegressionTests
{
    [Test]
    public void ReflectionStillInterceptsAtTheFriendlyProjectileCap()
    {
        var count = typeof(PlayerRound).GetField("<LiveCount>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
        int previous = (int)count.GetValue(null);
        var round = CombatRound.Launch<SimpleRound>(UnityEngine.Vector3.zero, UnityEngine.Vector2.down);
        Assert.IsNotNull(round);
        try
        {
            count.SetValue(null, 180);
            Assert.IsTrue(BulletReflection.Reflect(round));
            Assert.IsFalse(round.gameObject.activeSelf);
            Assert.AreEqual(180, PlayerRound.LiveCount);
            Assert.IsFalse(BulletReflection.Reflect(null));
        }
        finally
        {
            count.SetValue(null, previous);
            CombatRound.Live.Remove(round); // Edit Mode does not invoke the runtime OnDisable callback.
            UnityEngine.Object.DestroyImmediate(round.gameObject);
        }
    }

    static T TestEnemy<T>(UnityEngine.Transform parent, float health) where T : AEnemy
    {
        var go = new UnityEngine.GameObject(typeof(T).Name);
        go.SetActive(false);
        go.transform.SetParent(parent);
        var enemy = go.AddComponent<T>();
        enemy.health = health;
        if (enemy is DistrictBoss)
            typeof(DistrictBoss).GetField("initialHealth", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(enemy, health);
        return enemy;
    }

    static void EnterTestPhase(DistrictBoss boss, int phase)
    {
        typeof(DistrictBoss).GetField("phaseTwo", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(boss, phase >= 2);
        typeof(DistrictBoss).GetField("phaseThree", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(boss, phase >= 3);
        var value = boss.GetType().GetProperty("PhaseMeterValue", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(boss);
        typeof(DistrictBoss).GetField("phaseStart", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(boss, value);
    }

    [TestCase(RunDifficulty.Easy)]
    [TestCase(RunDifficulty.Hard)]
    public void MultipartMetersTrackPhaseObjectives(RunDifficulty difficulty)
    {
        var previous = Difficulty.Current;
        Difficulty.Select(difficulty);
        var root = new UnityEngine.GameObject("Phase meter test");
        root.SetActive(false);
        try
        {
            var space = TestEnemy<SpaceCitadelBoss>(root.transform, 1000);
            for (int i = 0; i < 5; i++)
                space.Parts[i] = TestEnemy<SpaceBossPart>(root.transform, i < 2 ? 130 : i < 4 ? 110 : 520);
            Assert.AreEqual(1, space.PhaseFraction, .0001f);
            space.Parts[0].health = space.Parts[1].health = 0;
            space.health = 740;
            Assert.AreEqual(0, space.PhaseFraction, .0001f);
            EnterTestPhase(space, 2);
            Assert.AreEqual(1, space.PhaseFraction, .0001f);
            if (Difficulty.Hard)
            {
                space.Parts[4].health = 30;
                space.health = 250;
                Assert.AreEqual(0, space.PhaseFraction, .0001f);
                EnterTestPhase(space, 3);
                Assert.AreEqual(1, space.PhaseFraction, .0001f);
            }
            space.Parts[4].health = 0; // Optional guns survive the reactor.
            space.health = 220;
            Assert.AreEqual(0, space.PhaseFraction, .0001f);

            var hell = TestEnemy<HellCrabBoss>(root.transform, 1000);
            for (int i = 0; i < 2; i++)
                hell.Eyes[i] = TestEnemy<HellCrabEye>(root.transform, 250);
            hell.health = 500;
            Assert.AreEqual(.5f, hell.PhaseFraction, .0001f); // Neither eye is dead yet.
            hell.Eyes[0].health = 0;
            Assert.AreEqual(0, hell.PhaseFraction, .0001f);

            var desert = TestEnemy<DesertWormBoss>(root.transform, 2700);
            for (int i = 0; i < desert.Cores.Length; i++)
                desert.Cores[i] = TestEnemy<DesertWormCore>(root.transform, i == 0 ? 670 : 290);
            for (int i = 0; i < 4; i++)
                desert.Cores[i].health = 0;
            Assert.AreEqual(0, desert.PhaseFraction, .0001f);

            foreach (bool heavyFirst in new[] { false, true })
            {
                var first = TestEnemy<CityDuoBoss>(root.transform, heavyFirst ? 1400 : 1000);
                var survivor = TestEnemy<CityDuoBoss>(root.transform, heavyFirst ? 1000 : 1400);
                first.partner = survivor;
                survivor.partner = first;
                first.health = 0;
                Assert.AreEqual(0, survivor.PhaseFraction, .0001f);
                survivor.partner = null;
                EnterTestPhase(survivor, 2);
                Assert.AreEqual(1, survivor.PhaseFraction, .0001f);
                survivor.health *= Difficulty.Hard ? .45f : 0;
                Assert.AreEqual(0, survivor.PhaseFraction, .0001f);
            }
        }
        finally
        {
            Difficulty.Select(previous);
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

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
            Assert.IsNull(PlasmaOrb.FirstOnBeam(UnityEngine.Vector2.zero, UnityEngine.Vector2.up * 5, .06f, out _));
        }
        finally
        {
            CombatRound.Live.Remove(orb); // Edit Mode does not invoke the runtime OnDisable callback.
            UnityEngine.Object.DestroyImmediate(orb.gameObject);
        }
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
