using UnityEngine;

// Rolling ground hulls share firing clocks with the wave; their models belong to the scenery depth.
public abstract class GroundVehicle : DistrictEnemy
{
    PSXGrassField field;
    Transform tile,weapon;
    Vector3 local,planned,previous;
    bool hasPrevious;
    float roadY;
    bool drivingRoad;
    GroundTreadMarks marks;
    MeshRenderer[] parts;
    MaterialPropertyBlock tint;
    public bool Grounded=>tile!=null;
    public Transform GroundSection=>tile;
    public Vector3 GroundPoint=>local;
    protected Transform Weapon=>weapon;
    protected abstract bool Snow {get;}
    public override bool CanBeTargeted=>health>0 && Grounded;
    public override void FlyPhrase(WaveUnit unit)
    {
        base.FlyPhrase(unit);var flight=movement as FormationFlight;if(flight!=null)flight.SurfaceDriver=this;
    }
    protected override void Start()
    {
        base.Start();GetComponent<CombatHullVisual>().enabled=false;
        field=FindAnyObjectByType<PSXGrassField>();
        if(movement==null)
        {
            var part=Snow ? WavePartLibrary.SnowGround[0] : WavePartLibrary.PlainsGround[0];
            FlyPhrase(part.Generate(0,1)[0]);
        }
        var turret=new GameObject("Articulated ground weapon");turret.transform.SetParent(Hull,false);turret.transform.localPosition=Vector3.back*.12f;
        turret.AddComponent<MeshFilter>().sharedMesh=IndustrialMeshFactory.GroundWeapon(hull);
        var renderer=turret.AddComponent<MeshRenderer>();renderer.sharedMaterial=CombatMaterials.Fleet;weapon=turret.transform;
        BuildRunningGear();parts=Hull.GetComponentsInChildren<MeshRenderer>();
        foreach(var part in parts){part.sortingLayerName="Background";part.sortingOrder=-32;part.enabled=false;}
        tint=new MaterialPropertyBlock();marks=gameObject.AddComponent<GroundTreadMarks>();marks.Snow=Snow;
        planned=transform.position;
    }
    protected virtual void BuildRunningGear(){}
    public Vector3 ProjectToSurface(Vector3 position)
    {
        if(!Snow && field!=null)
        {
            if(!drivingRoad){drivingRoad=true;roadY=position.y;}
            var flight=movement as FormationFlight;
            float drive=1.45f+Mathf.Clamp((5.1f-(flight!=null ? flight.duration : 4.8f))*.8f,0,.9f);
            roadY-=Time.deltaTime*(field.GroundScrollSpeed+drive*GameRules.enemySpeed*LevelRun.EnemyTempoScale*(Challenged ? 1.15f : 1));
            position.y=roadY;
        }
        planned=position;Sample(position);if(!Grounded)return position;
        var p=tile.TransformPoint(local);return new Vector3(p.x,p.y,1);
    }
    void Sample(Vector3 position)
    {
        if(Snow)LevelEnvironment.TrySnowGround(position,out tile,out local);
        else if(field!=null)field.SampleRoad(position.y,out tile,out local);
    }
    protected override void Update()
    {
        GetComponent<Collider2D>().enabled=CanBeTargeted;base.Update();
    }
    protected virtual void LateUpdate()
    {
        Sample(planned);if(parts==null)return;
        foreach(var part in parts)part.enabled=Grounded;
        if(!Grounded)return;
        var p=tile.TransformPoint(local);transform.position=new Vector3(p.x,p.y,1);Hull.position=p+Vector3.back*.075f;
        float scroll=Snow ? 4.4f : field.GroundScrollSpeed;
        Vector2 driving=hasPrevious ? (Vector2)(p-previous)+Vector2.up*scroll*Time.deltaTime : Vector2.up;
        if(!Snow)driving=field.RoadDownDirection(p.y)*Mathf.Max(.001f,driving.magnitude);
        if(driving.sqrMagnitude>.00001f)
        {
            var target=Quaternion.FromToRotation(Vector3.down,driving.normalized);
            Hull.rotation=!hasPrevious && !Snow ? target : Quaternion.RotateTowards(Hull.rotation,target,Time.deltaTime*360);
        }
        if(player!=null)
        {
            var target=Quaternion.FromToRotation(Vector3.down,(Vector2)(player.position-weapon.position));
            weapon.rotation=Quaternion.RotateTowards(weapon.rotation,target,Time.deltaTime*(Snow ? 160 : 220));
        }
        tint.SetColor("_Tint",GetComponent<SpriteRenderer>().color);foreach(var part in parts)part.SetPropertyBlock(tint);
        if(hasPrevious)marks.Step(tile,p,Hull.rotation,driving.magnitude);
        previous=p;hasPrevious=true;
    }
}
