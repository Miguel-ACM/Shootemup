using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class Magpie : AItem
{
    void Awake() => SetData("Magpie", "Bright things", "Fetches coins from the edges of the arena.", ItemType.partner);
    public override void OnBegin() { }

    public override void OnPickup()
    {
        RoamingCompanion.Attach<MagpieFlight>(s, nameof(Magpie), .44f, new Vector2(-.69f, .18f));
        base.OnPickup();
    }
}

public sealed class MagpieFlight : RoamingCompanion
{
    float searchIn;
    Money target;

    protected override void Step(float dt)
    {
        searchIn -= dt;
        if (!Available(target))
        {
            target = null;
            if (searchIn <= 0)
            {
                target = FindCoin();
                searchIn = .24f;
            }
        }
        if (target == null)
        {
            Fly(Home, 9, dt);
            return;
        }

        var point = (Vector2)target.transform.position;
        Fly(point, 12, dt);
        if ((point - Position).sqrMagnitude > .22f * .22f) return;
        target.Collect();
        target = null;
        searchIn = .42f;
        RunInventory.Ping(this);
    }

    protected override void Park() => target = null;

    Money FindCoin()
    {
        Money result = null;
        float nearest = float.PositiveInfinity;
        foreach (var coin in Money.Live)
        {
            if (!Available(coin)) continue;
            Vector2 position = coin.transform.position;
            bool edge = position.x < Camera.LeftLimit + 1.35f || position.x > Camera.RightLimit - 1.35f ||
                position.y < Camera.DownLimit + 1.20f;
            if (!edge) continue;
            float distance = (position - Position).sqrMagnitude;
            if (distance >= nearest || CombatWalls.Sweep(Position, position, .10f, out _, out _)) continue;
            nearest = distance;
            result = coin;
        }
        return result;
    }

    static bool Available(Money coin)
    {
        if (coin == null || coin.IsCollected || coin.IsScattering) return false;
        var position = coin.transform.position;
        return position.x >= Camera.LeftLimit - .3f && position.x <= Camera.RightLimit + .3f &&
            position.y >= Camera.DownLimit - .35f && position.y <= Camera.UpLimit;
    }
}
