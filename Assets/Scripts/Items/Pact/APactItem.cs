using UnityEngine;

// A pact shows its boon in gold and its curse in red on the item card.
public abstract class APactItem : AItem
{
    const string BoonColor = "#F2C35A", CurseColor = "#FF5A5A";

    protected void SetPact(string name, string boon, string curse)
    {
        SetData(name, "<color=" + BoonColor + ">" + boon + "</color>", "<color=" + CurseColor + ">" + curse + "</color>", ItemType.passive);
        SetRarity(PactPool.Rarity);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        Seal();
        // A dark red seal: the bargain is binding.
        PSXFireballs.Shockwave(s.transform.position, 2.2f, .55f, AItem.GetRarityColor(PactPool.Rarity), 2, .5f);
        ItemSound.Play("shatter", .2f, .6f);
        base.OnPickup();
    }

    protected abstract void Seal();
}
