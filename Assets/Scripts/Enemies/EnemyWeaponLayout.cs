using UnityEngine;

// Barrel lips in hull space. Articulated batteries register their own transforms.
public static class EnemyWeaponLayout
{
    static Vector3 P(float x, float y, float z = -.2f) => new Vector3(x, y, z);
    static readonly System.Collections.Generic.Dictionary<int, Vector3[]> cache = new System.Collections.Generic.Dictionary<int, Vector3[]>();
    public static Vector3[] Ports(int hull)
    {
        if (!cache.TryGetValue(hull, out var ports)) cache[hull] = ports = BuildPorts(hull);
        return ports;
    }
    static Vector3[] BuildPorts(int hull)
    {
        if (hull >= 61) return EnemyExpansion.Find(hull)?.ports ?? System.Array.Empty<Vector3>();
        switch (hull)
        {
            case 0: return new[] { P(-.66f, -.16f, -.08f), P(.66f, -.16f, -.08f) };
            case 1: return new[] { P(-.64f, -1), P(.64f, -1) };
            case 2: return new[] { P(-.95f, -.68f), P(.95f, -.68f) };
            case 3: return new[] { P(-.22f, -1.115f, -.26f), P(.22f, -1.115f, -.26f) };
            case 4: return new[] { P(-.4f, -.55f), P(.4f, -.55f) };
            case 5: return new[] { P(-.74f, -.8f), P(.74f, -.8f) };
            case 6: return new[] { P(-.55f, -.7f), P(.55f, -.7f) };
            case 7: return new[] { P(-.78f, -.98f, -.34f), P(0,-1.06f,-.34f), P(.78f, -.98f, -.34f) };
            case 8: return new[] { P(-.55f, -.65f), P(.55f, -.65f) };
            case 9: return new[] { P(0, -.28f, -.24f) };
            case 10: return new[] { P(-.46f, -.995f), P(0, -1.24f), P(.46f, -.995f) };
            case 11: return new[] { P(-.42f, -.7f), P(.42f, -.7f) };
            case 12: return new[] { P(0,-1.38f,-.15f), P(-.43f, -.79f), P(.43f, -.79f) };
            case 13: return new[] { P(0, -.7f) };
            case 14: return new[] { P(-1.02f, -.72f), P(0, -.7f, -.3f), P(1.02f, -.72f) };
            case 15: return new[] { P(0, -.9f) };
            case 17: return new[] { P(-.88f, -.72f), P(.88f, -.72f) };
            case 18: return new[] { P(.52f, -1.45f) };
            case 20: case 39: case 40: case 49: return System.Array.Empty<Vector3>();
            case 21: return new[] { P(0, -.65f, -.25f) };
            case 22: return new[] { P(-.71f, .435f, -.3f), P(.71f, .435f, -.3f), P(0, -1.98f) };
            case 23: return new[] { P(-1.34f, .32f, -.36f), P(1.34f, .32f, -.36f), P(-1.4f, -.68f, -.16f), P(1.4f, -.68f, -.16f), P(-.97f, -.36f, -.51f), P(.97f, -.36f, -.51f) };
            case 24: return new[] {P(-.68f,-1.2f,-.3f),P(.68f,-1.2f,-.3f)};
            case 25: return new[] {P(-.2f,-1.09f,-.48f),P(0,-1.09f,-.48f),P(.2f,-1.09f,-.48f)};
            case 26: return new[] {P(0,-1.46f,-.3f)};
            case 27: return new[] {P(0,-.35f,-.4f)};
            case 28: return new[] {P(-1.04f,-1.33f,-.18f),P(1.04f,-1.33f,-.18f)};
            case 30: return new[] { P(0, 0, -.45f) };
            case 31: return new[] { P(0, -1.1f, -.13f) };
            case 33: return new[] { P(0, -.92f, -.18f) };
            case 35: return new[] { P(0,-.63f,-.1f) };
            case 36: return new[] {P(-.27f,-.78f,-.16f),P(.27f,-.78f,-.16f)};
            case 37: return new[] { P(0, -.73f, -.08f) };
            case 38: return new[] { P(-.27f, -1.07f), P(.27f, -1.07f), P(-.82f, -.35f), P(.82f, -.35f) };
            case 41: return new[] { P(-.19f, -.68f, -.15f), P(.19f, -.68f, -.15f) };
            case 42: return new[] { P(0, -1.05f, -.24f) };
            case 43: return new[] { P(0, -.93f) };
            case 44: return new[] { P(0, -.34f) };
            case 45: return new[] { P(0, -1.48f, -.3f), P(-.4f, -.9f, -.4f), P(.4f, -.9f, -.4f), P(0, -.7f, -.5f) };
            case 46: return new[] { P(0, -.48f, -.4f) };
            case 47: return new[] {P(-.61f,.43f,-.22f),P(-.61f,.01f,-.22f),P(-.61f,-.41f,-.22f),P(.61f,.43f,-.22f),P(.61f,.01f,-.22f),P(.61f,-.41f,-.22f)};
            case 48: return new[] { P(0, -.55f) };
            case 50: return new[] {P(-.17f,-.55f,-.2f),P(.17f,-.55f,-.2f)};
            case 51: return new[] {P(-.65f,-.2f,-.18f),P(0,-.2f,-.18f),P(.65f,-.2f,-.18f)};
            case 52: return new[] { P(-.7f, -.4f), P(.7f, -.4f) };
            case 53: return new[] { P(0, -1.65f, -.5f), P(-.65f, -.6f, -.4f), P(.65f, -.6f, -.4f), P(-1.02f, .3f, -.3f), P(1.02f, .3f, -.3f) };
            case 56: return new[] {P(-.85f,-.45f),P(.85f,-.45f),P(0,-.65f,-.3f)};
            case 57: return new[] {P(-.74f,-.55f,-.25f),P(.74f,-.55f,-.25f)};
            case 58: return new[] {P(-.82f,-.7f,-.25f),P(.82f,-.7f,-.25f)};
            case 59: return new[] {P(-.88f,.45f,-.3f),P(-.88f,-.1f,-.3f),P(-.88f,-.65f,-.3f),P(.88f,.45f,-.3f),P(.88f,-.1f,-.3f),P(.88f,-.65f,-.3f)};
            case 60: return new[] {P(-.95f,-.85f,-.3f),P(.95f,-.85f,-.3f)};
            default: return new[] { P(0, -.75f, -.3f) };
        }
    }
    public static bool NoseAimed(int hull) => hull >= 61 ? EnemyExpansion.Find(hull)?.nose ?? false : hull == 0 || hull == 12 || hull == 13 || hull == 15 || hull == 31 || hull == 36 || hull == 50;
}
