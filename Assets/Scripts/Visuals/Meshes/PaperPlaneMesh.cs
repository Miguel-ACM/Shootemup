using UnityEngine;

public static partial class IndustrialMeshFactory
{
    // A folded dart: two raised wings meeting at a sharp nose, a keel fold beneath
    // and blue-edged trailing creases. Paper is thin, so every sheet is two-sided.
    static void PaperPlaneModel(Builder b)
    {
        void Sheet(int tile, params Vector3[] points)
        {
            b.Face(tile, points);
            var back = (Vector3[])points.Clone();
            System.Array.Reverse(back);
            b.Face(tile == 15 ? 1 : tile, back);
        }
        var nose = new Vector3(0, .46f, 0);
        var tail = new Vector3(0, -.36f, 0);
        var keel = new Vector3(0, -.3f, .14f);
        for (int side = -1; side <= 1; side += 2)
        {
            var tip = new Vector3(side * .36f, -.34f, -.07f);
            var fold = new Vector3(side * .08f, -.36f, -.02f);
            Sheet(15, nose, tip, fold, tail);
            Sheet(6, tip + new Vector3(0, .02f, -.003f), tip + new Vector3(-side * .04f, -.02f, -.003f),
                fold + new Vector3(0, -.002f, -.004f), fold + new Vector3(side * .01f, .05f, -.004f));
            Sheet(15, nose, tail, keel + new Vector3(side * .005f, 0, 0));
        }
        EquipmentRod(b, nose, tail, .008f, 9);
    }
}
