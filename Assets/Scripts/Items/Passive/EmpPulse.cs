using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class EmpPulse : AItem
{
    void Awake()
    {
        SetData("EMP Pulse", "Clean slate", "Clears all bullets every 20 seconds.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        if (s.GetComponent<PeriodicEmp>() == null)
            s.gameObject.AddComponent<PeriodicEmp>();
        base.OnPickup();
    }
}
public sealed class PeriodicEmp : MonoBehaviour
{
    public float Remaining { get; private set; } = 20;
    void Update()
    {
        if (GameRules.playerDisabled)
            return;
        Remaining -= Time.deltaTime;
        if (Remaining > 0)
            return;
        Remaining += 20;
        Discharge(transform.position);
    }

    public static void Discharge(Vector3 position)
    {
        foreach (var bullet in Object.FindObjectsByType<AEnemyBullet>())
            bullet.Vanish();
        foreach (var bullet in Object.FindObjectsByType<ABullet>())
        {
            bullet.gameObject.SetActive(false);
            Object.Destroy(bullet.gameObject);
        }
        PSXFireballs.Shockwave(position, 24, .65f, new Color(.24f, .75f, 1), 2, .1f);
        SoundManager.PlaySound("blankbullet", .15f);
    }
}
