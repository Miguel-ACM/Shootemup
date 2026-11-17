using UnityEngine;

// The wing hinge raises the outer panel as it sweeps aft. The cannon exits
// are children of those hinges, so the charge, flash and round agree throughout.
public sealed class MushroomMothRig
{
    readonly Transform[] hinges = new Transform[2];
    readonly Transform[] ports = new Transform[3];

    public static MushroomMothRig Attach(Transform hull)
    {
        var result = new MushroomMothRig();
        var skin = hull.GetComponent<MeshRenderer>();
        hull.GetComponent<MeshFilter>().sharedMesh = IndustrialMeshFactory.MushroomWardenBodyMesh();
        for (int i = 0; i < result.hinges.Length; i++)
        {
            bool right = i == 1;
            Vector3 pivot = right ? IndustrialMeshFactory.WardenRightPivot : IndustrialMeshFactory.WardenLeftPivot;
            Vector3 exit = right ? IndustrialMeshFactory.WardenRightMuzzle : IndustrialMeshFactory.WardenLeftMuzzle;
            Transform hinge = Marker(hull, right ? "Right moth wing" : "Left moth wing", pivot);
            hinge.gameObject.AddComponent<MeshFilter>().sharedMesh = IndustrialMeshFactory.MushroomWardenWingMesh(right);
            var plate = hinge.gameObject.AddComponent<MeshRenderer>();
            plate.sharedMaterial = skin.sharedMaterial;
            plate.sortingLayerID = skin.sortingLayerID;
            plate.sortingOrder = skin.sortingOrder;
            result.hinges[i] = hinge;
            result.ports[i] = Marker(hinge, "Spore cannon exit", exit - pivot);
        }
        result.ports[2] = Marker(hull, "Keel gun exit", IndustrialMeshFactory.WardenNoseMuzzle);
        return result;
    }

    static Transform Marker(Transform parent, string name, Vector3 local)
    {
        var marker = new GameObject(name).transform;
        marker.SetParent(parent, false);
        marker.localPosition = local;
        return marker;
    }

    public Transform Muzzle(int index) => ports[index];

    public void Pose(float fold)
    {
        // The leading edge lifts first. The backwards sweep follows the stroke.
        float lift = Mathf.Sin(Mathf.Clamp01(fold) * Mathf.PI * .5f) * 28;
        float sweep = Mathf.SmoothStep(0, 31, fold);
        for (int i = 0; i < hinges.Length; i++)
        {
            int side = i * 2 - 1;
            hinges[i].localRotation = Quaternion.AngleAxis(side * sweep, Vector3.forward) *
                Quaternion.AngleAxis(side * lift, Vector3.up);
        }
    }
}
