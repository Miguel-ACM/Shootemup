using UnityEngine;

/// <summary>Keep original root visibility callbacks when its visual is a child.</summary>
public sealed class PSXVisibilityRelay : MonoBehaviour
{
    public GameObject target;
    void OnBecameVisible(){if(target!=null)target.SendMessage("OnBecameVisible",SendMessageOptions.DontRequireReceiver);}
    void OnBecameInvisible(){if(target!=null)target.SendMessage("OnBecameInvisible",SendMessageOptions.DontRequireReceiver);}
}
