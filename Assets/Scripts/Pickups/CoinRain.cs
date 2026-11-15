using UnityEngine;

// SALVAGE pays out as a shower of coins falling across the arena for a few seconds, so the
// pilot sweeps through it instead of banking an instant sum. Later districts rain more.
public sealed class CoinRain : MonoBehaviour
{
    const float Seconds = 4.5f;
    int remaining, total;
    float elapsed;
    GameObject prefab;

    public static int Coins(int stage, float luck) => 16 + 7 * Mathf.Max(0, stage - 1) + Mathf.FloorToInt(Mathf.Clamp(luck, 0, 2) * 12);

    public static void Begin(int coins)
    {
        var rain = new GameObject("Salvage coin rain").AddComponent<CoinRain>();
        rain.total = rain.remaining = Mathf.Max(1, coins);
        rain.prefab = Resources.Load<GameObject>("Pickables/money");
    }

    void Update()
    {
        if (!GameClock.ActivePlay)
            return;
        elapsed += Time.deltaTime;
        // Release evenly over the shower, a little heavier in its middle.
        float progress = Mathf.Clamp01(elapsed / Seconds);
        float shaped = progress * progress * (3 - 2 * progress);
        int due = Mathf.CeilToInt(total * shaped) - (total - remaining);
        for (int i = 0; i < due && remaining > 0; i++, remaining--)
        {
            float x = Random.Range(Camera.LeftLimit + .35f, Camera.RightLimit - .35f);
            // Born well above the visible top edge, so no coin is ever seen appearing.
            var view = UnityEngine.Camera.main;
            float top = view != null ? view.transform.position.y + view.orthographicSize : Camera.UpLimit + 1;
            var coin = Instantiate(prefab, new Vector3(x, Mathf.Max(top, Camera.UpLimit) + Random.Range(1.2f, 2.6f), 1), Quaternion.identity).GetComponent<APickable>();
            coin.Rain(Random.Range(2.6f, 4.2f));
        }
        if (remaining <= 0)
            Destroy(gameObject);
    }
}
