using UnityEngine;

public class AngryMoon : BlockerOrbital
{

    private float shootSpeed = 4f;
    private float shootDistance = 16f;
    private GameObject lineDrawerObject;
    private LineDrawer lineDrawer;

    public void Start()
    {
        lineDrawerObject = Instantiate(Resources.Load<GameObject>("Bullets/LineDrawer"));
        lineDrawer = lineDrawerObject.GetComponent<LineDrawer>();
        InvokeRepeating("Shoot", 0f, 1 / shootSpeed);
    }

    private void Shoot()
    {
        float distance;
        GameObject other = Utils.FindClosestWithTag(this.transform.position, "EnemyBullet", out distance);
        if (distance < shootDistance)
        {
            Vector3 positionOther = other.transform.position;
            positionOther.z = this.transform.position.z;
            lineDrawer.DrawLine(new Vector3[] { this.transform.position, positionOther }, 0.1f);
            other.GetComponent<EnemyBullet>().OnTrigger();
        }
    }
}
