#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>Temporary editor play test. Added to an unsaved scene by the verifier.</summary>
public sealed class IndustrialRuntimeProbe : MonoBehaviour
{
    IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(1);
        Time.timeScale = 1;
        var player = GameObject.FindGameObjectWithTag("Player").GetComponent<Shipcontroller>();
        player.health = 3;
        player.mana = 47;
        int score = Score.CurrentScore;
        foreach (string kind in new[] { "life", "mana", "money" })
            Instantiate(Resources.Load<GameObject>("Pickables/" + kind), player.transform.position, Quaternion.identity);
        var item = Instantiate(Resources.Load<GameObject>("Items/Item"), new Vector3(2, 3, 1), Quaternion.identity);
        item.AddComponent<Magnet>();
        var active = Instantiate(Resources.Load<GameObject>("Items/Item"), new Vector3(-2, 3, 1), Quaternion.identity);
        var blank = active.AddComponent<BlankBullet>();
        var enemy = Instantiate(Resources.Load<GameObject>("Enemy/enemy1"), new Vector3(-2, 5, 1), Quaternion.identity);
        yield return null;
        var itemVisual = item.GetComponent<PSXObjectVisual>();
        Quaternion rotation = itemVisual.model.localRotation;
        blank.OnPickup();
        yield return new WaitForSecondsRealtime(.6f);
        bool healing = player.health > 3, charging = player.mana > 47, scoring = Score.CurrentScore > score;
        bool spinning = Quaternion.Angle(rotation, itemVisual.model.localRotation) > 1;
        bool itemMapped = itemVisual.model.GetComponent<MeshFilter>().sharedMesh.name.Contains("Magnet");
        bool activeHidden = !active.GetComponent<PSXObjectVisual>().model.gameObject.activeSelf;
        var bulletPrefab = Resources.Load<GameObject>("Bullets/PlayerBullet");
        bool spriteBullets = bulletPrefab.GetComponent<SpriteRenderer>() != null && bulletPrefab.GetComponent<MeshRenderer>() == null && bulletPrefab.GetComponent<PSXProjectileEffects>() == null;
        bool enemyMesh = enemy != null && enemy.GetComponent<PSXObjectVisual>() != null;
        bool hud = FindAnyObjectByType<IndustrialHUD>() != null;
        yield return new WaitForEndOfFrame();
        var image = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes("ArtSource/PSXPreview/L1_Playtest.png", image.EncodeToPNG());
        Destroy(image);
        string report = "{\"scene\":\"L1\",\"healingPickup\":" + B(healing) + ",\"reactorPickup\":" + B(charging) + ",\"scorePickup\":" + B(scoring) + ",\"itemRotates\":" + B(spinning) + ",\"itemTypeMapped\":" + B(itemMapped) + ",\"activeItemHides\":" + B(activeHidden) + ",\"spriteBullets\":" + B(spriteBullets) + ",\"enemyMesh\":" + B(enemyMesh) + ",\"liveHUD\":" + B(hud) + "}";
        File.WriteAllText("ArtSource/PSXPreview/GameplayAudit.json", report);
        Debug.Log("Industrial gameplay audit: " + report);
    }

    static string B(bool value) => value ? "true" : "false";
}
#endif
