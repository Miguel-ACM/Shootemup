using UnityEngine;

// These companions can leave formation while remaining owned by the ship's pickup.
public abstract class RoamingCompanion : ACompanion
{
    protected Vector2 Position { get; private set; }
    protected Vector2 Pilot => Owner.transform.position;
    protected Vector2 Home => new Vector2(Mathf.Clamp(Pilot.x + perch.x, Camera.LeftLimit + .23f, Camera.RightLimit - .23f),
        Mathf.Clamp(Pilot.y + perch.y, Camera.DownLimit + .24f, Camera.UpLimit - .24f));
    protected Shipcontroller Owner { get; private set; }
    protected Transform Model { get; private set; }
    protected Vector3 RestScale { get; private set; }
    protected float Age { get; private set; }
    protected int Copy { get; private set; }
    Vector2 perch;
    float angle;

    public static T Attach<T>(Shipcontroller ship, string kind, float width, Vector2 offset) where T : RoamingCompanion
    {
        var go = new GameObject(kind + " companion");
        go.transform.SetParent(ship.transform, false);
        var pet = go.AddComponent<T>();
        pet.Owner = ship;
        pet.type = CompanionType.sides;
        pet.Copy = ship.GetComponentsInChildren<T>().Length - 1;
        pet.perch = offset + new Vector2(0, -.25f * pet.Copy);
        var model = new GameObject(kind + " model");
        model.transform.SetParent(go.transform, false);
        pet.Model = model.transform;
        var mesh = IndustrialMeshFactory.ExpansionItem(kind);
        model.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = model.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = EverydayItemMaterials.For(kind);
        renderer.sortingLayerName = "Player";
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        pet.RestScale = Vector3.one * (width / (Mathf.Max(.01f, mesh.bounds.size.x) * Mathf.Max(.01f, ship.transform.lossyScale.x)));
        pet.Model.localScale = pet.RestScale;
        pet.Place(pet.Home);
        return pet;
    }

    void LateUpdate()
    {
        if (Owner == null || Owner.health <= 0)
        {
            Destroy(gameObject);
            return;
        }
        if (Menu.GameplayPaused) return;
        if (!GameClock.ActivePlay)
        {
            Park();
            Place(Home);
            return;
        }
        float dt = GameClock.PlayerDelta;
        if (dt <= 0) return;
        Age += dt;
        Step(dt);
    }

    protected abstract void Step(float dt);
    protected virtual void Park() { }

    protected void Place(Vector2 point)
    {
        Position = point;
        transform.position = new Vector3(point.x, point.y, Owner.transform.position.z - .09f);
    }

    protected void Fly(Vector2 point, float speed, float dt)
    {
        Vector2 offset = point - Position;
        if (offset.sqrMagnitude > .001f)
            angle = Mathf.MoveTowardsAngle(angle, Vector2.SignedAngle(Vector2.up, offset), dt * 900);
        Place(Vector2.MoveTowards(Position, point, speed * dt));
        Model.localRotation = Quaternion.Euler(-12, Mathf.Sin(Age * 20) * 12, angle);
    }
}
