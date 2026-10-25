using UnityEngine;
using System.Collections.Generic;

public sealed class TrailDrone : ACompanion
{
    Shipcontroller owner;
    Transform leader;
    int index;
    bool laser;
    float clock=-.8f,nextDamage;
    Vector2 heading=Vector2.up;
    BeamRibbon beam;
    LaserSound sound;
    PSXShipEffects effects;
    public bool Laser=>laser;
    public static TrailDrone Attach(Shipcontroller ship,bool laser)
    {
        var siblings=ship.GetComponentsInChildren<TrailDrone>();
        var go=new GameObject(laser ? "Trailing laser drone" : "Trailing gun drone");
        go.transform.SetParent(ship.transform,false);go.transform.position=ship.transform.position;
        var drone=go.AddComponent<TrailDrone>();drone.owner=ship;drone.laser=laser;drone.index=siblings.Length;
        drone.leader=siblings.Length>0 ? siblings[siblings.Length-1].transform : ship.transform;
        drone.type=CompanionType.follower;
        // Scale models only; damage, rounds and spacing stay constant.
        float scale=.7f*Mathf.Max(.6f,Mathf.Pow(.9f,drone.index));
        go.transform.localScale=Vector3.one*(scale/ship.transform.lossyScale.x);
        go.AddComponent<MeshFilter>().sharedMesh=IndustrialMeshFactory.ExpansionItem(laser ? "LaserDroneItem" : "BulletDroneItem");
        var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=CombatMaterials.Fleet;renderer.sortingLayerName="Player";
        var source=ship.GetComponent<PSXShipEffects>();
        if(source!=null)
        {
            drone.effects=go.AddComponent<PSXShipEffects>();drone.effects.flameMesh=source.flameMesh;drone.effects.exhaustMaterial=source.exhaustMaterial;
            drone.effects.exhaustOffset=new Vector2(.22f,-.27f);drone.effects.exhaustScale=.5f;
        }
        if(laser)
        {
            var ribbon=new GameObject("Drone beam");ribbon.transform.SetParent(go.transform,false);
            drone.beam=ribbon.AddComponent<BeamRibbon>();drone.beam.Build(true);ribbon.SetActive(false);
            drone.sound=go.AddComponent<LaserSound>();
        }
        drone.clock=-.4f-drone.index*.18f;return drone;
    }
    Vector3 Muzzle=>transform.TransformPoint(new Vector3(0,.44f,-.1f));
    void Update()
    {
        if(owner==null || owner.health<=0){Destroy(gameObject);return;}
        if(Time.deltaTime<=0)return;
        if(leader==null)leader=owner.transform;
        float stagger=index%2==0 ? .19f : -.19f;
        Vector3 target=leader.position+new Vector3(stagger,-.65f,-.02f);
        transform.position=Vector3.Lerp(transform.position,target,1-Mathf.Exp(-12*Time.deltaTime));
        if(GameRules.playerDisabled)
        {
            clock=-.4f;if(beam!=null)beam.gameObject.SetActive(false);sound?.Stop();return;
        }
        if(laser)FireLaser();else FireBullets();
    }
    void FireBullets()
    {
        clock+=Time.deltaTime;if(clock<0)return;
        var target=CombatUpgrades.Nearest(Muzzle,null,18);
        if(target==null){clock=-.15f;return;}
        clock=-.55f;effects?.Shot();SoundManager.PlaySound("shot1",.045f);
        var direction=((Vector2)(target.transform.position-Muzzle)).normalized;
        foreach(int side in new[]{-1,1})
        {
            var shot=PlayerRound.Spawn(Muzzle+Vector3.right*side*.09f,direction,GameRules.playerShootDamage*.4f,GameRules.playerShootTravelSpeed);
            if(shot!=null)shot.companion=true;
        }
    }
    void FireLaser()
    {
        if(clock<0)
        {
            clock+=Time.deltaTime;if(clock<0)return;
            var target=CombatUpgrades.Nearest(Muzzle,null,18);
            if(target==null){clock=-.15f;return;}
            heading=((Vector2)(target.transform.position-Muzzle)).normalized;
            beam.gameObject.SetActive(true);sound.Charge(.4f,true);nextDamage=.4f;
        }
        clock+=Time.deltaTime;
        bool firing=clock>=.4f && clock<.95f;
        float power=firing ? Mathf.Min(Mathf.Clamp01((clock-.4f)/.06f),Mathf.Clamp01((.95f-clock)/.1f)) : clock>=.95f ? Mathf.Pow(Mathf.Clamp01(1-(clock-.95f)/.2f),2)*.3f : 0;
        beam.Draw(Muzzle,heading,18,.12f,Mathf.Clamp01(clock/.4f),power,Mathf.Max(0,clock-.95f)/.2f);
        sound.Drive(power,firing);
        if(firing && clock>=nextDamage)
        {
            nextDamage=clock+.12f;
            foreach(var enemy in new List<AEnemy>(AEnemy.Live))
            {
                if(enemy==null || !enemy.CanBeTargeted)continue;
                var delta=(Vector2)(enemy.transform.position-Muzzle);float along=Vector2.Dot(delta,heading);
                if(along<0 || along>18)continue;
                var collider=enemy.GetComponent<Collider2D>();
                var point=(Vector2)Muzzle+heading*along;
                if(collider==null || (collider.ClosestPoint(point)-point).sqrMagnitude>.1f*.1f)continue;
                float damage=GameRules.playerShootDamage*.55f;
                if(CombatUpgrades.Current!=null)damage=CombatUpgrades.Current.DirectHit(enemy,damage,0,true);
                enemy.HitAt(point,heading);enemy.damage(damage);
            }
        }
        if(clock>=1.15f){clock=-.8f;beam.gameObject.SetActive(false);sound.Stop();}
    }
    void OnDisable(){sound?.Stop();}
}
