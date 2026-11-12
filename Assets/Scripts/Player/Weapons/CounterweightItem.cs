using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class CounterweightItem : AWeaponItem
{
    void Awake()
    {
        weaponName = nameof(Counterweight);
        SetData("Counterweight", "Weapon", "Fires against your last steering direction. Focus locks aim.", ItemType.weapon);
    }
    public override void OnBegin() { }
}

public sealed class Counterweight : ExpansionWeapon
{
    Vector2 aim = Vector2.up;
    protected override PlayerDelivery Delivery => PlayerDelivery.Needle;
    protected override Vector2 FireDirection => aim;
    protected override float Power => 2.6f;
    protected override int Pellets => 2;
    protected override float PelletSpacing => 7;
    protected override Vector3 MuzzleOffset(Vector3 offset, Vector2 direction) => Quaternion.FromToRotation(Vector3.up, direction) * offset;

    protected override void OnWeaponFrame()
    {
        var input = GameInput.Snapshot;
        if (!input.Focus && input.Move.sqrMagnitude > .04f)
            aim = -input.Move.normalized;
    }
}
