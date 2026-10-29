// The imp is fed two hits up front, then hunts anything that strays close.
[UnityEngine.Scripting.Preserve]
public sealed class DevilImp : APactItem
{
    void Awake()
    {
        SetPact("Devil Imp", "An imp orbits you, shooting nearby enemies.", "Lose 2 hits now.");
    }

    protected override void Seal()
    {
        s.Sacrifice(2);
        ImpCompanion.Attach(s);
    }
}
