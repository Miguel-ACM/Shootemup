using UnityEngine;

public class Life : APickable
{
    void Awake()
    {
        gameObject.AddComponent<HealthPackGlow>();
    }

    protected override void PickUp()
    {
        Shipcontroller s = GameObject.FindGameObjectWithTag("Player").GetComponent<Shipcontroller>();
        HealthRecovery.Begin(s, 1, .1f);
        base.PickUp();
    }
}
