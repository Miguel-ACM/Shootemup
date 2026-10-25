[UnityEngine.Scripting.Preserve]
public sealed class StaticSweater : AItem
{
    void Awake(){SetData("Static sweater","Keep moving","Movement charges a chain-lightning hit.",ItemType.passive);}
    public override void OnBegin(){}
    public override void OnPickup(){if(s.GetComponent<StaticCharge>()==null)s.gameObject.AddComponent<StaticCharge>();base.OnPickup();}
}
