#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// Headless checks of actual secondary releases and the shader state used by gum.
public static class VerifyTeethAndGum
{
    const string Folder = "ReviewOutput/TeethAndGum";
    const BindingFlags Instance = BindingFlags.Instance | BindingFlags.NonPublic;
    const BindingFlags Static = BindingFlags.Static | BindingFlags.NonPublic;
    static int checks;
    public static void Run()
    {
        if (!Application.dataPath.Replace('\\', '/').EndsWith("/Temp/UnlockReview/Assets", StringComparison.Ordinal))
            throw new Exception("Use the isolated UnlockReview checkout");
        Directory.CreateDirectory(Folder);
        string saveDirectory = Path.GetFullPath(Folder + "/Save");
        MetaProgress.ReviewSaveDirectory = saveDirectory;
        Directory.CreateDirectory(saveDirectory);
        AudioListener.pause = true;
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RefreshGalleryDescription();
            CheckTeeth();
            CheckGum();
            File.WriteAllText(Folder + "/Verification.txt", "PASS\n" + checks + " checks.\n" +
                "Five-shot forward volleys from the ship, focused spread, faster cadence and travel, secondary pool reserve, gallery wording, attached gum and pooled pops using player colors and coatings across all districts.\n" +
                "No gameplay or visual review was started.\n");
            if (Environment.GetCommandLineArgs().Contains("--teeth-build")) Build();
            VerifyCleanCompilation.Run();
        }
        catch (Exception exception)
        {
            File.WriteAllText(Folder + "/Verification.txt", "FAIL\n" + exception);
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    static void RefreshGalleryDescription()
    {
        // Refresh only this exhibit's text, preserving the baked model, paint and other items.
        var shelf = AssetDatabase.LoadAssetAtPath<GalleryShelf>("Assets/Resources/Gallery/Shelf9.asset");
        var exhibit = shelf.exhibits.Single(e => e.key == "WindUpTeeth");
        var probe = new GameObject("Teeth metadata check").AddComponent<WindUpTeeth>();
        Call(probe, "Awake");
        string description = probe.GetData()[2];
        if (exhibit.description != description)
        {
            exhibit.description = description;
            EditorUtility.SetDirty(shelf);
            AssetDatabase.SaveAssetIfDirty(shelf);
        }
        Check(exhibit.description == "Rapid five-shot bursts. Focus tightens the spread.", "Gallery describes the new firing pattern");
        Check(exhibit.mesh != null && exhibit.paint != null, "Existing teeth artwork preserved");
        Object.DestroyImmediate(probe.gameObject);
    }

    static void CheckTeeth()
    {
        GameRules.ResetRun();
        var ship = new GameObject("Ship origin check").AddComponent<Shipcontroller>();
        ship.transform.position = new Vector3(2, -3, -1);
        var teeth = SecondaryShot.Attach(ship, SecondaryPattern.Teeth, "WindUpTeeth");
        // Put the visual well off to the side: the actual rounds must still leave the ship.
        var option = (Transform)typeof(SecondaryShot).GetField("option", Instance).GetValue(teeth);
        option.localPosition = new Vector3(-2, 0, 0);
        Near(teeth.ReleasesPerSecond, 7, "Twice the previous release cadence");
        var spreads = new List<float>();
        foreach (bool focus in new[] { false, true })
        {
            ClearRounds();
            Call(teeth, "Release", focus);
            var rounds = Object.FindObjectsByType<PlayerRound>();
            Check(rounds.Length == 5, "Five bullets per bite");
            foreach (var round in rounds)
            {
                Near(round.transform.position.x, ship.transform.position.x, "Muzzle stays on the ship centreline");
                Near(round.transform.position.y, ship.transform.position.y + .42f, "Shots leave the nose");
                Check(round.transform.up.y > .95f, "Every bite bullet travels forward");
                Near(round.speed, GameRules.playerShootTravelSpeed * 1.35f, "35 percent faster bullet travel");
                Near(round.damage, GameRules.playerShootDamage * .34f, "Per-bullet damage preserved");
                Check(round.generation == 1 && !round.returns, "Secondary fire remains bounded without recursive volleys");
            }
            spreads.Add(rounds.Max(r => r.transform.up.x) - rounds.Min(r => r.transform.up.x));
        }
        Check(spreads[1] < spreads[0] * .5f, "Focus narrows the five-shot fan");
        ClearRounds();
        typeof(PlayerRound).GetProperty("LiveCount").SetValue(null, PlayerRound.Limit - 50);
        Call(teeth, "Release", false);
        Check(Object.FindObjectsByType<PlayerRound>().Length == 0, "Keeps fifty pooled rounds available for primary weapons");
        typeof(PlayerRound).GetProperty("LiveCount").SetValue(null, 0);
        var paperclip = SecondaryShot.Attach(ship, SecondaryPattern.Paperclip, "PaperclipBox");
        Call(paperclip, "Release", false);
        Check(Object.FindObjectsByType<PlayerRound>().Length == 1, "Other secondary patterns keep their shot counts");
        Near(paperclip.ReleasesPerSecond, 1.65f, "Other secondary cadences unchanged");
        ClearRounds();
        Object.DestroyImmediate(ship.gameObject);
    }

    static void CheckGum()
    {
        GameRules.ResetRun();
        var target = new GameObject("Gum color target").AddComponent<TeethGumReviewTarget>();
        var upgrades = new GameObject("Gum coatings").AddComponent<CombatUpgrades>();
        typeof(CombatUpgrades).GetProperty("Current").SetValue(null, upgrades);
        var round = PlayerRound.Spawn(Vector3.zero, Vector2.up, 1, 1);
        var block = new MaterialPropertyBlock();
        GumSpritePop existing = null;
        foreach (LevelId district in Enum.GetValues(typeof(LevelId)))
        {
            LevelRun.Reset(district, 52001);
            Near(StickyGum.Stick(target, 8), 0, "Gum still stores hit damage");
            var bubble = target.GetComponentsInChildren<SpriteRenderer>().Last();
            GumSpritePop.Spawn(bubble.transform.position);
            var pop = Object.FindAnyObjectByType<GumSpritePop>();
            if (existing != null) Check(pop == existing, "Pop reuses its pooled sprite set");
            existing = pop;
            var sprites = pop.GetComponentsInChildren<SpriteRenderer>();
            Check(sprites.Length == 5, "Pop retains its five existing sprites");
            foreach (var sprite in sprites.Append(bubble))
            {
                var tint = sprite.GetComponent<ProjectileSpriteTint>();
                Check(tint.Tint == round.GetComponent<ProjectileSpriteTint>().Tint && tint.enabled,
                    district + " gum uses the player tint path");
                Check(sprite.sharedMaterial == round.GetComponent<SpriteRenderer>().sharedMaterial,
                    district + " gum uses the player material");
                Check(tint.Tint != LevelProjectilePalette.Accent(district), district + " gum differs from hostile fire");
                Check(sprite.sortingLayerName == "Bullet" && sprite.sortingOrder >= 24, "Attached gum stays visible over the enemy hull");
                Check(sprite.sprite.texture == ProjectileSprites.Load("standard").texture, "Approved neutral sprite preserved");
            }
            pop.gameObject.SetActive(false);
            ((Stack<GumSpritePop>)typeof(GumSpritePop).GetField("pool", Static).GetValue(null)).Push(pop);
        }
        // Compare the actual shader properties after each coating combination, including removal.
        foreach (int coatings in new[] { 0, 1, 3, 2, 0 })
        {
            upgrades.Remove(UpgradeKind.Gasoline); upgrades.Remove(UpgradeKind.Poison);
            if ((coatings & 1) != 0) upgrades.Add(UpgradeKind.Gasoline);
            if ((coatings & 2) != 0) upgrades.Add(UpgradeKind.Poison);
            Call(round.GetComponent<ProjectileSpriteTint>(), "LateUpdate");
            round.GetComponent<SpriteRenderer>().GetPropertyBlock(block);
            float burn = block.GetFloat("_Burn"), poison = block.GetFloat("_Poison");
            var tintColor = block.GetColor("_SpriteTint");
            foreach (var sprite in target.GetComponentsInChildren<SpriteRenderer>().Concat(existing.GetComponentsInChildren<SpriteRenderer>(true)))
            {
                Call(sprite.GetComponent<ProjectileSpriteTint>(), "LateUpdate");
                sprite.GetPropertyBlock(block);
                Check(block.GetColor("_SpriteTint") == tintColor && block.GetFloat("_Burn") == burn && block.GetFloat("_Poison") == poison,
                    "Gum matches the player's current fire/poison coating combination");
            }
        }
        Check(target.GetComponent<StickyGum>().Count == 12, "Existing gum attachment cap preserved");
        Object.DestroyImmediate(target.gameObject);
        Object.DestroyImmediate(existing.gameObject);
        ((Stack<GumSpritePop>)typeof(GumSpritePop).GetField("pool", Static).GetValue(null)).Clear();
        Object.DestroyImmediate(upgrades.gameObject);
        ClearRounds();
    }

    static void ClearRounds()
    {
        foreach (var round in Object.FindObjectsByType<PlayerRound>(FindObjectsInactive.Include))
            Object.DestroyImmediate(round.gameObject);
        typeof(PlayerRound).GetMethod("ResetPool", Static).Invoke(null, null);
    }
    static void Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Instance).Invoke(target, args);
    static void Near(float value, float expected, string label) => Check(Mathf.Abs(value - expected) < .001f, label);
    static void Check(bool ok, string label)
    {
        checks++;
        if (!ok) throw new Exception(label);
    }
    static void Build()
    {
        string output = Path.GetFullPath("Build/TeethAndGum/Shootemup.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            target = BuildTarget.StandaloneWindows64, locationPathName = output, options = BuildOptions.None
        });
        File.WriteAllText(Folder + "/Build.txt", report.summary.result + "\n" + output + "\nErrors=" + report.summary.totalErrors + "; warnings=" + report.summary.totalWarnings);
        Check(report.summary.result == BuildResult.Succeeded, "Windows build");
    }
}

public sealed class TeethGumReviewTarget : AEnemy
{
    public override void damage(float amount) => health -= amount;
}
#endif
