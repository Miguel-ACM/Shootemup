using System;
using System.IO;
using NUnit.Framework;
using System.Reflection;

public class ReviewRegressionTests
{
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
