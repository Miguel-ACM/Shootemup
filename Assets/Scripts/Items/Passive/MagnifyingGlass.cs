[UnityEngine.Scripting.Preserve]
public sealed class MagnifyingGlass : AItem
{
    void Awake()
    {
        SetData("Magnifying glass", "Look closer", "Hold focus for larger, stronger, slower shots.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        if (s.GetComponent<MagnifyingLens>() == null)
            s.gameObject.AddComponent<MagnifyingLens>();
        base.OnPickup();
    }
}
