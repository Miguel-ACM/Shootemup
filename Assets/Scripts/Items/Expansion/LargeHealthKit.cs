[UnityEngine.Scripting.Preserve]
public sealed class LargeHealthKit : AItem
{
    void Awake(){SetData("Large Health Kit","Emergency repair","Restore 45 hull. Offered by shops when your health is low.",ItemType.passive);}
    public override void OnBegin(){}
    public override void OnPickup(){s.HealFlat(45);base.OnPickup();}
}
