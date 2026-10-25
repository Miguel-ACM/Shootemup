public class AngryMoonItem : ACompanionItem
{
    private void Awake()
    {
        companionName = "AngryMoon";
        SetData("Angry Moon", "This time it's personal", "Orbiting shield that shoots.", AItem.ItemType.partner);
    }

    public override void OnBegin()
    {
    }
}
