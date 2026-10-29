using UnityEngine;

// A nod to the Estus Flask: two sips per district, refilled whenever a new level begins.
[UnityEngine.Scripting.Preserve]
public sealed class FillableFlask : AActiveItem
{
    public const int Charges = 2;
    const int HitsPerSip = 2;
    static readonly Color Ember = new Color(1f, .58f, .16f);
    int charges = Charges;
    int filledAtStage = -1, filledAtLoop = -1;

    public int Remaining => charges;

    void Awake()
    {
        SetData("Fillable Flask", "Rest at a bonfire", "Two heals. Refills every level.", ItemType.active);
        cost = 0;
        cd = 1;
    }

    protected override void Update()
    {
        base.Update();
        if (LevelRun.Stage == filledAtStage && LevelRun.Loop == filledAtLoop)
            return;
        filledAtStage = LevelRun.Stage;
        filledAtLoop = LevelRun.Loop;
        charges = Charges;
    }

    public override float OnPress(float mana)
    {
        float valid = CheckValidPress(mana);
        if (valid < 0)
            return valid;
        if (charges <= 0 || s.health <= 0 || s.health >= s.maxHealth)
            return -1;
        charges--;
        s.Repair(HitsPerSip);
        lastCD = 0;
        PSXFireballs.Shockwave(s.transform.position, 1.1f, .5f, Ember, 2, .08f);
        PSXImpactEffects.ItemGet(s.transform.position, Ember);
        ItemSound.Play("heal", .22f, .85f);
        return mana;
    }
}
