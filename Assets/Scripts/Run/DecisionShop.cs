using UnityEngine;
using TMPro;
using System;
using System.Collections.Generic;

public sealed class DecisionShop : MonoBehaviour
{
    public bool paid;
    public bool coinShop;
    Shipcontroller player;
    WaveOrchestrer owner;
    Action done;
    readonly List<AItem> items = new List<AItem>();
    readonly Dictionary<AItem, int> prices = new Dictionary<AItem, int>();
    AItem claimed;
    DecisionOverlay overlay;
    public float SkipProgress => skipCharge / RouteCharge.HoldSeconds;
    Dialog dialog;
    int preview = -1;
    float age, skipCharge;
    bool closed;
    bool collectBoth;
    RunSlotKind exchangeKind;
    RunInventory inventory;
    RunInventory.Entry reforgeSource;
    List<RunInventory.Entry> tradeEntries;
    int tradePage;
    float pageCharge;
    bool pageLatch;
    const int TradesPerPage = 3;
    public static int RewardCount(float luck, float roll)
    {
        float chance = Mathf.Clamp(.08f + Mathf.Max(0, luck) * .35f, .08f, .4f);
        return roll < chance * .15f ? 4 : roll < chance ? 3 : 2;
    }

    public void Open(WaveOrchestrer orchestrer, Shipcontroller ship, string kind, Action completed, Vector3? dropPosition = null)
        => Open(orchestrer, ship, RunSlot.Parse(kind).Kind, completed, dropPosition);

    public void Open(WaveOrchestrer orchestrer, Shipcontroller ship, RunSlotKind kind, Action completed, Vector3? dropPosition = null,
        string reservedKey = null, string reservedRarity = null, string reason = null, bool doubleBossReward = false)
    {
        collectBoth = kind == RunSlotKind.BossReward && doubleBossReward;
        bool earnedReward = kind == RunSlotKind.BossReward || kind == RunSlotKind.ChallengeReward || kind == RunSlotKind.CargoReward;
        bool pact = kind == RunSlotKind.Pact;
        owner = orchestrer;
        player = ship;
        exchangeKind = kind;
        bool exchange = kind == RunSlotKind.HealthTrade || kind == RunSlotKind.Reforge;
        paid = kind != RunSlotKind.Reward && !earnedReward && !pact && !exchange;
        coinShop = kind == RunSlotKind.CoinShop;
        done = completed;
        CombatDecision.Begin();
        player.GetComponent<ShipLoadout>().ResetSelection();
        foreach (var weapon in player.GetComponentsInChildren<AWeapon>())
            weapon.StopFiring();
        dialog = GameObject.FindGameObjectWithTag("Dialog").GetComponent<Dialog>();
        overlay = new GameObject("Shop decision overlay", typeof(RectTransform)).AddComponent<DecisionOverlay>();
        overlay.Build(dialog.font);
        // Earned rewards say where they came from.
        reason = reason ?? (kind == RunSlotKind.ChallengeReward ? "CHALLENGE COMPLETE" : kind == RunSlotKind.BossReward ? "BOSS REWARD" : null);
        if (reason != null)
            overlay.SetReason(reason, reservedRarity != null ? AItem.GetRarityColor(reservedRarity) : new Color(.95f, .76f, .42f));
        if (exchange)
        {
            inventory = RunInventory.For(player);
            tradeEntries = inventory.Tradeable();
            if (kind == RunSlotKind.Reforge)
                tradeEntries.RemoveAll(entry => ItemPool.ReforgeAlternatives(entry.key, entry.rarity).Count < 2);
            ShowTradePage(0);
            return;
        }
        // Pacts always offer two bargains when two remain; declining is the bottom skip.
        var pacts = pact ? PactPool.Draw(player, 2) : null;
        int count = pact ? pacts.Count : earnedReward ? (collectBoth ? 2 : 1) : paid ? (UnityEngine.Random.value < .35f ? 4 : 3) : RewardCount(GameRules.luck, UnityEngine.Random.value);
        bool healthKit = paid && player.health < player.maxHealth * .45f && ItemPool.ReserveHealthKit();
        if (!pact && reservedKey == null)
            count = Mathf.Min(count, ItemPool.RemainingCount + (healthKit ? 1 : 0));
        if (count == 0 && !paid && !pact)
            RunCoins.Add(8);
        for (int i = 0; i < count; i++)
        {
            string rarity = reservedRarity ?? (pact ? PactPool.Rarity : "common");
            string itemKind = reservedKey ?? (pact ? pacts[i] : healthKit && i == count - 1 ? "LargeHealthKit" :
                kind == RunSlotKind.CargoReward ? ItemPool.GetCargoReward(out rarity) : ItemPool.GetItem(out rarity));
            if (itemKind == null)
            {
                RunCoins.Add(8);
                continue;
            }
            var target = dropPosition ?? new Vector3(Mathf.Lerp(Camera.LeftLimit + 1, Camera.RightLimit - 1, count == 1 ? .5f : i / (float)(count - 1)), Camera.UpLimit * .5f, 1);
            if (collectBoth && count == 2 && dropPosition.HasValue)
            {
                const float halfSpacing = 1.25f;
                float center = Mathf.Clamp(target.x, Camera.LeftLimit + .8f + halfSpacing, Camera.RightLimit - .8f - halfSpacing);
                target.x = center + (i == 0 ? -halfSpacing : halfSpacing);
            }
            var entrance = dropPosition.HasValue ? target + Vector3.up * .9f : new Vector3(target.x, Camera.UpLimit + 3, 1);
            var go = Instantiate(Resources.Load<GameObject>("Items/Item"), entrance, Quaternion.identity);
            var item = ItemCatalog.Get(itemKind).Attach(go);
            item.SetRarity(rarity);
            item.Register(owner);
            item.GoToPosition(target);
            items.Add(item);
            int tier = rarity == "legendary" ? 3 : rarity == "epic" ? 2 : rarity == "rare" ? 1 : 0;
            int price = !paid ? 0 : coinShop ? new[] { 12, 22, 36, 55 }[tier] : tier == 3 ? 3 : tier == 2 ? 2 : 1;
            if (healthKit && i == count - 1)
                price = coinShop ? 10 : 1;
            prices[item] = price;
            overlay.Add(item);
        }
    }

