using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class CommandGunship : DistrictEnemy
{
    protected override void Configure(){hull=14;health=60;hullScale=.8f;attackInterval=2;Mover<TopTracker>();}
    protected override void Attack(){Fan<ReturningDisc>(2,25,3.5f,.24f);}

    protected override void Start()
    {
        base.Start();foreach(int side in new[]{-1,1})
        {
            var e=Spawn(typeof(ScoutInterceptor),transform.position+Vector3.right*side*1.4f);
            e.Escort(transform);
        }
    }
}
