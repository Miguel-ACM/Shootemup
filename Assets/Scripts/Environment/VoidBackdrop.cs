using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Six shared scenery draws. Everything falls/deforms on the GPU; no physics,
// per-prop Update, runtime mesh uploads or spawning during the final fight.
public sealed class VoidBackdrop : MonoBehaviour
{
    readonly List<Material> materials = new List<Material>(6);
    readonly List<Mesh> meshes = new List<Mesh>(4);
    static readonly int Clock = Shader.PropertyToID("_Clock");
    float clock;

    void Start()
    {
        float seed = (LevelRun.Seed % 791) * .01f;
        var abyss = Material("VoidFalls");
        abyss.SetFloat("_Seed", seed);
        var backdrop = new Mesh { name = "Unbroken Void" };
        backdrop.vertices = new[] { new Vector3(-18,-24), new Vector3(18,-24), new Vector3(18,24), new Vector3(-18,24) };
        backdrop.triangles = new[] { 0,2,1,0,3,2 }; backdrop.RecalculateBounds();
        meshes.Add(backdrop);
        Add("Black distance", backdrop, abyss, 115, -80);

        var falls = Curtain();
        for (int layer = 0; layer < 3; layer++)
        {
            var liquid = Material("VoidFalls");
            liquid.SetFloat("_Curtain", 1);
            liquid.SetFloat("_Seed", seed + layer * 3.71f);
            liquid.SetFloat("_Edge", 2.95f + layer * .46f);
            liquid.SetFloat("_Flow", .8f + layer * .39f);
            liquid.SetFloat("_Shade", .52f + layer * .24f);
            Add("Ink cascade / layer " + layer, falls, liquid, 103 - layer * 10, -70 + layer * 10);
        }

        var random = new System.Random(unchecked(LevelRun.Seed + 27349));
        var cubes = new DriftMesh("Tumbling obsidian cubes");
        var cube = BevelCube();
        for (int i = 0; i < 48; i++)
        {
            bool near = i % 6 == 0;
            float size = near ? Range(random,.55f,.95f) : Range(random,.16f,.55f);
            float x = near ? (i%12==0?-1:1)*Range(random,3.2f,4.9f) : Range(random,-5.4f,5.4f);
            var fall = new Vector4(x, Range(random,-16,16), near?Range(random,1.0f,1.6f):Range(random,.35f,.95f), Range(random,0,6.28f));
            var spin = new Vector4(Range(random,-.3f,.3f), Range(random,-.35f,.35f), Range(random,-.22f,.22f), near?91:109);
            cubes.Append(cube, Vector3.one*size, fall, spin, near?1:.62f);
        }
        var stone = Material("VoidDrift");
        Add("Falling cubes", cubes.Finish(), stone, 0, -65);

        var drops = new DriftMesh("Falling viscous beads");
        var drop = Droplet();
        for (int i = 0; i < 44; i++)
        {
            float side = i%2==0?-1:1;
            var fall = new Vector4(side*Range(random,2.95f,5.15f), Range(random,-16,16), Range(random,1.8f,3.5f), Range(random,0,6.28f));
            var spin = new Vector4(0,0,0,79+Range(random,0,3));
            drops.Append(drop, new Vector3(Range(random,.12f,.34f),Range(random,.55f,1.6f),.22f), fall, spin, Range(random,.65f,1));
        }
        var wet = Material("VoidDrift"); wet.SetFloat("_Liquid",1);
        Add("Detached ink droplets", drops.Finish(), wet, 0, -45);
    }

    static float Range(System.Random random, float a, float b) => Mathf.Lerp(a,b,(float)random.NextDouble());
    Material Material(string shader)
    {
        var result = new Material(Resources.Load<Shader>(shader)); materials.Add(result); return result;
    }
    void Add(string label, Mesh mesh, Material material, float depth, int order)
    {
        if (!meshes.Contains(mesh)) meshes.Add(mesh);
        var go = new GameObject(label) { layer = gameObject.layer };
        go.transform.SetParent(transform,false); go.transform.localPosition = new Vector3(0,0,depth);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
        renderer.sortingLayerName = "Background"; renderer.sortingOrder = order;
        renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
    }

    static Mesh Curtain()
    {
        const int rows=144, cols=16;
        var vertices=new Vector3[(rows+1)*(cols+1)*2];var uv=new Vector2[vertices.Length];
        var triangles=new int[rows*cols*12];int k=0;
        for(int side=0;side<2;side++)
            for(int y=0;y<=rows;y++)for(int x=0;x<=cols;x++)
            {
                int id=side*(rows+1)*(cols+1)+y*(cols+1)+x;
                float u=x/(float)cols;
                vertices[id]=new Vector3((side==0?-1:1)*(3+u*6),-24+y/3f,0);
                uv[id]=new Vector2(u,side);
                if(y==rows||x==cols)continue;
                int a=id,b=id+1,c=id+cols+1,d=c+1;
                triangles[k++]=a;triangles[k++]=c;triangles[k++]=b;
                triangles[k++]=b;triangles[k++]=c;triangles[k++]=d;
            }
        var mesh=new Mesh{name="Continuous folded liquid / shared seams"};
        mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;
        mesh.bounds=new Bounds(Vector3.zero,new Vector3(23,49,4));return mesh;
    }

