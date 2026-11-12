#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

// Headless, edit-time integration checks. No Play mode and no access to the player's save.
public static class VerifyWarningCargoPrompts
{
    const string Folder = "ReviewOutput/WarningCargoPrompts";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static int checks;
    public static void Run()
    {
        if (!Application.dataPath.Replace('\\', '/').EndsWith("/Temp/UnlockReview/Assets", StringComparison.Ordinal))
            throw new Exception("Use the isolated UnlockReview checkout");
        Directory.CreateDirectory(Folder);
        MetaProgress.ReviewSaveDirectory = Path.GetFullPath(Folder + "/Save-" + DateTime.UtcNow.Ticks);
        AudioListener.pause = true;
        try
        {
            BakeInputPrompts.Run();
            CheckPrompts();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CheckWarning();
            CheckCargo();
            File.WriteAllText(Folder + "/Verification.txt", "PASS\n" + checks + " checks.\n" +
                "Authored button assets, all default bindings, keyboard/controller overrides, TMP inline sprites, warning beam/round clear, restored tow, cargo rarity and reserved decision ownership.\nNo gameplay or visual review was started.\n");
            if (Environment.GetCommandLineArgs().Contains("--cargo-build")) Build();
            VerifyCleanCompilation.Run();
        }
        catch (Exception exception)
        {
            File.WriteAllText(Folder + "/Verification.txt", "FAIL\n" + exception);
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }
    static void CheckPrompts()
    {
        var atlas = InputPrompts.InlineAsset;
        Check(atlas != null && atlas.material != null && atlas.spriteSheet != null, "Baked prompt atlas and material");
        var paths = Directory.GetFiles("Assets/Resources/InputPrompts/Icons", "*.png");
        Check(atlas.spriteCharacterTable.Count == paths.Length, "Every PNG has an inline glyph");
        foreach (var path in paths)
        {
            string name = Path.GetFileNameWithoutExtension(path);
            Check(Resources.Load<Sprite>("InputPrompts/Icons/" + name) != null, "Imported sprite " + name);
            Check(atlas.GetSpriteIndexFromName(name) >= 0, "Inline glyph " + name);
        }
        var actions = new Inputs();
        try
        {
            foreach (var action in actions)
                foreach (var binding in action.bindings.Where(b => !b.isComposite))
                {
                    bool keyboard = binding.path.StartsWith("<Keyboard>");
                    string control = binding.path.Substring(binding.path.IndexOf('>') + 2);
                    foreach (var device in keyboard ? new[] { PromptDevice.Keyboard } : new[] { PromptDevice.PlayStation, PromptDevice.Xbox })
                        Check(atlas.GetSpriteIndexFromName(InputPrompts.KeyForControl(control, device)) >= 0, "Default binding " + binding.path + " / " + device);
                }
            var fire = actions.PlayerControls.FirePrimary;
            fire.ApplyBindingOverride(0, "<Keyboard>/f7");
            fire.ApplyBindingOverride(1, "<Gamepad>/buttonNorth");
            Check(InputPrompts.Key(fire, PromptDevice.Keyboard) == "key_f7", "Keyboard override");
            Check(InputPrompts.Key(fire, PromptDevice.PlayStation) == "ps_triangle", "PlayStation override");
            Check(InputPrompts.Key(fire, PromptDevice.Xbox) == "xbox_y", "Xbox override");
            Check(InputPrompts.Key(actions.PlayerControls.Accept, PromptDevice.PlayStation) == "ps_cross", "Cross confirms");
            Check(InputPrompts.Key(actions.PlayerControls.Back, PromptDevice.PlayStation) == "ps_circle", "Circle returns");
        }
        finally { Object.DestroyImmediate(actions.asset); }
        var label = new GameObject("Inline prompt check", typeof(RectTransform), typeof(Canvas), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        label.SendMessage("Awake");
        label.font = TMP_Settings.defaultFontAsset;
        label.spriteAsset = atlas;
        label.rectTransform.sizeDelta = new Vector2(500, 80);
        label.text = InputPrompts.InlineKey("ps_triangle") + InputPrompts.InlineKey("xbox_y") + InputPrompts.InlineKey("key_c");
        label.ForceMeshUpdate(true, true);
        Check(label.textInfo.characterCount == 3 && label.textInfo.characterInfo.Take(3).All(c =>
            c.elementType == TMP_TextElementType.Sprite && c.textElement.textAsset == atlas && c.isVisible && c.topRight.x > c.bottomLeft.x),
            "TMP produces three visible image quads from the prompt atlas");
        Object.DestroyImmediate(label.gameObject);
    }
    static void CheckWarning()
    {
        var shots = new CombatRound[] {
            CombatRound.Launch<SimpleRound>(Vector3.zero, Vector2.down),
            CombatRound.Launch<NeedleRound>(Vector3.right, Vector2.up),
            CombatRound.Launch<PlasmaOrb>(Vector3.left, Vector2.down),
            CombatRound.Launch<PulseLaser>(Vector3.up, Vector2.down).Sustain()
        };
        Check(shots.All(s => s != null && s.gameObject.activeSelf), "Live round, needle, destructible orb and sustained beam");
        Warning.ClearField();
        Check(shots.All(s => !s.gameObject.activeSelf), "Warning clears every hostile delivery including sustained beams");
        Check(ClearEffects.Flash > .99f, "Warning starts the full-screen clear");
        Warning.ClearField();
        Check(ClearEffects.Flash > .99f, "Empty warning still shows its clear");
        foreach (var shot in shots) Object.DestroyImmediate(shot.gameObject);
        CombatRound.Live.Clear();
    }
    static void CheckCargo()
    {
        var shipObject = new GameObject("Cargo check ship");
        var ship = shipObject.AddComponent<Shipcontroller>();
        shipObject.AddComponent<ShipLoadout>();
        FlightTrials.Reset();
        CombatDecision.End();
        var cargo = CargoEscort.Create(ship, _ => { });
        Check(cargo.Hits == 3 && cargo.Quality == ItemRarity.Common, "Cargo begins intact and common");
        Check(Mathf.Approximately(Vector2.Distance(cargo.transform.position, ship.transform.position), .85f), "Original spawn distance");
        Check(CargoEscort.FollowDistance == .95f && CargoEscort.FollowSmoothing == .3f && CargoEscort.FollowSpeed == 1.4f, "Original trailing distance, damping and speed");
        Check(cargo.GetComponentInChildren<SpriteRenderer>() == null, "No world-space button prompt");
        Check(cargo.transform.Find("Strapped salvage crate/Hinged crate lid") != null, "Separate attached opening lid");
        Check(InputPrompts.Key(cargo.OpenControl, PromptDevice.PlayStation) == "ps_triangle" && InputPrompts.Key(cargo.OpenControl, PromptDevice.Xbox) == "xbox_y", "Cargo controller controls");
        for (int tier = 1; tier <= 4; tier++)
        {
            Check(!cargo.FinishWave(), "Ordinary cargo never auto-delivers");
            Check((int)cargo.Quality == Math.Min(tier, 3), "Rarity progression caps at legendary");
        }
        CombatDecision.Begin();
        Check(!cargo.TryClaim(), "Cannot claim during a decision");
        CombatDecision.End();
        Object.DestroyImmediate(cargo.gameObject);
        if (!FlightTrials.All[1].TrySelect()) throw new System.InvalidOperationException("Cold Delivery must be available");
        cargo = CargoEscort.Create(ship);
        Check(cargo.OpenControl == null && !cargo.FinishWave() && cargo.FinishWave(), "Cold Delivery retains its two-wave objective");
        Object.DestroyImmediate(cargo.gameObject);
        FlightTrials.Reset();

        var plan = new RunPlan();
        plan.Load(new[] { "2", "encounterChoice", "2", "boss" });
        plan.Insert(1, RunSlotKind.CargoReward);
        Check(plan.Following(1).IsDecision && plan.RemainingCombat(1) == 1, "Cargo creates a clear decision boundary without consuming a combat wave");
        var dialogObject = new GameObject("Dialog", typeof(RectTransform));
        dialogObject.tag = "Dialog";
        dialogObject.AddComponent<Dialog>().font = TMP_Settings.defaultFontAsset;
        var flow = new GameObject("Cargo owner").AddComponent<WaveOrchestrer>();
        // A fresh profile has no legendary items. Earn one through the real progression
        // path before checking a legendary delivery; do not bypass the unlock gate.
        Difficulty.Select(RunDifficulty.Normal);
        LevelRun.Reset(LevelId.Void, 17381);
        MetaProgress.BeginRun();
        MetaProgress.EngageBoss();
        MetaProgress.DefeatBoss();
        MetaProgress.EndRun("Won");
        for (int tier = 0; tier < 4; tier++)
        {
            ItemPool.ResetRun();
            string key = ItemPool.GetCargoReward(out var rarity, (ItemRarity)tier);
            Check(key != null && rarity == ((ItemRarity)tier).ToString().ToLowerInvariant(), "Exact reserved cargo rarity " + tier);
            // Simulate the last available item: the reserved reward must survive an empty pool.
            while (ItemPool.RemainingCount > 0) ItemPool.GetItem(out _);
            Check(ItemPool.GetCargoReward(out var emptyRarity, (ItemRarity)tier) == null && emptyRarity == rarity, "Exhaustion never substitutes another rarity");
            var shop = flow.gameObject.AddComponent<DecisionShop>();
            shop.Open(flow, ship, RunSlotKind.CargoReward, () => { }, reservedKey: key, reservedRarity: rarity);
            var items = (List<AItem>)typeof(DecisionShop).GetField("items", Private).GetValue(shop);
            Check(items.Count == 1 && items[0].GetType() == ItemCatalog.Get(key).ComponentType && items[0].rarity == rarity,
                "Decision receives exactly the previewed item even after pool exhaustion");
            Check(!shop.paid && !items[0].Settled && !items[0].CanPick(ship), "Free ordinary item entrance blocks early pickup");
            Check(ReferenceEquals(typeof(AItem).GetField("waveOrchestrer", Private).GetValue(items[0]), flow), "Cargo reward uses normal decision ownership");
            foreach (var item in items) Object.DestroyImmediate(item.gameObject);
            typeof(DecisionShop).GetField("closed", Private).SetValue(shop, true);
            Object.DestroyImmediate(shop);
            foreach (var overlay in Object.FindObjectsByType<DecisionOverlay>()) Object.DestroyImmediate(overlay.gameObject);
            CombatDecision.End();
        }
        Object.DestroyImmediate(flow.gameObject);
        Object.DestroyImmediate(dialogObject);
        Object.DestroyImmediate(shipObject);
    }
    static void Build()
    {
        string output = Path.GetFullPath("Build/CargoPrompts/Shootemup.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            target = BuildTarget.StandaloneWindows64, locationPathName = output, options = BuildOptions.None
        });
        File.WriteAllText(Folder + "/Build.txt", report.summary.result + "\nErrors=" + report.summary.totalErrors + "; warnings=" + report.summary.totalWarnings);
        Check(report.summary.result == BuildResult.Succeeded && report.summary.totalWarnings == 0, "Clean Windows build");
    }
    static void Check(bool value, string message)
    {
        checks++;
        if (!value) throw new Exception(message);
    }
}
#endif
