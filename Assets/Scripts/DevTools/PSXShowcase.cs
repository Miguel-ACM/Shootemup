using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
#if UNITY_EDITOR
using System.IO;
#endif

/// <summary>Optional visual showcase: F8 hands the real player back to the user.</summary>
[DefaultExecutionOrder(-20)]
public sealed class PSXShowcase : MonoBehaviour
{
    public Font hudFont;
    public bool autopilot = true;
    public bool capturePreview = true;
    Shipcontroller player;
    BaseWeapon weapon;
    float shotTime;
    float elapsed;
    public bool showFleet = true;

    IEnumerator Start()
    {
        Time.timeScale = 1f;
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<Shipcontroller>();
        weapon = player.GetComponent<BaseWeapon>();
        // Let the real controller initialise its damage flash and movement limits.
        yield return null;
        player.enabled = !autopilot;
        player.transform.position = new Vector3(0, -3.5f, 20);
        Debug.Log("Industrial showcase ready: sprite bullets, live HUD, native screen rendering.");
        if (showFleet)
            StartCoroutine(Fleet());
        if (capturePreview)
            StartCoroutine(Capture());
    }

    IEnumerator Fleet()
    {
        yield return null;
        string[] paths = { "Enemy/enemy1", "Enemy/waver", "Enemy/Biggie" };
        int wave = 0;
        while (true)
        {
            var enemy = Instantiate(Resources.Load<GameObject>(paths[wave % 3]), new Vector3(wave % 2 == 0 ? -2.2f : 2.2f, 6.5f, 1), Quaternion.identity);
            foreach (var mover in enemy.GetComponents<AMover>())
                mover.enabled = false;
            var body = enemy.GetComponent<Rigidbody2D>();
            if (body != null)
                body.linearVelocity = Vector2.down * .65f;
            foreach (var shooter in enemy.GetComponents<AShooter>())
            {
                shooter.activation = AShooter.ShootTrigger.onTrigger;
                shooter.Trigger();
            }
            Destroy(enemy, 12);
            if (wave % 2 == 0)
            {
                var prefab = Resources.Load<GameObject>("Pickables/" + new[] { "life", "mana", "money" }[wave / 2 % 3]);
                var pickup = Instantiate(prefab, new Vector3(-3.2f, 2.5f, 1), Quaternion.identity);
                pickup.GetComponent<APickable>().enabled = false;
                var rb = pickup.GetComponent<Rigidbody2D>();
                if (rb != null)
                    rb.linearVelocity = Vector2.down * .25f;
                Destroy(pickup, 14);
            }
            if (wave % 3 == 0)
            {
                var upgrade = Instantiate(Resources.Load<GameObject>("Items/Item"), new Vector3(3.25f, 1.5f, 1), Quaternion.identity);
                if (wave % 2 == 0)
                    upgrade.AddComponent<Magnet>();
                else
                    upgrade.AddComponent<ElectricityItem>();
                Destroy(upgrade, 12);
            }
            wave++;
            yield return new WaitForSeconds(3.5f);
        }
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        if (Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame)
        {
            autopilot = !autopilot;
            player.enabled = !autopilot;
            weapon.isStopingPrimary = !autopilot;
            weapon.isShootingPrimary = autopilot;
        }
        if (autopilot)
        {
            player.health = player.maxHealth;
            player.transform.position = new Vector3(Mathf.Sin(elapsed * .60f) * 2.25f, -3.5f + Mathf.Sin(elapsed * .91f) * .55f, 20);
            if (Time.time >= shotTime)
            {
                shotTime = Time.time + .13f;
                weapon.isStopingPrimary = false;
                weapon.isShootingPrimary = true;
                weapon.Shoot();
            }
        }
    }

    IEnumerator Capture()
    {
#if UNITY_EDITOR
        yield return new WaitForSecondsRealtime(9);
        const string folder = "ArtSource/PSXPreview";
        Directory.CreateDirectory(folder + "/frames");
        for (int i = 0; i < 30; i++)
        {
            yield return new WaitForEndOfFrame();
            var image = ScreenCapture.CaptureScreenshotAsTexture();
            if (image != null)
            {
                byte[] data = image.EncodeToPNG();
                File.WriteAllBytes(folder + "/frames/frame_" + i.ToString("000") + ".png", data);
                if (i == 0)
                    File.WriteAllBytes(folder + "/PSX_Showcase.png", data);
                Destroy(image);
            }
            yield return new WaitForSecondsRealtime(1f / 12f);
        }
        var presentation = UnityEngine.Camera.main.GetComponent<PSXPresentation>();
        int count = FindObjectsByType<Bullet>().Length;
        var report = "{\"scene\":\"PSX_Showcase\",\"shipTriangles\":236,\"bulletTriangles\":2,\"activeProjectiles\":" + count + ",\"thrusters\":2,\"virtualHeight\":" + presentation.SceneTexture.height + ",\"filterEnabled\":" + presentation.filterEnabled.ToString().ToLowerInvariant() + ",\"capturedFrames\":30}";
        File.WriteAllText(folder + "/PreviewReport.json", report);
        Debug.Log("PSX showcase capture complete: " + report);
#else
        yield break;
#endif
    }
}
