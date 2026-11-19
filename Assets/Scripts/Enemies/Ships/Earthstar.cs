using UnityEngine;

public sealed class Earthstar : DistrictEnemy
{
    protected override void Configure() { health = 48; hullScale = .55f; }
    protected override void Attack()
    {
        int count = Dense(7);
        for (int i = 0; i < count; i++)
        {
            float u = count == 1 ? .5f : i / (count - 1f);
            float angle = Mathf.Lerp(-72, 72, u) + ((volley & 1) == 0 ? 9 : -9);
            var spore = Fire<SimpleRound>(Muzzle(), CombatVisuals.Rotate(Vector2.down, angle),
                1.55f + .35f * Mathf.Sin(u * Mathf.PI), .20f, i == 0).Ramp(0, 2.8f);
            if (spore != null) spore.settlingSpore = true;
        }
    }
}

public static partial class IndustrialMeshFactory
{
    static Mesh earthstar;
    public static Mesh EarthstarMesh()
    {
        if (earthstar != null) return earthstar;
        var b = new Builder();
        for (int i = 0; i < 6; i++)
        {
            var turn = Quaternion.Euler(0, 0, i * 60);
            FoldedFin(b, turn * new Vector3(-.19f,.14f,0), turn * new Vector3(-.26f,.49f,-.06f),
                turn * new Vector3(.05f,.8f,.04f), turn * new Vector3(.21f,.39f,0),
                turn * new Vector3(0,.42f,-.16f), 8, 14);
        }
        b.Drum(new Vector3(0,0,-.14f), .32f, .24f, 15, 8);
        FieldPort(b, new Vector3(0,0,-.27f), .16f, 13);
        return earthstar = b.Finish("Earthstar spore pod");
    }
}

public static partial class WavePartLibrary
{
    public static readonly WavePart[] EarthstarParts =
    {
        DeepPart("Spores across the clearing", nameof(Earthstar), WaveShape.Train, 3, 5.6f, 3, new[] { .30f, .54f, .72f },
            -1.4f,.86f,-.78f,.73f,-.39f,.49f,.12f,.47f,.58f,.68f,1.4f,.78f),
        DeepPart("Overlapping fairy arcs", nameof(Earthstar), WaveShape.Rake, 2, 5.2f, 5, new[] { .29f, .56f },
            .72f,1.32f,.62f,.83f,.19f,.61f,-.25f,.56f,-.68f,.71f,-1.4f,.95f),
    };
}
