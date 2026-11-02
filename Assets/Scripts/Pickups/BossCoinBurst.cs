using UnityEngine;

public static class BossCoinBurst
{
    public static void Spawn(Vector3 origin, int count)
    {
        var prefab = Resources.Load<GameObject>("Pickables/money");
        origin.x = Mathf.Clamp(origin.x, Camera.LeftLimit + .5f, Camera.RightLimit - .5f);
        origin.y = Mathf.Clamp(origin.y, Camera.DownLimit + .5f, Camera.UpLimit - .5f);
        float phase = Random.Range(0, Mathf.PI * 2);
        for (int i = 0; i < count; i++)
        {
            // Golden-angle directions and varied speeds fill a broad disk instead
            // of piling up along one ring or sending the whole payout upward.
            float fraction = (i + .5f) / count;
            float angle = phase + i * 2.39996323f;
            var outward = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            var coin = Object.Instantiate(prefab, origin + (Vector3)outward * (.12f + fraction * .25f), Quaternion.identity).GetComponent<Money>();
            coin.Launch(outward * Mathf.Lerp(4.5f, 12, Mathf.Sqrt(fraction)) + Vector2.down * .7f,
                .9f + fraction * .3f);
        }
    }
}
