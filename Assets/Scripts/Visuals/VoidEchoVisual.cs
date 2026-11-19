using UnityEngine;

// A memory borrows the original model and its moving weapon mounts. Only the Sovereign owns health.
public sealed class VoidEchoVisual
{
    public readonly LevelId district;
    public readonly Transform root, hull;
    public Transform Target { get; private set; }
    readonly Transform[] mounts = new Transform[8];
    readonly Transform[] moving = new Transform[8];
    readonly Transform[] vertebrae = new Transform[DesertWormBoss.BodyCount];
    SandSpine spine;
    public void FollowWorm(Vector3 position, Vector3 tangent)
    {
        if (tangent.sqrMagnitude > .00001f) hull.rotation = Quaternion.FromToRotation(Vector3.down,tangent);
        spine ??= new SandSpine(position,1.04f,vertebrae.Length);
        spine.Follow(position,vertebrae);
    }
    MushroomMothRig moth;
    HeavenAngelVisual angel;
    static Material holeDisk, holeEye, mothSilk, paintedFace, obsidian;
    internal static Material Obsidian
    {
        get
        {
            if (!obsidian)
            {
                obsidian = new Material(VoidSovereignBoss.Body);
                obsidian.SetFloat("_MemorySurface", 1);
            }
            return obsidian;
        }
    }

    static Material Painted(ref Material material, Texture texture)
    {
        if (!material)
        {
            material = new Material(Obsidian);
            material.SetTexture("_MemoryPaint",texture);
            material.SetFloat("_Painted",1);
        }
        return material;
    }

    Transform Part(string name, Mesh mesh, Transform parent, Vector3 pos, Material material = null)
    {
        var part = new GameObject(name).transform;
        part.SetParent(parent, false);
        part.localPosition = pos;
        part.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = part.gameObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material ?? Obsidian;
        renderer.sortingLayerName = "Enemy";
        return part;
    }

    void Mount(int index, Transform parent, Vector3 local)
    {
        var port = mounts[index] != null ? mounts[index] : new GameObject("Echo muzzle " + index).transform;
        port.SetParent(parent, false);
        port.localPosition = local;
        mounts[index] = port;
    }

    public Transform Port(int i) => angel != null ? angel.Emitter(i % 5) : mounts[i % mounts.Length];

