public class ProteinSupplement : AItem
{

    private void Awake()
    {
        SetData("Protein Supplement", "Faster gains", "Increased bullet size and damage.", AItem.ItemType.passive);
    }

    public override void OnBegin()
    {

    }

    public override void OnPickup()
    {
        GameRules.playerShootSize += 1;
        GameRules.playerShootDamage += 1;
        base.OnPickup();
    }


}
