using UnityEngine;

public sealed class TripodTurret : ACompanion
{
    const float TapWindow = .22f, ShotInterval = .22f;
    static readonly Color Cue = new Color(.58f, .85f, .47f);
    Shipcontroller owner;
    Vector3 anchor;
    bool deployed, focused;
    float focusAge, shotClock;

    public static void Attach(Shipcontroller ship)
    {
        var body = new GameObject("Tripod turret");
        body.transform.SetParent(ship.transform, false);
        var turret = body.AddComponent<TripodTurret>();
        turret.owner = ship;
        turret.type = CompanionType.sides;
        turret.focused = GameInput.Snapshot.Focus;
        turret.focusAge = TapWindow;
        var mesh = IndustrialMeshFactory.ExpansionItem(nameof(Tripod));
        body.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = body.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = EverydayItemMaterials.For(nameof(Tripod));
        renderer.sortingLayerName = "Player";
        body.transform.localScale = Vector3.one * (.57f / (mesh.bounds.size.x * Mathf.Max(.01f, ship.transform.lossyScale.x)));
        body.transform.position = ship.transform.position + new Vector3(-.55f, -.38f, -.06f);
    }

    void LateUpdate()
    {
        if (owner == null || owner.health <= 0)
        {
            Destroy(gameObject);
            return;
        }

        bool held = GameInput.Snapshot.Focus;
        float dt = GameClock.ActivePlay ? GameClock.PlayerDelta : 0;
        if (dt > 0)
        {
            if (held)
                focusAge = (focused ? focusAge : 0) + dt;
            else if (focused && focusAge < TapWindow)
                Toggle();
        }
        else if (held)
            focusAge = TapWindow;
        focused = held;
        if (CloudTransition.Busy || deployed && CombatWalls.Overlaps(anchor, .16f))
            deployed = false;

        Vector3 home = owner.transform.position + new Vector3(-.55f, -.38f, -.06f);
        transform.position = deployed ? anchor : home;
        if (!deployed || dt <= 0)
            return;

        shotClock -= dt;
        if (shotClock > 0 || !Shipcontroller.Firing)
            return;
        shotClock = ShotInterval;
        Vector3 muzzle = transform.position + Vector3.up * .3f;
        var shot = PlayerRound.Spawn(muzzle, Vector2.up, GameRules.playerShootDamage * .7f,
            GameRules.playerShootTravelSpeed, PlayerDelivery.Pulse);
        if (shot != null)
            shot.companion = true;
        ItemSound.Cue("shotCompanion", volume: .04f, cooldown: .13f);
    }

    void Toggle()
    {
        if (!deployed)
        {
            var siblings = owner.GetComponentsInChildren<TripodTurret>();
            int index = System.Array.IndexOf(siblings, this);
            anchor = owner.transform.position + new Vector3((index - (siblings.Length - 1) * .5f) * .52f, 0, -.06f);
            anchor.x = Mathf.Clamp(anchor.x, Camera.LeftLimit + .16f, Camera.RightLimit - .16f);
            if (CombatWalls.Overlaps(anchor, .16f))
                return;
            shotClock = 0;
        }
        ItemProcVfx.Emit(deployed ? ItemProcShape.Return : ItemProcShape.Lock, anchor, Cue, .42f);
        ItemSound.Cue("itemFocus", volume: .08f, pitch: deployed ? .75f : 1.15f);
        deployed = !deployed;
    }
}