    public VoidEchoVisual(LevelId district, Transform parent)
    {
        this.district = district;
        root = new GameObject(district + " memory").transform;
        root.SetParent(parent, false);
        var id = (int)EnemyCatalog.Get(DistrictWaves.Bosses(district)[0]).Hull;
        hull = Part("Remembered hull", IndustrialMeshFactory.CombatHull(id), root, Vector3.zero);
        Target = hull;
        if (district != LevelId.Space) IndustrialMeshFactory.AttachTerraParts("Hull" + id, hull, Obsidian);
        var layout = EnemyWeaponLayout.Ports(id);
        for (int i = 0; i < mounts.Length; i++)
            Mount(i,hull,layout.Length == 0 ? Vector3.zero : layout[i % layout.Length]);
        switch (district)
        {
            case LevelId.Mushroom:
                moth = MushroomMothRig.Attach(hull);
                foreach (var r in hull.GetComponentsInChildren<MeshRenderer>())
                    r.sharedMaterial = Painted(ref mothSilk, IndustrialMeshFactory.MothPaint.mainTexture);
                for (int i=0;i<3;i++) mounts[i]=moth.Muzzle(i);
                break;
            case LevelId.Dream:
            case LevelId.Candyland:
                bool dreaming=district==LevelId.Dream;
                for(int i=0;i<2;i++)
                {
                    float side=i*2-1;
                    var hand = Part(dreaming?"Porcelain hand":"Lollipop wand",
                        dreaming?IndustrialMeshFactory.DreamHandMesh():IndustrialMeshFactory.CandyRegentWand(),
                        hull,new Vector3(side*(dreaming?2.25f:.98f),-.05f,-.20f));
                    moving[i]=hand;
                    if(dreaming) hand.localScale=new Vector3(-side,1,1);
                    for(int f=0;f<4;f++)
                        Mount(f*2+i,hand,dreaming?IndustrialMeshFactory.DreamNibs[f]:new Vector3(0,-.98f,-.12f));
                }
                if (dreaming)
                    foreach (var r in hull.GetComponentsInChildren<MeshRenderer>())
                        r.sharedMaterial = Painted(ref paintedFace, IndustrialMeshFactory.DreamPorcelain.mainTexture);
                if (!dreaming)
                {
                    Part("Crown",IndustrialMeshFactory.CandyRegentCrown(),hull,new Vector3(0,.55f,-.12f));
                    Part("Pupil",IndustrialMeshFactory.CandyRegentPupil(),hull,new Vector3(0,.02f,-.56f));
                }
                break;
            case LevelId.Heaven:
                angel=hull.gameObject.AddComponent<HeavenAngelVisual>();
                angel.Build();
                foreach(var r in hull.GetComponentsInChildren<MeshRenderer>()) r.sharedMaterial=Obsidian;
                break;
            case LevelId.Space:
                if (!holeDisk)
                {
                    holeDisk=new Material(SpaceBlackHoleVisual.Body);
                    holeEye=new Material(SpaceBlackHoleVisual.Core);
                    holeDisk.SetFloat("_VoidEcho",1);
                    holeEye.SetFloat("_VoidEcho",1);
                }
                hull.GetComponent<MeshRenderer>().sharedMaterial=holeDisk;
                Target=Part("Event horizon",IndustrialMeshFactory.CitadelReactor(),hull,Vector3.back*.38f,holeEye);
                Mount(0,Target,Vector3.zero);
                break;
            case LevelId.Desert:
                for(int i=0;i<vertebrae.Length;i++)
                {
                    var section=Part("Vertebra",IndustrialMeshFactory.WhiteWormSection(),hull,Vector3.up*(i+1)*.88f);
                    section.localScale=Vector3.one*(.91f-i*.035f);
                    vertebrae[i]=section;
                    var core=Part("Reactor",IndustrialMeshFactory.WhiteWormCore(false),section,Vector3.back*.44f);
                    Mount(i+1,core,Vector3.zero);
                }
                Mount(0,Part("Head reactor",IndustrialMeshFactory.WhiteWormCore(true),hull,Vector3.back*.44f),Vector3.zero);
                break;
            case LevelId.Hell:
                for(int i=0;i<2;i++)
                {
                    int side=i*2-1;
                    var claw=Part("Pincer",IndustrialMeshFactory.CombatHull(54),hull,new Vector3(side*2.65f,-.6f,0));
                    claw.localScale=Vector3.one*1.55f;
                    IndustrialMeshFactory.AttachTerraParts("Hull54",claw,Obsidian);
                    moving[i]=claw;
                    moving[i+2]=Part("Arm",IndustrialMeshFactory.CrabArm(),hull,Vector3.zero);
                    Mount(i,claw,new Vector3(0,-.32f,-.4f));
                    Part("Eye",IndustrialMeshFactory.CombatHull(55),hull,new Vector3(side*.67f,-.52f,-.96f));
                }
                break;
            case LevelId.Core:
                moving[0]=Part("Reactor carriage",IndustrialMeshFactory.GantryCarriage(),hull,Vector3.back*.2f);
                Mount(2,moving[0],new Vector3(0,-.77f,-.28f));
                break;
            case LevelId.Tunnel:
                Mount(0,hull,Vector3.zero);
                break;
            case LevelId.Thunder:
                Mount(0,hull,new Vector3(-.71f,.435f,-.3f));
                Mount(1,hull,new Vector3(.71f,.435f,-.3f));
                Mount(2,hull,new Vector3(0,-1.98f,-.2f));
                break;
        }
        root.gameObject.SetActive(false);
    }

    public void Pose(float t)
    {
        moth?.Pose(.45f+.45f*Mathf.Sin(3.4f*t));
        if (angel!=null) angel.Pose(t*1.25f);
        if (district==LevelId.Tunnel) hull.localRotation=Quaternion.Euler(0,0,t*245);
        if (district==LevelId.Core) moving[0].localPosition=new Vector3(2.72f*Mathf.Sin(t*1.12f+.65f),0,-.2f);
        for(int i=0;i<2;i++)
        {
            int side=i*2-1;
            if(district==LevelId.Candyland || district==LevelId.Dream)
            {
                if(district==LevelId.Dream)
                    moving[i].localPosition=new Vector3(side*2.42f*Mathf.Cos(t*.72f),side*1.28f*Mathf.Sin(t*.72f)-.64f,-.28f);
                moving[i].localRotation=Quaternion.Euler(0,0,side*32*Mathf.Sin(t*1.44f));
            }
            if(district==LevelId.Hell)
            {
                Vector3 shoulder=new Vector3(side*1.58f,0,0);
                Vector3 tip=new Vector3(side*(2.56f+.39f*Mathf.Sin(t*2)), -.83f+.55f*Mathf.Sin(t*4),0);
                moving[i].localPosition=tip;
                moving[i].localRotation=Quaternion.Euler(0,0,side*36*Mathf.Cos(t*2));
                moving[i+2].localPosition=(shoulder+tip)*.5f;
                moving[i+2].localRotation=Quaternion.FromToRotation(Vector3.up,tip-shoulder);
                moving[i+2].localScale=new Vector3(1,(tip-shoulder).magnitude,1);
            }
        }
    }
}
