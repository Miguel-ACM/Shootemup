using System;
using System.IO;
using NUnit.Framework;

public class ReviewRegressionTests
{
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
