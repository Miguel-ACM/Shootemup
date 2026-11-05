using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class FirecrackerBelt : OffensiveActiveItem
{
    void Awake()
    {
        SetData("Firecracker Belt", "Light the fuse", "Unload a straight rocket barrage. 25 energy.", ItemType.active);
        cost = 25; cd = 4;
    }
    protected override OffensiveCast Launch() => CreateCast<FirecrackerBarrage>("Firecracker barrage");
}

public sealed class FirecrackerBarrage : OffensiveCast
{
    int launched;
    protected override void Step(float dt)
    {
        while (launched < 18 && Age >= launched * .065f)
        {
            var port = Owner.transform.position + new Vector3(launched % 2 == 0 ? -.18f : .18f, .6f, -1);
            var shot = Shot(port, Vector2.up, 1.35f, 18, PlayerDelivery.Missile);
            if (shot != null) shot.LockHeading();
            ItemSpark.Emit(port, new Color(.9f, .38f, .2f), 2, .07f, true);
            ItemSound.Cue("shotMissile", volume: .10f, pitch: 1.35f + (launched % 3) * .08f, cooldown: .05f);
            launched++;
        }
        if (launched == 18) Finish();
    }
}
