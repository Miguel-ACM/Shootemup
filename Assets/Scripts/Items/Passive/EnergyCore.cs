using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class EnergyCore : AItem
{
    void Awake()
    {
        SetData("Energy Regen", "Self sustaining", "Regenerate 3 energy per second in combat.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        if (s.GetComponent<PassiveEnergyCore>() == null)
            s.gameObject.AddComponent<PassiveEnergyCore>();
        base.OnPickup();
    }
}
public sealed class PassiveEnergyCore : MonoBehaviour
{
    Shipcontroller ship;
    void Awake()
    {
        ship = GetComponent<Shipcontroller>();
    }

    void Update()
    {
        if (!GameRules.playerDisabled && ship.health > 0 && ship.mana < ship.maxMana)
            ship.HealManaFlat(Mathf.Min(3 * Time.deltaTime, ship.maxMana - ship.mana));
    }
}
