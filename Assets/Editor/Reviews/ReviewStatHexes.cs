#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Isolated edit-mode integration and native UI preview; never starts a gameplay run.
public static class ReviewStatHexes
{
    const string Folder = "ArtSource/StatHexReview";
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Set(object obj, string name, object value) => obj.GetType().GetField(name, Hidden).SetValue(obj, value);
    static object Call(object obj, string name, params object[] args) => obj.GetType().GetMethod(name, Hidden).Invoke(obj, args);

    public static void Run() => Review(false);
    public static void Offers() => Review(true);

    static void Review(bool offersOnly)
    {
        if (!Application.dataPath.Replace('\\', '/').Contains("/Temp/StatHexReview/"))
            throw new Exception("Use the isolated Temp/StatHexReview project");
        Directory.CreateDirectory(Folder);
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/L1.unity");
            AudioListener.pause = true;
            CheckOffers();
            var ship = UnityEngine.Object.FindAnyObjectByType<Shipcontroller>();
            Check(ship != null, "Missing scene ship");
            if (ship.GetComponent<CombatUpgrades>() == null) ship.gameObject.AddComponent<CombatUpgrades>();
            if (!offersOnly) CheckRewards(ship);
            Render(ship);
            string offers = "PASS\nIsolated editor checks and production RewardHexes rendering; no Play mode.\n" +
                "1024 seeds: two/three/four choices, unique compatible offers, deterministic draws, Glass Hull exclusion and guaranteed due pacts.\n" +
                "Four-zone layouts stay inside the arena, keep separate flight corridors and avoid the ship across 1024 seeds/positions.\n" +
                "10000 evenly spaced rolls: zero luck gives 75/20/5 percent; high luck caps at 35/45/20 percent.\n" +
                "Rendered two, three and four choices, including the four-zone hover and diamond layout.\n";
            File.WriteAllText(Folder + (offersOnly ? "/OfferVerification.txt" : "/Verification.txt"), offers + (offersOnly ? "" :
                "Actual hex selection resolves all four rewards immediately, expires receipts after five seconds, adds no decision waves.\n" +
                "Repeated grants stack; extra-shot rebuilds both weapon spawn arrays; Overdrive expiry preserves damage bonus.\n" +
                "Health grows by one and queues one repair; Glass Hull blocks growth. Trading an older item preserves later bonuses.\n"));
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            File.WriteAllText(Folder + (offersOnly ? "/OfferVerification.txt" : "/Verification.txt"), "FAIL\n" + e);
            EditorApplication.Exit(1);
        }
    }

    static void CheckOffers()
    {
        var seen = new System.Collections.Generic.HashSet<EncounterKind>();
        var counts = new int[5];
        var place = typeof(RewardHexes).GetMethod("Place", BindingFlags.Static | BindingFlags.NonPublic);
        var four = new[] { EncounterKind.AttackSpeed, EncounterKind.AttackDamage, EncounterKind.MaxHealth, EncounterKind.ExtraShot };
        for (int seed = 0; seed < 1024; seed++)
        {
            var options = EncounterChoice.Draw(seed, true, 6, 6);
            Check(options.SequenceEqual(EncounterChoice.Draw(seed, true, 6, 6)), "Nondeterministic offer");
            Check(options.Length >= 2 && options.Length <= 4 && options.Distinct().Count() == options.Length, "Invalid option count");
            counts[options.Length]++;
            Check(!options.Contains(EncounterKind.Health) && !options.Contains(EncounterKind.HealthTrade), "Healthy repair offer");
            Check(!(options.Contains(EncounterKind.Item) && options.Contains(EncounterKind.Challenge)), "Incompatible encounter pair");
            foreach (var kind in options) seen.Add(kind);
            Check(!EncounterChoice.Draw(seed, true, 1, 1, canGrowHull: false).Contains(EncounterKind.MaxHealth), "Glass Hull offered capacity");
            var pact = EncounterChoice.Draw(seed, true, 3, 6, true, 1, true, true);
            Check(pact.Contains(EncounterKind.Pact) && pact.Length <= 4 && pact.Distinct().Count() == pact.Length, "Invalid pact offer");
            Check(!(pact.Contains(EncounterKind.Item) && pact.Contains(EncounterKind.Challenge)) &&
                !(pact.Contains(EncounterKind.Health) && pact.Contains(EncounterKind.HealthTrade)), "Incompatible pact offer");
            var avoid = new Vector2(Mathf.Lerp(Camera.LeftLimit, Camera.RightLimit, seed % 32 / 31f),
                Mathf.Lerp(Camera.DownLimit, Camera.UpLimit, seed / 32 / 31f));
            var spots = (Vector2[])place.Invoke(null, new object[] { four, seed, avoid });
            for (int i = 0; i < spots.Length; i++)
            {
                Check(spots[i].x - RewardHexes.Radius > Camera.LeftLimit && spots[i].x + RewardHexes.Radius < Camera.RightLimit &&
                    spots[i].y - RewardHexes.Radius > Camera.DownLimit && spots[i].y + RewardHexes.Radius < Camera.UpLimit, "Clipped four-zone offer");
                Check(Vector2.Distance(spots[i], avoid) > RewardHexes.Radius + .15f, "Four-zone offer spawned on the pilot");
                for (int j = 0; j < i; j++)
                    Check(Vector2.Distance(spots[i], spots[j]) > RewardHexes.Radius * 2 + .2f, "Four-zone corridor blocked");
            }
        }
        Check(counts[2] > 0 && counts[3] > 0 && counts[4] > 0, "An offer size is unreachable");
        var baseline = new int[5];
        var capped = new int[5];
        for (int i = 0; i < 10000; i++)
        {
            float roll = (i + .5f) / 10000;
            baseline[EncounterChoice.OptionCount(0, roll)]++;
            capped[EncounterChoice.OptionCount(10, roll)]++;
            Check(EncounterChoice.OptionCount(-1, roll) == EncounterChoice.OptionCount(0, roll), "Negative luck changed baseline");
        }
        Check(baseline[2] == 7500 && baseline[3] == 2000 && baseline[4] == 500, "Wrong baseline odds");
        Check(capped[2] == 3500 && capped[3] == 4500 && capped[4] == 2000, "Wrong capped odds");
        foreach (EncounterKind kind in Enum.GetValues(typeof(EncounterKind)))
            if (EncounterChoice.IsStatReward(kind))
            {
                Check(seen.Contains(kind), "Unreachable stat: " + kind);
                var icon = EncounterChoice.Icon(kind);
                Check(icon.texture.filterMode == FilterMode.Point && icon.texture.mipmapCount == 1, "Blurred icon: " + kind);
            }
    }

    static void CheckRewards(Shipcontroller ship)
    {
        GameRules.ResetRun();
        PactPool.ResetRun();
        var inventory = RunInventory.For(ship);
        var flow = new GameObject("Stat reward resolution check").AddComponent<WaveOrchestrer>();
        Set(flow, "ship", ship.gameObject);
        var first = new GameObject("Primary check gun").AddComponent<BaseWeapon>();
        var second = new GameObject("Secondary check gun").AddComponent<BaseWeapon>();
        first.transform.SetParent(ship.transform);
        second.transform.SetParent(ship.transform);
        float health = ship.maxHealth;
        var kinds = new[] { EncounterKind.AttackSpeed, EncounterKind.AttackDamage, EncounterKind.MaxHealth, EncounterKind.ExtraShot };
        foreach (var kind in kinds)
        {
            var hex = RewardHexes.Open(new[] { kind, EncounterKind.Salvage }, ship.transform, 42,
                i => { if (i == 0) Call(flow, "ResolveEncounter", kind); });
            Call(hex, "Choose", 0);
            // A second callback invocation must not grant again after the offer was dismissed.
            Call(hex, "Choose", 0);
            Check(flow.ShowingEncounter(kind), "Missing receipt: " + kind);
            var expiry = (float[])typeof(WaveOrchestrer).GetField("encounterExpiry", Hidden).GetValue(flow);
            Check(Mathf.Abs(expiry[(int)kind] - Time.time - 5) < .01f, "Receipt never expires");
            UnityEngine.Object.DestroyImmediate(hex.gameObject);
        }
        Check(flow.waves.Length == 0, "Stat bonus queued a decision");
        Check(Mathf.Approximately(GameRules.playerShootSpeed, 10.8f) && Mathf.Approximately(GameRules.playerShootDamage, 2.25f), "Incorrect stat amount or duplicate claim");
        Check(ship.maxHealth == health + 1 && ship.GetComponent<HealthRecovery>() != null, "Missing hull segment/repair");
        Check((int)typeof(HealthRecovery).GetField("remaining", Hidden).GetValue(ship.GetComponent<HealthRecovery>()) == 1, "Wrong repair count");
        foreach (var gun in new[] { first, second })
            Check(((Vector3[])typeof(AWeapon).GetField("spawnPoints", Hidden).GetValue(gun)).Length == 3, "Extra shot did not update both guns");
        EncounterChoice.ApplyStatReward(EncounterKind.AttackSpeed, ship);
        Check(Mathf.Approximately(GameRules.playerShootSpeed, 11.6f), "Repeated stat rewards do not stack");
        Check(inventory.Tradeable().Count == 0, "Hex became a trade item");
        // Edit mode does not run MonoBehaviour lifetime methods, so invoke these explicitly.
        var boost = ship.gameObject.AddComponent<OverdriveBoost>();
        Call(boost, "Awake");
        EncounterChoice.ApplyStatReward(EncounterKind.AttackDamage, ship);
        Call(boost, "OnDestroy");
        Check(Mathf.Approximately(GameRules.playerShootDamage, 2.5f), "Overdrive diluted the permanent bonus");
        UnityEngine.Object.DestroyImmediate(boost);
        PactPool.BindGlassHull();
        EncounterChoice.ApplyStatReward(EncounterKind.MaxHealth, ship);
        Check(ship.maxHealth == health + 1, "Capacity broke Glass Hull");
        PactPool.ResetRun();
        // An earlier multiplicative item must not divide a later hex bonus on removal.
        var item = new GameObject("Stat history check").AddComponent<FocusTime>();
        Call(item, "Awake");
        var before = inventory.Capture();
        GameRules.playerShootDamage *= 2;
        inventory.Record(item, before, null);
        var entry = inventory.Entries.Last();
        EncounterChoice.ApplyStatReward(EncounterKind.AttackDamage, ship);
        Check(inventory.Remove(entry) && Mathf.Approximately(GameRules.playerShootDamage, 2.75f), "Trade removed a later stat bonus");
        UnityEngine.Object.DestroyImmediate(item.gameObject);
        UnityEngine.Object.DestroyImmediate(flow.gameObject);
        GameRules.ResetRun();
        Check(GameRules.playerShootDamage == 2 && GameRules.playerShootSpeed == 10 && GameRules.playerShootNumber == 2, "Run bonuses survived stat reset");
    }

    static void Render(Shipcontroller ship)
    {
        foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include)) canvas.enabled = false;
        foreach (var old in UnityEngine.Object.FindObjectsByType<UnityEngine.Camera>(FindObjectsInactive.Include)) old.tag = "Untagged";
        var camera = new GameObject("Stat hex preview camera").AddComponent<UnityEngine.Camera>();
        camera.tag = "MainCamera";
        camera.orthographic = true;
        camera.orthographicSize = (Camera.UpLimit - Camera.DownLimit) / 2;
        camera.transform.position = new Vector3(0, (Camera.UpLimit + Camera.DownLimit) / 2, -10);
        camera.nearClipPlane = .01f;
        camera.farClipPlane = 100;
        camera.cullingMask = 1 << 31;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.05f, .08f, .12f);
        var target = new RenderTexture(960, 720, 24);
        camera.targetTexture = target;
        var backdrop = new GameObject("Prior gameplay capture", typeof(RectTransform), typeof(Canvas));
        var backCanvas = backdrop.GetComponent<Canvas>();
        backCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        backCanvas.worldCamera = camera;
        backCanvas.planeDistance = 2;
        var picture = new GameObject("Backdrop", typeof(RectTransform), typeof(RawImage));
        picture.transform.SetParent(backdrop.transform, false);
        var rect = (RectTransform)picture.transform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        var texture = new Texture2D(2, 2);
        texture.LoadImage(File.ReadAllBytes("ArtSource/RiverbankReview/After-18.0.png"));
        texture.filterMode = FilterMode.Point;
        picture.GetComponent<RawImage>().texture = texture;
        foreach (var t in backdrop.GetComponentsInChildren<Transform>()) t.gameObject.layer = 31;
        var groups = new[] {
            new[] { EncounterKind.AttackSpeed, EncounterKind.AttackDamage },
            new[] { EncounterKind.MaxHealth, EncounterKind.ExtraShot },
            new[] { EncounterKind.AttackSpeed, EncounterKind.MaxHealth, EncounterKind.ExtraShot },
            new[] { EncounterKind.AttackSpeed, EncounterKind.AttackDamage, EncounterKind.MaxHealth, EncounterKind.ExtraShot },
            new[] { EncounterKind.AttackSpeed, EncounterKind.AttackDamage, EncounterKind.MaxHealth, EncounterKind.ExtraShot } };
        string[] names = { "Speed-Damage", "Health-Shots", "Three-Hover", "Four-Open", "Four-Hover" };
        for (int n = 0; n < groups.Length; n++)
        {
            ship.transform.position = new Vector3(0, Camera.DownLimit + 1, 0);
            if (n == 4) ship.transform.position = new Vector3(-2.6f, -6, 0);
            var hex = RewardHexes.Open(groups[n], ship.transform, 123, _ => { });
            var canvas = hex.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1;
            foreach (var t in hex.GetComponentsInChildren<Transform>()) t.gameObject.layer = 31;
            Set(hex, "age", 1f);
            Canvas.ForceUpdateCanvases();
            Call(hex, "LateUpdate");
            if (n == 2 || n == 4)
            {
                var zone = ((Array)typeof(RewardHexes).GetField("zones", Hidden).GetValue(hex)).GetValue(1);
                var zoneType = zone.GetType();
                ((CanvasGroup)zoneType.GetField("body").GetValue(zone)).alpha = .1f;
                var charge = (HexagonGraphic)zoneType.GetField("charge").GetValue(zone);
                charge.progress = .65f;
                charge.SetVerticesDirty();
            }
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(960, 720, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 960, 720), 0, 0);
            image.Apply();
            File.WriteAllBytes(Folder + "/" + names[n] + ".png", image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(hex.gameObject);
        }
        RenderTexture.active = null;
        target.Release();
    }
}
#endif
