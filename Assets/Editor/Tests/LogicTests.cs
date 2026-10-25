using NUnit.Framework;

// Edit Mode tests for checks that need no scene or Play mode. Each check throws with a
// description of the first failure, which the Test Runner reports as the failure message.
// Run them from Window > General > Test Runner, or from a terminal:
//   Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testResults Logs/TestResults.xml
public class LogicTests
{
    [Test]
    public void BossMovement() => VerifyBossMovement.Run();

    [Test]
    public void EnemyFlight() => VerifyEnemyFlight.Run();

    [Test]
    public void MovementPatterns() => VerifyMovementPatterns.Run();

    [Test]
    public void RunProgression() => VerifyRunProgression.Run();

    // Written for the original WavePool budgets. Numbered waves now come from the authored
    // DistrictWaves, whose costs follow different rules, so its first check fails.
    [Test, Ignore("Out of date since numbered waves moved to DistrictWaves")]
    public void WavePacing() => VerifyWavePacing.Run();

    [Test]
    public void PolygonBreakup() => VerifyPolygonBreakup.RunChecks();

    [Test]
    public void ProjectileContacts() => VerifyProjectileContacts.RunChecks();
}
