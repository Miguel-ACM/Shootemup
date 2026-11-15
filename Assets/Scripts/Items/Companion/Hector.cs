// Hector, a pet rock and a nod to OMORI. He hops after the ship and takes a round for it,
// then lies upside down as toast until he finds his feet again.
[UnityEngine.Scripting.Preserve]
public sealed class Hector : AItem
{
    void Awake()
    {
        SetData("Hector", "A very good rock", "Takes a bullet for you, then needs a rest.", ItemType.partner);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        HectorPet.Attach(s);
        base.OnPickup();
    }
}
