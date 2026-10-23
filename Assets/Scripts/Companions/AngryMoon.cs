using UnityEngine;

public class AngryMoon : BlockerOrbital
{

    private float shootSpeed = 4f;
    private float shootDistance = 4f;
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
        GameObject other = Utils.FindClosestWithTag(this.transform.position, "EnemyBullet", out float sqrDistance);
        if (sqrDistance < shootDistance * shootDistance)
        {
            Vector3 positionOther = other.transform.position;
            positionOther.z = this.transform.position.z;
            lineDrawer.DrawLine(new Vector3[] { this.transform.position, positionOther }, 0.1f);
            var bullet=other.GetComponent<AEnemyBullet>();
            if(bullet is PulseLaser || bullet is SegmentedBeam)return;
            if(CombatUpgrades.Current!=null)CombatUpgrades.Current.Block();
            bullet.OnTrigger();
        }
    }
    void OnDestroy(){if(lineDrawerObject!=null)Destroy(lineDrawerObject);}
}
