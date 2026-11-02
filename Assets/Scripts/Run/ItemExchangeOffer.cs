using UnityEngine;

// A selectable model of a pickup already owned by the player; never grants a copy.
public sealed class ItemExchangeOffer : AItem
{
    public RunInventory.Entry Entry { get; private set; }
    public override bool RecordsPickup => false;
    public void Initialize(RunInventory.Entry entry, bool healing)
    {
        Entry = entry;
        SetData(entry.name, "", healing ? "Trade this item to repair three hits." : "Reforge this item into one of two alternatives of the same rarity.", ItemType.passive);
        SetRarity(entry.rarity);
        var visual = GetComponent<PSXObjectVisual>();
        if (visual != null)
            visual.itemKindOverride = entry.key;
    }
    public override void OnBegin() { }
}
