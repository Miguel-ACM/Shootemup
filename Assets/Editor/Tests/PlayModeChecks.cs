using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

// The checks that play the real game: each opens its scene, plays for a while and writes a
// report beginning with PASS or FAIL. These are slow, so they are in the "PlayMode" category:
//   Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testCategory PlayMode
// Variables don't survive entering Play mode (scripts reload), so CheckHarness keeps the state.
[Category("PlayMode")]
public class PlayModeChecks
{
    const double Minutes = 60;

    static IEnumerator Check(Action start, string report, double timeoutMinutes = 15)
    {
        CheckHarness.Begin(start, report, timeoutMinutes * Minutes);
        yield return new EnterPlayMode();
        while (!CheckHarness.Finished)
            yield return null;
        yield return new ExitPlayMode();
        CheckHarness.AssertPassed();
    }

    [UnityTest] public IEnumerator Backgrounds() => Check(VerifyBackgrounds.Run, "BackgroundReview/Verification.txt");
    [UnityTest] public IEnumerator BossFinale() => Check(VerifyBossFinale.Run, VerifyBossFinale.Folder + "/Verification.txt");
    [UnityTest] public IEnumerator BossPatterns() => Check(VerifyBossPatterns.Run, "Temp/BossPatterns.result");
    [UnityTest] public IEnumerator Candyland() => Check(VerifyCandyland.Run, VerifyCandyland.Folder + "/Verification.txt");
    [UnityTest] public IEnumerator CombatPolish() => Check(VerifyCombatPolish.Run, VerifyCombatPolish.Folder + "/Verification.txt");
    [UnityTest] public IEnumerator Desert() => Check(VerifyDesert.Run, VerifyDesert.Folder + "/Verification.txt");
    [UnityTest] public IEnumerator EverydayItems() => Check(VerifyEverydayItems.Run, "ArtSource/EverydayItemReview/Verification.txt");
    [UnityTest] public IEnumerator GroundWaves() => Check(VerifyGroundWaves.Run, VerifyGroundWaves.Folder + "/Verification.txt");
    [UnityTest] public IEnumerator GumSprites() => Check(VerifyGumSprites.Run, "ArtSource/GumSpriteReview/Verification.txt");
    [UnityTest] public IEnumerator InputMigration() => Check(VerifyInputMigration.Run, "FlowReview/InputMigration.txt");
    [UnityTest] public IEnumerator LevelFlow() => Check(VerifyLevelFlow.Run, "FlowReview/LevelFlowResult.txt");
    [UnityTest] public IEnumerator PlainsScenery() => Check(VerifyPlainsScenery.Run, VerifyPlainsScenery.Folder + "/Runtime.txt");
    [UnityTest] public IEnumerator PlayerFeedback() => Check(VerifyPlayerFeedback.Run, "Temp/PlayerFeedback.result");
    [UnityTest] public IEnumerator RequestedItems() => Check(VerifyRequestedItems.Run, "ArtSource/RequestedItemReview/Verification.txt");
    [UnityTest] public IEnumerator Riverbanks() => Check(VerifyRiverbanks.Run, VerifyRiverbanks.Folder + "/Verification.txt");
    [UnityTest] public IEnumerator RunChoices() => Check(VerifyRunChoices.Run, VerifyRunChoices.Folder + "/Verification.txt");
    [UnityTest] public IEnumerator SandEmitter() => Check(VerifySandEmitter.Run, VerifySandEmitter.Folder + "/Verification.txt");
    [UnityTest] public IEnumerator SpriteProjectiles() => Check(VerifySpriteProjectiles.Run, "ArtSource/SpriteProjectileReview/Verification.txt");
    [UnityTest] public IEnumerator VisualClarity() => Check(VerifyVisualClarity.Run, "ArtSource/VisualClarityReview/Verification.txt");
    [UnityTest] public IEnumerator WaveVariety() => Check(VerifyWaveVariety.Run, "ArtSource/WaveVarietyReview/Verification.txt");
    [UnityTest] public IEnumerator WindupItems() => Check(VerifyWindupItems.Run, "ArtSource/ItemFeedbackReview/Verification.txt");
}
