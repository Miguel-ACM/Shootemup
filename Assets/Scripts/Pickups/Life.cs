using UnityEngine;

public class Life : APickable
{
    protected override void PickUp()
    {
        Shipcontroller s = GameObject.FindGameObjectWithTag("Player").GetComponent<Shipcontroller>();
        HealthRecovery.Begin(s, 1, .1f);
        base.PickUp();
    }
}
