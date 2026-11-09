using UnityEngine;

public static partial class IndustrialMeshFactory
{
    // Two tumbling dice: a ruby die showing a white six and an ivory die with red pips,
    // tilted against each other so three faces of each read.
    static void LoadedDiceModel(Builder b)
    {
        Die(b, new Vector3(-.13f, -.12f, 0), .3f, Quaternion.Euler(-24, 32, 12), 6, 15);
        Die(b, new Vector3(.17f, .17f, -.06f), .25f, Quaternion.Euler(-18, 24, -28), 15, 12);
    }

    static void Die(Builder b, Vector3 centre, float size, Quaternion turn, int body, int pip)
    {
        float h = size * .5f;
        Vector3 P(float x, float y, float z) => centre + turn * new Vector3(x, y, z);
        // Faces wind toward the camera (negative z) like Builder.Box.
        b.Face(body, P(-h, -h, -h), P(-h, h, -h), P(h, h, -h), P(h, -h, -h));
        b.Face(body, P(h, -h, h), P(h, h, h), P(-h, h, h), P(-h, -h, h));
        b.Face(body, P(-h, -h, h), P(-h, h, h), P(-h, h, -h), P(-h, -h, -h));
        b.Face(body, P(h, -h, -h), P(h, h, -h), P(h, h, h), P(h, -h, h));
        b.Face(body, P(-h, h, -h), P(-h, h, h), P(h, h, h), P(h, h, -h));
        b.Face(body, P(-h, -h, h), P(-h, -h, -h), P(h, -h, -h), P(h, -h, h));
        float r = size * .085f, o = size * .27f;
        void Pip(Vector3 normal, Vector3 right, Vector3 up, float u, float v)
        {
            Vector3 c = normal * (h + .004f) + right * u + up * v;
            b.Face(pip, P(c.x - right.x * r - up.x * r, c.y - right.y * r - up.y * r, c.z - right.z * r - up.z * r),
                P(c.x - right.x * r + up.x * r, c.y - right.y * r + up.y * r, c.z - right.z * r + up.z * r),
                P(c.x + right.x * r + up.x * r, c.y + right.y * r + up.y * r, c.z + right.z * r + up.z * r),
                P(c.x + right.x * r - up.x * r, c.y + right.y * r - up.y * r, c.z + right.z * r - up.z * r));
        }
        // Front: six.
        for (int col = -1; col <= 1; col += 2)
            for (int row = -1; row <= 1; row++)
                Pip(Vector3.back, Vector3.right, Vector3.up, col * o, row * o);
        // Top: three on the diagonal.
        for (int i = -1; i <= 1; i++)
            Pip(Vector3.up, Vector3.right, Vector3.forward, i * o, -i * o);
        // Right: five.
        Pip(Vector3.right, Vector3.forward, Vector3.up, 0, 0);
        for (int a = -1; a <= 1; a += 2)
            for (int c = -1; c <= 1; c += 2)
                Pip(Vector3.right, Vector3.forward, Vector3.up, a * o, c * o);
    }
}
