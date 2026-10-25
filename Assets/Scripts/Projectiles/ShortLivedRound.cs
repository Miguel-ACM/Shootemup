using UnityEngine;
public sealed class ShortLivedRound : MonoBehaviour
{
    public float duration = .5f;
    float age;
    void Update()
    {
        if (WorldTimeStop.Frozen)
            return;
        age += Time.deltaTime;
        if (age >= duration)
            Destroy(gameObject);
    }
}
