using UnityEngine;

public static partial class IndustrialMeshFactory
{
    static readonly Mesh[] dockMachines = new Mesh[3];
    // Sculpted top-down machinery uses the existing ship trim atlas and shared material.
    public static Mesh DockMachine(int kind)
    {
        if (dockMachines[kind] != null)
            return dockMachines[kind];
        var b = new Builder();
        if (kind == 0)
        {
            b.Prism(new[] {new Vector2(-1,-1.9f),new Vector2(1,-1.9f),new Vector2(1.45f,-1),
                new Vector2(1.45f,1),new Vector2(.9f,1.7f),new Vector2(-.9f,1.7f),new Vector2(-1.45f,1),new Vector2(-1.45f,-1)}, .48f, 6, Vector3.zero, 11);
            b.Prism(new[] {new Vector2(-.75f,-1.4f),new Vector2(.75f,-1.4f),new Vector2(.9f,.65f),
                new Vector2(.5f,1.25f),new Vector2(-.5f,1.25f),new Vector2(-.9f,.65f)}, .35f, 11, Vector3.back * .37f, 6);
            b.Box(new Vector3(0, -.6f, -.64f), new Vector3(1, 1.1f, .28f), 7, 11);
            b.Box(new Vector3(0, -1.15f, -.78f), new Vector3(.95f, .23f, .3f), 6, 11);
            b.Box(new Vector3(0, .65f, -.7f), new Vector3(1.2f, .6f, .18f), 9, 6);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(new Vector3(side * 1.2f, -.3f, -.5f), new Vector3(.32f, 1.7f, .42f), 6, 11);
                b.Box(new Vector3(side * .75f, .15f, -.82f), new Vector3(.16f, .75f, .16f), 6, 11);
                b.Ball(new Vector3(side * .75f, -.1f, -.96f), Vector3.one * .18f, 10);
                for (int j = 0; j < 4; j++)
                    b.Box(new Vector3(side * 1.21f, .3f - j * .27f, -.77f), new Vector3(.18f, .12f, .08f), j == 0 ? 9 : 11);
            }
            b.Box(new Vector3(0, 1.2f, -.6f), new Vector3(1.2f, .13f, .2f), 6, 11);
        }
        else if (kind == 1)
        {
            b.Box(Vector3.zero, new Vector3(2.5f, 3.7f, .55f), 6, 11);
            b.Box(new Vector3(0, .6f, -.38f), new Vector3(2.22f, 1.65f, .3f), 11);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Drum(new Vector3(side * .55f, .7f, -.61f), .47f, .12f, 6, 16, 11);
                b.Drum(new Vector3(side * .55f, .7f, -.7f), .13f, .13f, 10, 8);
                for (int spoke = 0; spoke < 5; spoke++)
                {
                    float angle = spoke * Mathf.PI * 2 / 5;
                    b.Drum(new Vector3(side * .55f + Mathf.Cos(angle) * .29f, .7f + Mathf.Sin(angle) * .29f, -.69f), .09f, .025f, 11, 8);
                }
            }
            for (int drive = 0; drive < 3; drive++)
            {
                b.Box(new Vector3(0, -.35f - drive * .45f, -.39f), new Vector3(2, .31f, .25f), 11, 6);
                b.Box(new Vector3(-.3f, -.35f - drive * .45f, -.54f), new Vector3(1, .065f, .04f), 6);
                b.Box(new Vector3(.78f, -.35f - drive * .45f, -.55f), new Vector3(.15f, .09f, .05f), 9);
            }
        }
        else
        {
            b.Prism(new[]{new Vector2(-1.5f,-1),new Vector2(1.5f,-1),new Vector2(1.8f,.7f),new Vector2(1.4f,1.3f),
                new Vector2(-1.4f,1.3f),new Vector2(-1.8f,.7f)}, .65f, 6, Vector3.zero, 11);
            b.Box(new Vector3(0, .1f, -.4f), new Vector3(2.85f, 1.7f, .18f), 11, 6);
            b.Box(new Vector3(0, .1f, -.52f), new Vector3(2.6f, 1.45f, .06f), 9, 11);
            // The route plot is engraved in the map table, with two branching routes.
            for (int i = 0; i < 5; i++)
            {
                float y = -.4f + i * .25f;
                b.Drum(new Vector3(i % 2 == 0 ? -.5f : .5f, y, -.59f), .1f, .04f, 10, 6);
                if (i < 4)
                    b.Box(new Vector3(0, y + .12f, -.58f), new Vector3(1, .035f, .03f), 6);
            }
            for (int side = -1; side <= 1; side += 2)
                b.Box(new Vector3(side * 1.15f, -.94f, -.43f), new Vector3(.3f, .22f, .16f), 10, 11);
        }
        return dockMachines[kind] = b.Finish("Dock machinery " + kind);
    }
}
