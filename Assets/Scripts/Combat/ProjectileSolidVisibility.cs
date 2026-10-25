using UnityEngine;

// Legacy prefabs remove themselves via renderer visibility callbacks on their root.
// The replacement renderer lives on a child because SpriteRenderer excludes MeshFilter.
public sealed class ProjectileSolidVisibility : MonoBehaviour
{
    void OnBecameInvisible()
    {
        if(transform.parent!=null)transform.parent.SendMessage("OnBecameInvisible",SendMessageOptions.DontRequireReceiver);
    }
}
