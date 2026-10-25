#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

public sealed class DecisionFeedbackProbe : MonoBehaviour
{
    readonly List<string> checks = new List<string>(), errors = new List<string>();
    Shipcontroller ship; ShipLoadout loadout; WaveOrchestrer waves; Keyboard keyboard;
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Application.runInBackground = true;
        Application.logMessageReceived += Log;
    }

    void Log(string text, string trace, LogType kind)
    {
        if ((kind == LogType.Error || kind == LogType.Exception) && !trace.Contains("UnityEditor.Search"))
            errors.Add(text);
    }

    IEnumerator Start()
    {
        var stack = new Stack<IEnumerator>();
        stack.Push(Run());
        while (stack.Count > 0)
        {
            object step = null;
            bool more = false;
            Exception error = null;
            try
            {
                more = stack.Peek().MoveNext();
                if (more)
                    step = stack.Peek().Current;
            }
            catch (Exception ex) { error = ex; }
            if (error != null)
            {
                errors.Add(error.ToString());
                break;
            }
            if (!more)
            {
                stack.Pop();
                continue;
            }
            if (step is IEnumerator nested)
            {
                stack.Push(nested);
                continue;
            }
            yield return step;
        }
        if (keyboard != null)
            InputSystem.RemoveDevice(keyboard);
        string result = (errors.Count == 0 ? "PASS" : "FAIL") + "\n" + string.Join("\n", checks) + "\n" + string.Join("\n", errors);
        File.WriteAllText("ArtSource/CombatReview/DecisionFeedbackVerification.txt", result);
        File.WriteAllText("Temp/CombatResult.txt", result);
    }

    void Check(bool pass, string text)
    {
        if (!pass)
            throw new Exception(text);
        checks.Add("PASS " + text);
    }

    void Press(params Key[] keys)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
    }

    void Clear()
    {
        foreach (var enemy in FindObjectsByType<AEnemy>())
            Destroy(enemy.gameObject);
        foreach (var shot in FindObjectsByType<AEnemyBullet>())
            shot.Vanish();
        foreach (var shot in FindObjectsByType<ABullet>())
            Destroy(shot.gameObject);
        foreach (var pickup in FindObjectsByType<APickable>())
            Destroy(pickup.gameObject);
    }

    T Item<T>(Vector3 p) where T : AItem => Instantiate(Resources.Load<GameObject>("Items/Item"), p, Quaternion.identity).AddComponent<T>();
    IEnumerator Capture(string name)
    {
        yield return new WaitForEndOfFrame();
        var t = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes("ArtSource/CombatReview/" + name + ".png", t.EncodeToPNG());
        Destroy(t);
    }

    IEnumerator Run()
    {
        GameRules.StartNewRun();
        LevelRun.Reset();
        Time.timeScale = 1;
        if (SceneManager.GetActiveScene().name != "L1")
            yield return SceneManager.LoadSceneAsync("L1");
        yield return new WaitForSecondsRealtime(1.2f);
        Time.timeScale = 1;
        ship = FindAnyObjectByType<Shipcontroller>();
        ship.health = ship.maxHealth = 10000;
        loadout = ship.GetComponent<ShipLoadout>();
        waves = FindAnyObjectByType<WaveOrchestrer>();
        waves.enabled = false;
        waves.CancelInvoke();
        waves.StopAllCoroutines();
        Clear();
        keyboard = InputSystem.AddDevice<Keyboard>();
        ship.transform.position = new Vector3(0, -2, -1);
        yield return null;
        Check(!UnityEditor.ShaderUtil.ShaderHasError(CombatVisuals.ProjectileMaterial.shader), "projectile overlay shader compiles");
        var shot = CombatRound.Launch<SimpleRound>(ship.transform.position, Vector2.up, 0, .3f);
        shot.transform.position += Vector3.forward * 22;
        shot.GetComponent<Collider2D>().enabled = false;
        yield return Capture("BulletOverShip");
        shot.Vanish();
        yield return null;
        loadout.EquipWeapon("NeedleCannon", 0);
        loadout.EquipWeapon("BurstLauncher", 1);
        yield return null;
        CombatDecision.Begin();
        Press(Key.X);
        yield return null;
        Press();
        yield return null;
        var primary = loadout.Weapons[0];
        var secondary = loadout.Weapons[1];
        var weapon = Item<RailDriverItem>(new Vector3(0, 2, 1));
        yield return null;
        Check(loadout.HoveredItem == null, "no replacement prompt away from an item");
        ship.transform.position = new Vector3(0, 2, -1);
        yield return new WaitForSeconds(.15f);
        Check(!weapon.Collected && loadout.HoveredItem == weapon, "previous fire press does not collect a full-loadout weapon");
        Press(Key.A);
        yield return null;
        Press();
        yield return null;
        Check(!weapon.Collected, "active control cannot replace a weapon");
        yield return Capture("HoverWeaponChoice");
        Press(Key.X);
        yield return null;
        Press();
        Check(weapon.Collected && loadout.Weapons[0] == primary && loadout.Weapons[1] is RailDriver, "secondary control immediately picks and replaces only the secondary weapon");
        yield return null;
        Check(loadout.HoveredItem == null, "replacement prompts disappear after collection");
        Destroy(loadout.Weapons[1].gameObject);
        loadout.Weapons[1] = null;
        var emptySlot = Item<BurstLauncherItem>(ship.transform.position + Vector3.forward * 2);
        yield return new WaitForSeconds(.25f);
        Check(emptySlot == null || emptySlot.Collected, "empty weapon slot collects without a choice");
        Check(loadout.Weapons[1] is BurstLauncher, "empty secondary slot is filled automatically");
        ship.transform.position = new Vector3(0, -2, -1);
        var left = Item<GravityWell>(new Vector3(3, 4, 1));
        var right = Item<MirrorBurst>(new Vector3(-3, 4, 1));
        yield return null;
        typeof(AItem).GetProperty("ReplacementSlot").SetValue(left, 0);
        left.OnPickup();
        typeof(AItem).GetProperty("ReplacementSlot").SetValue(right, 1);
        right.OnPickup();
        var active = Item<Overdrive>(new Vector3(0, 2, 1));
        yield return null;
        ship.transform.position = new Vector3(0, 2, -1);
        yield return new WaitForSeconds(.15f);
        Check(loadout.HoveredItem == active, "occupied active slots show their own hover choice");
        yield return Capture("HoverActiveChoice");
        float mana = ship.mana;
        Press(Key.S);
        yield return null;
        Press();
        yield return null;
        Check(loadout.Actives[0] == left && loadout.Actives[1] == active && active.Collected, "active press immediately replaces only the selected active");
        Check(ship.mana == mana && active.lastCD > active.cd, "replacement press does not activate the purchased item");
        CombatDecision.End();
        Clear();
        yield return null;
        foreach (string kind in new[] { "coinShop", "multiplierShop" })
        {
            ship.transform.position = new Vector3(0, -1, -1);
            Score.ResetRun();
            Score.IncreaseScore(15000);
            RunCoins.Reset();
            RunCoins.Add(200);
            var shop = waves.gameObject.AddComponent<DecisionShop>();
            bool closed = false;
            int exitScore = -1, exitMulti = -1;
            shop.Open(waves, ship, kind, () => { closed = true; exitScore = Score.CurrentScore; exitMulti = Score.CurrentMultiplier; });
            yield return new WaitForSeconds(2.1f);
            var overlay = FindAnyObjectByType<DecisionOverlay>();
            Check(overlay != null && overlay.GetComponent<Canvas>().renderMode == RenderMode.ScreenSpaceOverlay, "shop labels bypass the world filter");
            Check(overlay.GetComponentsInChildren<Image>().Where(x => x.name == "Offer label").All(x => x.color.a == 1), "shop labels have opaque backgrounds");
            var offerName = overlay.GetComponentsInChildren<TMPro.TextMeshProUGUI>().First(x => x.transform.parent.name == "Offer label");
            var savedName = offerName.text;
            offerName.text = "<nobr>Telekinesis</nobr>";
            offerName.ForceMeshUpdate();
            Check(offerName.textInfo.lineCount == 1 && !offerName.isTextOverflowing, "long item names fit without splitting a word");
            offerName.text = savedName;
            yield return Capture(kind + "Readable");
            int score = Score.CurrentScore, multi = Score.CurrentMultiplier;
            // Real movement reaches the HUD's visible-bottom clamp; no teleport below the screen.
            Press(Key.DownArrow);
            yield return new WaitForSeconds(1);
            Press();
            yield return new WaitForSeconds(.2f);
            Check(shop != null && shop.SkipProgress > 0, "holding at the reachable visible bottom charges skip");
            yield return Capture("BottomSkip");
            Press(Key.UpArrow);
            yield return new WaitForSeconds(.25f);
            Press();
            yield return null;
            Check(shop.SkipProgress == 0, "leaving the bottom cancels skip");
            Press(Key.DownArrow);
            yield return new WaitForSeconds(2);
            Press();
            yield return null;
            Check(closed && !CombatDecision.Active, "bottom hold skips the shop and releases the decision");
            Check(exitScore == score && exitMulti == multi, "skipping spends no currency");
            yield return new WaitForSeconds(.6f);
        }
        // Cover the previous batch's final-boss return, equipment preservation and side-margin HUD.
        LevelRun.Reset(LevelId.Tunnel);
        LevelEnvironment.Apply();
        ship.transform.position = new Vector3(0, -2, -1);
        var equipment = loadout.Weapons[0];
        int savedScore = Score.CurrentScore;
        typeof(WaveOrchestrer).GetMethod("SpawnBoss", Hidden).Invoke(waves, null);
        yield return new WaitForSeconds(.25f);
        yield return Capture("BossHealthMargin");
        FindAnyObjectByType<TunnelSawBoss>().damage(100000);
        yield return new WaitForSeconds(1.1f);
        Check(!LevelChoice.Active, "final boss returns without a redundant route choice");
        float deadline = Time.realtimeSinceStartup + 15;
        while (CloudTransition.Busy && Time.realtimeSinceStartup < deadline)
            yield return null;
        Check(LevelRun.Current == LevelId.Fields && LevelRun.Stage == 4 && LevelRun.Loop == 2, "final boss loops to harder Plains");
        Check(loadout.Weapons[0] == equipment && Score.CurrentScore >= savedScore + 1600, "loop preserves equipment and score including the boss award");
        waves.enabled = false;
        waves.CancelInvoke();
        waves.StopAllCoroutines();
        Clear();
        yield return null;
        yield return Capture("ProgressionSideHUD");
        ship.health = 1;
        CombatDecision.End();
        ship.ReceiveHazardDamage(100);
        yield return new WaitForSecondsRealtime(.8f);
        Check(GameOverState.Active && GameRules.playerDisabled && !waves.enabled, "death enters a single game-over state and stops combat");
        Check(FindObjectsByType<GameOverState>().Length == 1, "game over cannot duplicate");
        Check(FindObjectsByType<LevelReveal>().Length == 0, "game over dismisses any level-intro title");
        yield return Capture("GameOver");
        deadline = Time.realtimeSinceStartup + 16;
        while ((SceneManager.GetActiveScene().name != "Title" || CloudTransition.Busy) && Time.realtimeSinceStartup < deadline)
            yield return null;
        Check(SceneManager.GetActiveScene().name == "Title" && !GameOverState.Active, "game over returns to the title");
        FindAnyObjectByType<NewGame>().OnAccept();
        deadline = Time.realtimeSinceStartup + 15;
        while (CloudTransition.Busy && Time.realtimeSinceStartup < deadline)
            yield return null;
        Check(LevelRun.Current == LevelId.Fields && LevelRun.Stage == 1 && !GameRules.playerDisabled, "new game after death restores controls and fresh progression");
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= Log;
    }
}
#endif