    sealed class Shape
    {
        public readonly List<Vector3> vertices=new List<Vector3>();
        public readonly List<Vector3> normals=new List<Vector3>();
        public readonly List<int> triangles=new List<int>();
        public void Face(params Vector3[] points)
        {
            int start=vertices.Count;
            Vector3 normal=Vector3.Cross(points[1]-points[0],points[2]-points[0]).normalized;
            Vector3 centre=Vector3.zero;foreach(var p in points)centre+=p;
            bool flip=Vector3.Dot(normal,centre)<0;if(flip)normal=-normal;
            foreach(var p in points){vertices.Add(p);normals.Add(normal);}
            for(int i=1;i<points.Length-1;i++)
            {triangles.Add(start);triangles.Add(start+(flip?i+1:i));triangles.Add(start+(flip?i:i+1));}
        }
    }
    static Shape BevelCube()
    {
        var shape=new Shape();const float face=.5f, bevel=.41f;
        // Six broad faces, twelve bevels and eight clipped corners: real cubic volume.
        for(int axis=0;axis<3;axis++)for(int sign=-1;sign<=1;sign+=2)
        {
            Vector3 a=Vector3.zero,b=Vector3.zero,c=Vector3.zero;
            a[axis]=sign*face;b[(axis+1)%3]=bevel;c[(axis+2)%3]=bevel;
            shape.Face(a-b-c,a+b-c,a+b+c,a-b+c);
        }
        for(int axis=0;axis<3;axis++)for(int s=-1;s<=1;s+=2)for(int t=-1;t<=1;t+=2)
        {
            Vector3 a=Vector3.zero,b=Vector3.zero,along=Vector3.zero;
            a[(axis+1)%3]=s*face;a[(axis+2)%3]=t*bevel;
            b[(axis+1)%3]=s*bevel;b[(axis+2)%3]=t*face;along[axis]=bevel;
            shape.Face(a-along,a+along,b+along,b-along);
        }
        for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
            shape.Face(new Vector3(x*face,y*bevel,z*bevel),new Vector3(x*bevel,y*face,z*bevel),new Vector3(x*bevel,y*bevel,z*face));
        return shape;
    }
    static Shape Droplet()
    {
        var shape=new Shape();
        float[] heights={-.52f,-.43f,-.22f,.02f,.24f,.52f,.8f};
        float[] widths={.02f,.31f,.49f,.43f,.23f,.07f,.005f};
        for(int ring=0;ring<heights.Length-1;ring++)for(int side=0;side<12;side++)
        {
            float a=side*Mathf.PI/6,b=(side+1)*Mathf.PI/6;
            Vector3 Point(int row,float angle)=>new Vector3(Mathf.Cos(angle)*widths[row],heights[row],Mathf.Sin(angle)*widths[row]);
            Vector3 Normal(int row,float angle)
            {
                int before=Mathf.Max(0,row-1),after=Mathf.Min(heights.Length-1,row+1);
                float slope=(widths[after]-widths[before])/(heights[after]-heights[before]);
                return new Vector3(Mathf.Cos(angle),-slope,Mathf.Sin(angle)).normalized;
            }
            int first=shape.normals.Count;
            shape.Face(Point(ring,a),Point(ring,b),Point(ring+1,b),Point(ring+1,a));
            shape.normals[first]=Normal(ring,a);shape.normals[first+1]=Normal(ring,b);
            shape.normals[first+2]=Normal(ring+1,b);shape.normals[first+3]=Normal(ring+1,a);
        }
        return shape;
    }
    sealed class DriftMesh
    {
        readonly string name;
        readonly List<Vector3> vertices=new List<Vector3>(), normals=new List<Vector3>();
        readonly List<Vector4> fall=new List<Vector4>(), spin=new List<Vector4>();
        readonly List<Color> colors=new List<Color>();readonly List<int> triangles=new List<int>();
        public DriftMesh(string name){this.name=name;}
        public void Append(Shape shape,Vector3 scale,Vector4 motion,Vector4 rotation,float brightness)
        {
            int start=vertices.Count;
            for(int i=0;i<shape.vertices.Count;i++)
            {
                vertices.Add(Vector3.Scale(shape.vertices[i],scale));
                var n=shape.normals[i];normals.Add(new Vector3(n.x/scale.x,n.y/scale.y,n.z/scale.z).normalized);
                fall.Add(motion);spin.Add(rotation);colors.Add(new Color(brightness,brightness,brightness,1));
            }
            foreach(int index in shape.triangles)triangles.Add(start+index);
        }
        public Mesh Finish()
        {
            var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetColors(colors);
            mesh.SetUVs(0,fall);mesh.SetUVs(1,spin);mesh.SetTriangles(triangles,0);
            mesh.bounds=new Bounds(new Vector3(0,0,95),new Vector3(16,36,36));return mesh;
        }
    }
    void Update()
    {
        clock+=Time.deltaTime;
        foreach(var material in materials)material.SetFloat(Clock,clock);
    }
    void OnDestroy()
    {
        foreach(var material in materials)if(material!=null)Destroy(material);
        foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);
    }
}
