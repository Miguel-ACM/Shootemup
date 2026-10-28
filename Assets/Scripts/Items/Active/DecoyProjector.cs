using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class DecoyProjector : AActiveItem
{
    void Awake()
    {
        SetData("Decoy projector", "Look over there", "Launch a spectral copy that draws aimed fire.", ItemType.active);
        cost = 25;
        cd = 3;
    }

    public override float OnPress(float mana)
    {
        float valid = CheckValidPress(mana);
        if (valid < 0)
            return valid;
        SpectralDecoy.Launch(s);
        lastCD = 0;
        return mana - cost;
    }
}
