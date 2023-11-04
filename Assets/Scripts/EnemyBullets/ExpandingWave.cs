using UnityEngine;
using System.Collections;

public class ExpandingWave : AEnemyBullet
{
    [SerializeField] private float expandSpeed = 1f;
    [SerializeField] private float rotationSpeed = 0f;
    private float timeToDie = 10f;

    protected override void Start()
    {
        base.Start();
        StartCoroutine("DestroySelf");
        dieOnTrigger = false;
    }

    protected override void Move()
    {
        transform.localScale = new Vector3(transform.localScale.x + Time.deltaTime * expandSpeed, transform.localScale.y + Time.deltaTime * expandSpeed, transform.localScale.z);
        transform.Rotate(transform.rotation.x, transform.rotation.y, Time.deltaTime * rotationSpeed);
    }
    IEnumerator DestroySelf()
    {
        yield return new WaitForSeconds(timeToDie);
        Destroy(gameObject);
        // Code to execute after the delay
    }
}