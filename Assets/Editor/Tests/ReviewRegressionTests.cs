using System;
using System.IO;
using NUnit.Framework;

public class ReviewRegressionTests
{
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
