using UnityEngine;

// Piggy Bank: savings earn interest. Each time a new district opens, the pig pays one coin
// for every six in the wallet (at most fifteen per copy), thrown out of the ship as real coins.
[UnityEngine.Scripting.Preserve]
public sealed class PiggyBank : AItem
{
    public const int Rate = 6, Cap = 15;

    void Awake()
    {
        SetData("Piggy Bank", "Oink", "Each new district: +1 coin for every 6 you hold.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.PiggyBank);
        base.OnPickup();
    }

    public static int Interest(int coins, int copies) => copies <= 0 ? 0 : Mathf.Min(Cap, coins / Rate) * copies;

    public static void Pay(Shipcontroller ship, int copies)
    {
        int interest = Interest(RunCoins.Count, copies);
        if (interest <= 0)
            return;
        RunInventory.Ping(UpgradeKind.PiggyBank);
        // Thrown up and out of the ship as ordinary coins that come straight back to it.
        BossCoinBurst.Spawn(ship.transform.position + Vector3.up * .4f, interest, .42f);
        ItemProcVfx.Emit(ItemProcShape.Convert, ship.transform.position, new Color(.97f, .6f, .69f), .55f);
        ItemSound.Cue("itemSalvage", volume: .1f, pitch: 1.15f);
    }
}
