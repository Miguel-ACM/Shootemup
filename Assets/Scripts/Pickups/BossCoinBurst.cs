using UnityEngine;

public static class BossCoinBurst
{
    public static void Spawn(Vector3 origin, int count)
    {
        var prefab = Resources.Load<GameObject>("Pickables/money");
        origin.x = Mathf.Clamp(origin.x, Camera.LeftLimit + .5f, Camera.RightLimit - .5f);
        origin.y = Mathf.Clamp(origin.y, Camera.DownLimit + .5f, Camera.UpLimit - .5f);
        float phase = Random.Range(0, Mathf.PI * 2), gust = Random.Range(0, Mathf.PI * 2);
        for (int i = 0; i < count; i++)
        {
            // Three ragged sprays, with stray coins landing between them.
            float angle = phase + i * 2.39996323f + Random.Range(-.42f, .42f);
            angle += .24f * Mathf.Sin(angle * 3 + gust);
            var outward = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            var coin = Object.Instantiate(prefab, origin + (Vector3)(Random.insideUnitCircle * .32f), Quaternion.identity).GetComponent<Money>();
            float speed = Random.Range(4.2f, 11.5f) + 1.6f * Mathf.Cos(angle * 3 + gust);
            coin.Launch(outward * speed + Vector2.down * .7f, Random.Range(.9f, 1.32f));
        }
    }
}
