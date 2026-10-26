using UnityEngine;

// Separate cosmetic machinery: never scale/rotate the hull, weakpoint or collider.
// Each district has its own deployment motion, built before combat starts.
public sealed class BossPhaseEntry : MonoBehaviour
{
    readonly Transform[] vanes = new Transform[8];
    readonly Vector3[] homes = new Vector3[8];
    DistrictBoss boss;
    float clock;
    bool running;
    public void Build(DistrictBoss owner)
    {
        boss = owner;
        for (int i = 0; i < vanes.Length; i++)
        {
            var go = new GameObject("Phase mechanism " + i);
            vanes[i] = go.transform;
            go.transform.SetParent(owner.Hull, false);
            go.AddComponent<MeshFilter>().sharedMesh = IndustrialMeshFactory.PhaseVane(owner.hull);
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = owner.Hull.GetComponent<MeshRenderer>().sharedMaterial;
            renderer.sortingLayerName = "Enemy";
            float a = i * Mathf.PI * .25f;
            homes[i] = new Vector3(Mathf.Cos(a) * .57f, Mathf.Sin(a) * .57f, -.55f);
            go.SetActive(false);
        }
    }
    public void Begin()
    {
        clock = 0;
        running = true;
        for (int i = 0; i < vanes.Length; i++)
        {
            bool alive = true;
            if (boss is DesertWormBoss worm && worm.Cores[i] != null)
            {
                vanes[i].SetParent(worm.Cores[i].transform, false);
                alive = worm.Cores[i].health > 0;
            }
            vanes[i].gameObject.SetActive(alive);
        }
        PSXImpactEffects.Burst(boss.transform.position, LevelProjectilePalette.Accent(LevelRun.Current), true);
    }
    void Update()
    {
        if (!running || GameRules.playerDisabled)
            return;
        clock += Time.deltaTime;
        float t = Mathf.Clamp01(clock / 1.45f), open = Mathf.Sin(t * Mathf.PI), settle = Mathf.SmoothStep(0, 1, t);
        for (int i = 0; i < vanes.Length; i++)
        {
            float s = i % 2 == 0 ? -1 : 1, a = i * Mathf.PI * .25f;
            var p = homes[i];
            float turn = i * 45;
            if (boss is FieldsBomberBoss)
            {
                p = new Vector3(s * (.8f + open * .7f), .55f - (i / 2) * .3f, -.65f);
                turn = s * (25 + open * 65);
            }
            else if (boss is SnowSiegeBoss)
            {
                a += settle * .8f;
                p = new Vector3(Mathf.Cos(a) * (.55f + open * .7f), Mathf.Sin(a) * (.55f + open * .7f), -.65f);
                turn = a * Mathf.Rad2Deg;
            }
            else if (boss is ThunderLanceBoss)
            {
                p = new Vector3(s * (.55f + open * .35f), .6f - (i / 2) * .37f, -.4f);
                turn = s * open * 80;
            }
            else if (boss is CityDuoBoss)
            {
                p = new Vector3(s * (.4f + open * .35f), .65f - (i / 2) * .28f, -.4f);
                turn = s * open * 120;
            }
            else if (boss is TunnelSawBoss)
            {
                a += settle * 3;
                p = new Vector3(Mathf.Cos(a) * (1 + open * .3f), Mathf.Sin(a) * (1 + open * .3f), -.35f);
                turn = a * Mathf.Rad2Deg;
            }
            else if (boss is DesertWormBoss)
            {
                float ripple = Mathf.Sin(Mathf.Clamp01((t - i * .035f) / .72f) * Mathf.PI);
                p = new Vector3(0, .16f + ripple * .36f, -.26f);
                turn = s * ripple * 65;
            }
            else if (boss is CandyRegentBoss)
            {
                p = new Vector3(Mathf.Cos(a) * (.4f + open * .45f), .8f + open * .6f + Mathf.Sin(a) * .2f, -.5f);
                turn = i * 45 + settle * 180;
            }
            else if (boss is SpaceCitadelBoss)
            {
                p = homes[i] * (1 + open * 1.1f);
                p.z = -.6f;
                turn = i * 45 + open * 80;
            }
            else if (boss is AbyssSubmarineBoss)
            {
                p = new Vector3(s * (.55f + open * .28f), .7f - (i / 2) * .45f, -.6f);
                turn = s * open * 65;
            }
            else if (boss is HellCrabBoss)
            {
                p = new Vector3(s * (.6f + open * .5f), .45f - (i / 2) * .28f, -.9f);
                turn = s * (25 + open * 55);
            }
            vanes[i].localPosition = p;
            vanes[i].localRotation = Quaternion.Euler(open * 35, 0, turn);
            vanes[i].localScale = Vector3.one * (.7f + open * .45f) * Mathf.Min(1, (1 - t) * 6);
        }
        if (t >= 1)
        {
            running = false;
            foreach (var vane in vanes)
                vane.gameObject.SetActive(false);
        }
    }
}