    void Update()
    {
        if (closed || player == null)
            return;
        age += Time.deltaTime;
        // Nothing left to offer (every pact already seen): close on the next frame, never inside Open.
        if (items.Count == 0)
        {
            Close();
            return;
        }
        if (player.health <= 0)
        {
            Abort();
            return;
        }
        UpdateTradePages();
        int nearest = -1;
        float d = 99;
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == null || items[i].Collected || items[i].IsDismissing)
                continue;
            float x = Vector2.Distance(player.transform.position, items[i].transform.position);
            if (x < d)
            {
                d = x;
                nearest = i;
            }
            bool trade = items[i] is ItemExchangeOffer;
            overlay.SetOffer(i, trade ? (exchangeKind == RunSlotKind.HealthTrade ? "+3" : "2") : paid ? prices[items[i]] + (coinShop ? "" : "x") : "",
                Affordable(prices[items[i]]), trade ? (exchangeKind == RunSlotKind.HealthTrade ? "repair" : "reforge") : null);
        }
        if (d < 1.8f && nearest >= 0 && items[nearest].Settled)
        {
            if (preview != nearest)
            {
                preview = nearest;
                var data = items[nearest].GetData();
                dialog.ShowItemDesc(data[0], data[1], data[2], items[nearest].rarity);
            }
        }
        else
        {
            preview = -1;
            dialog.Hide();
        }
        var camera = UnityEngine.Camera.main;
        var hitbox = player.GetComponent<Collider2D>();
        float bottom = hitbox != null ? hitbox.bounds.min.y : player.transform.position.y;
        float worldPerPixel = camera.orthographicSize * 2 / 720f;
        bool atBottom = bottom <= camera.transform.position.y - camera.orthographicSize + DecisionOverlay.SkipHeight * worldPerPixel &&
            Mathf.Abs(player.transform.position.x - camera.transform.position.x) <= DecisionOverlay.SkipWidth * .5f * worldPerPixel;
        bool settled = items.TrueForAll(item => item == null || item.Collected || item.Settled);
        skipCharge = atBottom && settled && age > .4f && claimed == null ? skipCharge + Time.deltaTime : 0;
        overlay.SetSkip(SkipProgress);
        if (skipCharge >= RouteCharge.HoldSeconds)
            Close();
    }

    bool Affordable(int price) => !paid || (coinShop ? RunCoins.Count >= price : Score.CurrentMultiplier > price);
    bool Pay(int price) => !paid || (coinShop ? RunCoins.TrySpend(price) : Score.TrySpendMultiplier(price));
    public bool CommitItem(AItem item)
    {
        if (closed || claimed != item)
            return false;
        if (reforgeSource != null && !(item is ItemExchangeOffer))
        {
            if (!inventory.Remove(reforgeSource))
                return false;
            reforgeSource = null;
        }
        return true;
    }
    public bool TryClaim(AItem item)
    {
        if (item is ItemExchangeOffer && exchangeKind == RunSlotKind.HealthTrade && player.health >= player.maxHealth)
            return false;
        if (closed || item == null || item.Collected || item.IsDismissing || !items.Contains(item) ||
            Vector2.Distance(item.transform.position, player.transform.position) > AItem.ItemAttractRange)
            return false;
        if (claimed != null)
            return claimed == item;
        AItem nearest = null;
        float d = float.MaxValue;
        foreach (var option in items)
        {
            if (option == null || option.Collected || option.IsDismissing)
                continue;
            float distance = Vector2.SqrMagnitude(option.transform.position - player.transform.position);
            if (distance < d)
            {
                d = distance;
                nearest = option;
            }
        }
        if (nearest != item || !Pay(prices[item]))
            return false;
        claimed = item;
        if (paid)
            SoundManager.PlaySound("uiConfirm", .1f);
        return true;
    }

    public void Picked(GameObject selected)
    {
        if (claimed == null || claimed.gameObject != selected)
            return;
        if (claimed is ItemExchangeOffer trade)
        {
            if (exchangeKind == RunSlotKind.HealthTrade)
            {
                if (inventory.Remove(trade.Entry))
                    HealthRecovery.Begin(player, 3);
                Close(selected);
            }
            else
                ShowReforge(trade.Entry);
            return;
        }
        bool anotherReward = collectBoth && items.Exists(item => item != null && item != claimed && !item.Collected && !item.IsDismissing);
        if (!paid && !anotherReward)
        {
            Close(selected);
            return;
        }
        claimed = null;
        preview = -1;
        dialog.Hide();
        player.GetComponent<ShipLoadout>().ResetSelection();
    }

    void ClearOffers()
    {
        foreach (var item in items)
            if (item != null)
                item.Dismiss();
        items.Clear();
        prices.Clear();
        if (overlay != null)
            overlay.Dismiss();
        overlay = new GameObject("Exchange decision overlay", typeof(RectTransform)).AddComponent<DecisionOverlay>();
        overlay.Build(dialog.font);
        claimed = null;
        preview = -1;
        skipCharge = age = 0;
        dialog.Hide();
        player.GetComponent<ShipLoadout>().ResetSelection();
    }

    void ShowTradePage(int page)
    {
        tradePage = Mathf.Clamp(page, 0, Mathf.Max(0, (tradeEntries.Count - 1) / TradesPerPage));
        ClearOffers();
        int first = tradePage * TradesPerPage, count = Mathf.Min(TradesPerPage, tradeEntries.Count - first);
        for (int i = 0; i < count; i++)
        {
            var entry = tradeEntries[first + i];
            var target = new Vector3(Mathf.Lerp(Camera.LeftLimit + 1.3f, Camera.RightLimit - 1.3f, count == 1 ? .5f : i / (float)(count - 1)), Camera.UpLimit * .4f, 1);
            var go = Instantiate(Resources.Load<GameObject>("Items/Item"), new Vector3(target.x, Camera.UpLimit + 3, 1), Quaternion.identity);
            var token = go.AddComponent<ItemExchangeOffer>();
            token.Initialize(entry, exchangeKind == RunSlotKind.HealthTrade);
            token.Register(owner);
            token.GoToPosition(target);
            items.Add(token);
            prices[token] = 0;
            overlay.Add(token);
        }
        overlay.SetPages(tradePage, Mathf.CeilToInt(tradeEntries.Count / (float)TradesPerPage), 0, 0);
    }

    void UpdateTradePages()
    {
        if (tradeEntries == null || reforgeSource != null || tradeEntries.Count <= TradesPerPage || claimed != null)
            return;
        float y = Mathf.Lerp(Camera.DownLimit, Camera.UpLimit, .35f);
        int side = Vector2.Distance(player.transform.position, new Vector2(Camera.LeftLimit + 1, y)) < .7f ? -1 :
            Vector2.Distance(player.transform.position, new Vector2(Camera.RightLimit - 1, y)) < .7f ? 1 : 0;
        if (side == 0)
            pageLatch = false;
        bool valid = side < 0 ? tradePage > 0 : side > 0 && (tradePage + 1) * TradesPerPage < tradeEntries.Count;
        pageCharge = valid && !pageLatch ? pageCharge + Time.deltaTime : 0;
        if (pageCharge >= RouteCharge.HoldSeconds)
        {
            pageLatch = true;
            pageCharge = 0;
            ShowTradePage(tradePage + side);
            ItemSound.Play("ready", .12f);
        }
        overlay.SetPages(tradePage, Mathf.CeilToInt(tradeEntries.Count / (float)TradesPerPage), pageCharge / RouteCharge.HoldSeconds, side);
    }

    void ShowReforge(RunInventory.Entry source)
    {
        reforgeSource = source;
        ClearOffers();
        var choices = ItemPool.DrawReforge(source.key, source.rarity, 2);
        for (int i = 0; i < choices.Count; i++)
        {
            var target = new Vector3(i == 0 ? -2 : 2, Camera.UpLimit * .4f, 1);
            var go = Instantiate(Resources.Load<GameObject>("Items/Item"), new Vector3(target.x, Camera.UpLimit + 3, 1), Quaternion.identity);
            var item = ItemCatalog.Get(choices[i]).Attach(go);
            item.SetRarity(source.rarity);
            item.Register(owner);
            item.GoToPosition(target);
            items.Add(item);
            prices[item] = 0;
            overlay.Add(item);
        }
    }

    public void Abort()
    {
        done = null;
        Close();
    }

    void Close(GameObject selected = null)
    {
        if (closed)
            return;
        closed = true;
        foreach (var item in items)
            if (item != null && item.gameObject != selected)
                item.Dismiss();
        if (overlay != null)
            overlay.Dismiss();
        if (dialog != null)
            dialog.Hide();
        CombatDecision.End();
        done?.Invoke();
        Destroy(this);
    }

    void OnDestroy()
    {
        if (!closed)
        {
            CombatDecision.End();
            foreach (var item in items)
                if (item != null)
                    Destroy(item.gameObject);
            if (overlay != null)
                Destroy(overlay.gameObject);
        }
    }
}
