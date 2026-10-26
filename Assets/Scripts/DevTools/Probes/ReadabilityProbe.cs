#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>Unsaved visual QA, followed by a fresh launch of normal L1.</summary>
public sealed class ReadabilityProbe : MonoBehaviour
{
    IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(.6f);
        Time.timeScale = 1;
        foreach (var orchestrer in FindObjectsByType<WaveOrchestrer>())
        {
            orchestrer.StopAllCoroutines();
            orchestrer.CancelInvoke();
            orchestrer.enabled = false;
        }
        foreach (var item in FindObjectsByType<AItem>())
            Destroy(item.gameObject);
        var player = GameObject.FindGameObjectWithTag("Player").GetComponent<Shipcontroller>();
        player.enabled = false;
        player.transform.position = new Vector3(0, -3.4f, 20);
        var boss = Instantiate(Resources.Load<GameObject>("Enemy/Bosses/stage1boss"), new Vector3(0, 4.6f, 1), Quaternion.identity);
        foreach (var script in boss.GetComponentsInChildren<MonoBehaviour>())
            if (!(script is PSXObjectVisual) && !(script is PSXVisibilityRelay))
                script.enabled = false;
        foreach (var body in boss.GetComponentsInChildren<Rigidbody2D>())
            body.simulated = false;
        foreach (var collider in boss.GetComponentsInChildren<Collider2D>())
            collider.enabled = false;
        string[] pickups = { "life", "mana", "money" };
        for (int i = 0; i < 3; i++)
        {
            var go = Instantiate(Resources.Load<GameObject>("Pickables/" + pickups[i]), new Vector3(-2 + i * 2, -.7f, 1), Quaternion.identity);
            go.GetComponent<APickable>().enabled = false;
            go.GetComponent<Rigidbody2D>().simulated = false;
            go.transform.localScale = Vector3.one * 1.5f;
        }
        MakeItem<Caffeine>(new Vector3(2, 1.3f, 1));
        MakeItem<AMoonItem>(new Vector3(-2, 1.3f, 1));
        var dialog = FindAnyObjectByType<Dialog>();
        var warning = FindAnyObjectByType<Warning>();
        yield return new WaitForSecondsRealtime(.3f);
        dialog.ShowItemDesc("Caffeine", "Everything feels faster now!", "Increases firing speed, bullet speed and movement speed.", "rare");
        yield return new WaitForSecondsRealtime(.3f);
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
        float center = RectTransformUtility.WorldToScreenPoint(null, dialog.Panel.position).x;
        bool centered = Mathf.Abs(center - Screen.width * .5f) < 2;
        float bossWidth = boss.GetComponent<PSXObjectVisual>().modelRenderer.bounds.size.x;
        Capture("ReadableItems_Dialog.png");
        dialog.Hide();
        yield return new WaitForSecondsRealtime(.3f);
        bool hidden = !dialog.IsVisible;
        warning.blink();
        yield return new WaitForSecondsRealtime(1.1f);
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
        var corners = new Vector3[4];
        warning.ScreenRect.GetWorldCorners(corners);
        var lo = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
        var hi = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
        bool full = Mathf.Abs((hi.x - lo.x) - Screen.height * 405f / 720) < 2 && Mathf.Abs((hi.x + lo.x) * .5f - Screen.width * .5f) < 2 && Mathf.Abs(lo.y) < 2 && Mathf.Abs(hi.y - Screen.height) < 2 && lo.x > 1;
        int centeredWarnings = 0;
        foreach (Transform child in warning.ScreenRect)
        {
            var label = child.Find("Text");
            if (label == null)
                continue;
            var text = label.GetComponent<TMPro.TextMeshProUGUI>();
            if (text != null && text.text == "WARNING" && Mathf.Abs(RectTransformUtility.WorldToScreenPoint(null, text.rectTransform.position).x - Screen.width * .5f) < 2)
                centeredWarnings++;
        }
        bool warningWordsCentered = centeredWarnings == 7;
        Capture("Playfield_Warning.png");
        for (int frame = 0; frame < 12; frame++)
        {
            yield return new WaitForSecondsRealtime(.05f);
            yield return new WaitForEndOfFrame();
            Capture("WarningMotion_" + frame.ToString("00") + ".png");
        }
        yield return new WaitForSecondsRealtime(2.6f);
        bool warningClears = !warning.IsVisible;
        foreach (var item in FindObjectsByType<AItem>())
            Destroy(item.gameObject);
        yield return null;
        var selected = MakeItem<Caffeine>(player.transform.position + new Vector3(1.8f, 1, 0));
        selected.rarity = "legendary";
        var other = MakeItem<AMoonItem>(player.transform.position + new Vector3(-2.8f, 1, 0));
        var waves = FindAnyObjectByType<WaveOrchestrer>();
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        typeof(WaveOrchestrer).GetField("items", flags).SetValue(waves, new System.Collections.Generic.List<GameObject> { selected.gameObject, other.gameObject });
        selected.Register(waves);
        other.Register(waves);
        bool rejectsFarther = !waves.TryClaimItem(other), claimsNearest = waves.TryClaimItem(selected);
        int bursts = PSXImpactEffects.BurstCount;
        float firing = GameRules.playerShootSpeed;
        yield return new WaitForSecondsRealtime(.15f);
        bool flies = selected != null && selected.IsAttracting;
        bool orbit = selected != null && selected.GetComponentInChildren<ParticleSystemRenderer>() != null;
        yield return new WaitForSecondsRealtime(.7f);
        bool collectedOnce = selected == null && other == null && Mathf.Approximately(GameRules.playerShootSpeed, firing + 5) && PSXImpactEffects.BurstCount > bursts;
        waves.CancelInvoke();
        waves.StopAllCoroutines();
        bursts = PSXImpactEffects.BurstCount;
        player.SendMessage("GetDamaged", 1f);
        bool playerImpact = PSXImpactEffects.BurstCount == bursts + 1;
        var enemy = Instantiate(Resources.Load<GameObject>("Enemy/enemy1"), new Vector3(0, 1, 1), Quaternion.identity);
        yield return null;
        enemy.GetComponent<AEnemy>().health = 1000;
        bursts = PSXImpactEffects.BurstCount;
        enemy.GetComponent<AEnemy>().damage(.1f);
        bool enemyImpact = PSXImpactEffects.BurstCount == bursts + 1;
        yield return new WaitForEndOfFrame();
        Capture("PolishedEffects.png");
        string report = "{\"dialogCentered\":" + B(centered) + ",\"dialogFullyHides\":" + B(hidden) + ",\"warningClippedToPlayfield\":" + B(full) + ",\"warningWordsCentered\":" + B(warningWordsCentered) + ",\"warningClears\":" + B(warningClears) + ",\"bossWorldWidth\":" + bossWidth.ToString(System.Globalization.CultureInfo.InvariantCulture) + ",\"bossScaleRestored\":" + B(bossWidth > 5) + ",\"claimsNearestOnly\":" + B(rejectsFarther && claimsNearest) + ",\"itemFliesToPlayer\":" + B(flies) + ",\"rarityOrbit\":" + B(orbit) + ",\"collectsOnceAndRemovesOtherOptions\":" + B(collectedOnce) + ",\"playerImpact\":" + B(playerImpact) + ",\"enemyImpact\":" + B(enemyImpact) + "}";
        File.WriteAllText("ArtSource/PSXPreview/ReadabilityAudit.json", report);
        Debug.Log("Readability audit: " + report);
        File.WriteAllText("ArtSource/PSXPreview/open-gameplay.request", "Return to normal L1 after visual QA");
        UnityEditor.EditorApplication.isPlaying = false;
    }

    static T MakeItem<T>(Vector3 position) where T : AItem
    {
        var item = Instantiate(Resources.Load<GameObject>("Items/Item"), position, Quaternion.identity);
        foreach (var collider in item.GetComponents<Collider2D>())
            collider.enabled = false;
        return item.AddComponent<T>();
    }

    static void Capture(string name)
    {
        var image = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes("ArtSource/PSXPreview/" + name, image.EncodeToPNG());
        Destroy(image);
    }

    static string B(bool value) => value ? "true" : "false";
}
#endif