public static partial class IndustrialMeshFactory
{
    static readonly System.Collections.Generic.Dictionary<int, Mesh> phaseVanes = new System.Collections.Generic.Dictionary<int, Mesh>();
    public static Mesh PhaseVane(int hull)
    {
        if (phaseVanes.TryGetValue(hull, out var mesh) && mesh != null)
            return mesh;
        var b = new Builder();
        if (hull == 23) // Wing airbrakes open in opposing pairs.
            BevelArmor(b, new[] { V(-.2f, -.32f), V(.2f, -.18f), V(.33f, .28f), V(-.14f, .4f) }, .15f, 6);
        else if (hull == 21) // Curved shutter petals around Snow's circular pressure hull.
        {
            b.Prism(new[] { V(-.23f, -.22f), V(.14f, -.3f), V(.34f, .25f), V(.13f, .52f), V(-.16f, .37f) }, .12f, 4, Vector3.zero, 6);
            b.Box(new Vector3(0, .08f, -.1f), new Vector3(.08f, .4f, .04f), 10, 11);
        }
        else if (hull == 22) // Long charged capacitor paddles.
        {
            b.Box(Vector3.zero, new Vector3(.25f, .76f, .16f), 7, 11);
            for (int j = 0; j < 4; j++)
                b.Box(new Vector3(0, (j - 1.5f) * .16f, -.12f), new Vector3(.32f, .065f, .06f), 10, 11);
        }
        else if (hull == 12 || hull == 14) // City survivor's flared engine louvers.
            b.Prism(new[] { V(-.22f, -.36f), V(.22f, -.36f), V(.3f, .15f), V(0, .4f), V(-.3f, .15f) }, .15f, 1, Vector3.zero, 6);
        else if (hull == 20)
            b.Prism(new[] { V(-.2f, -.18f), V(.2f, -.18f), V(.06f, .54f), V(-.16f, .31f) }, .16f, 6, Vector3.zero, 10);
        else if (hull == 33) // One vent opens on each surviving articulated reactor.
        {
            b.Box(Vector3.zero, new Vector3(.43f, .4f, .15f), 15, 11);
            for (int j = 0; j < 3; j++)
                b.Box(new Vector3((j - 1) * .12f, 0, -.1f), new Vector3(.05f, .29f, .035f), 10, 11);
        }
        else if (hull == 38)
            b.Prism(new[] { V(-.17f, 0), V(0, .62f), V(.17f, 0), V(0, -.2f) }, .16f, 10, Vector3.zero, 4);
        else if (hull == 45) // Eight blades briefly expose the iris mechanism.
            BevelArmor(b, new[] { V(-.24f, -.17f), V(.12f, -.32f), V(.27f, .22f), V(.02f, .56f), V(-.24f, .3f) }, .15f, 15);
        else if (hull == 53)
        {
            b.Drum(Vector3.zero, .18f, .45f, 8, 10, 6);
            b.Box(new Vector3(0, .21f, -.2f), new Vector3(.28f, .48f, .13f), 6, 11);
        }
        else if (hull == 49)
            BevelArmor(b, new[] { V(-.25f, -.26f), V(.25f, -.26f), V(.35f, .2f), V(0, .52f), V(-.35f, .2f) }, .2f, 6);
        else
        {
            b.Box(Vector3.zero, new Vector3(.18f, .38f, .1f), hull == 45 ? 15 : 6, 11);
            b.Box(Vector3.back * .07f, new Vector3(.055f, .24f, .03f), 10, 11);
        }
        return phaseVanes[hull] = b.Finish("Deploying phase vent " + hull);
    }
}
