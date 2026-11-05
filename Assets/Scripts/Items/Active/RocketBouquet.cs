using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class RocketBouquet : OffensiveActiveItem
{
    void Awake()
    {
        SetData("Rocket Bouquet", "Special delivery", "Twelve seeking rockets. 30 energy.", ItemType.active);
        cost = 30; cd = 5;
    }
    protected override OffensiveCast Launch() => CreateCast<RocketBouquetSalvo>("Rocket bouquet salvo");
}

public sealed class RocketBouquetSalvo : OffensiveCast
{
    int launched;
    protected override void Begin() => SnapshotTargets();
    protected override void Step(float dt)
    {
        while (launched < 12 && Age >= launched * .065f)
        {
            int side = launched % 2 == 0 ? -1 : 1;
            Vector3 port = Owner.transform.position + new Vector3(side * .36f, .35f, -1);
            var shot = Shot(port, CombatVisuals.Rotate(Vector2.up, side * (38 + launched % 3 * 9)), 2, 10, PlayerDelivery.Missile);
            if (shot != null)
                shot.GuideTo(Targets.Count > 0 ? Targets[(launched / 2 + (side > 0 ? 1 : 0)) % Targets.Count] : null, .22f);
            ItemSpark.Emit(port, new Color(.9f, .65f, .25f), 2, .09f, true);
            ItemSound.Cue("shotMissile", volume: .13f, pitch: 1 + launched * .025f, cooldown: .055f);
            launched++;
        }
        if (launched == 12) Finish();
    }
}
