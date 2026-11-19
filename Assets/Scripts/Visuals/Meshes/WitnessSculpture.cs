using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// A chiseled robe folded around a single eye. The drapery, hood lining and
// the eye socket share control vertices. Facets split only for flat shading.
public sealed class WitnessSculpture : MonoBehaviour
{
    static readonly Color Basalt = new Color(.092f, .084f, .122f);
    static readonly Color Mantle = new Color(.15f, .147f, .19f);
    static readonly Color Ridge = new Color(.225f, .218f, .27f);
    static readonly Color Lip = new Color(.32f, .30f, .36f);
    static readonly Color Garnet = new Color(.56f, .06f, .135f);
    static Material material;
    static Carving carving;
    static Mesh body;
    public static Material Obsidian => material != null ? material : material = new Material(Resources.Load<Shader>("WitnessObsidian"));
    static Carving Sculpture => carving ??= new Carving();
    public static Mesh Body => body != null ? body : body = Sculpture.MakeMesh();

    // Each section gives the height, half-width, depth and lateral offset of
    // the cloth. The shoulder mantle folds back under itself into the robe.
    static readonly Vector4[] Sections = {
        new(1.88f,.12f,.19f,-.38f), new(1.62f,.45f,.49f,-.20f),
        new(1.19f,.72f,.69f,-.035f), new(.81f,.81f,.73f,0),
        new(.44f,.80f,.65f,0), new(.12f,.67f,.58f,0),
        new(-.13f,.49f,.45f,0), new(-.30f,.77f,.51f,0),
        new(-.51f,1.48f,.64f,.02f), new(-.85f,1.92f,.76f,.06f),
        new(-1.19f,2.03f,.78f,.10f), new(-1.16f,1.13f,.55f,0),
        new(-1.65f,1.00f,.58f,-.04f), new(-2.12f,.81f,.51f,-.04f),
        new(-2.55f,.85f,.55f,0), new(-3.02f,1.02f,.62f,.045f),
        new(-3.57f,1.22f,.69f,.12f), new(-4.06f,1.43f,.76f,.20f)
    };

    sealed class Carving
    {
        const int Sides = 32, HoodTop = 2, HoodBottom = 5, Cheek = 5;
        readonly List<Vector3> points = new List<Vector3>();
        readonly List<int> corners = new List<int>();
        readonly List<Color> colors = new List<Color>();
        readonly int[,] rings = new int[Sections.Length, Sides];
        public readonly Vector3[] Rest;
        public readonly int[] Corners;

        public Carving()
        {
            for (int row=0; row<Sections.Length; row++)
                for (int k=0; k<Sides; k++)
                    rings[row,k] = Add(Drape(row,k));

            for (int row=0; row<Sections.Length-1; row++)
                for (int k=-Sides/2; k<Sides/2; k++)
                {
                    if (row==9 || row>=HoodTop && row<HoodBottom && k>=-Cheek && k<Cheek) continue;
                    Color stone = Panel(row,k);
                    if (row==7 && k==2)
                        Clasp(Node(row,k),Node(row,k+1),Node(row+1,k+1),Node(row+1,k));
                    else
                        Quad(Node(row,k),Node(row,k+1),Node(row+1,k+1),Node(row+1,k),stone);
                }
            int peak = Add(new Vector3(-.48f,2.01f,.24f));
            int underside = Add(new Vector3(.16f,-3.97f,.06f));
            for (int k=0; k<Sides; k++)
            {
                Tri(Node(0,k+1),Node(0,k),peak,Mantle);
                Tri(Node(Sections.Length-1,k),Node(Sections.Length-1,k+1),underside,Basalt);
            }
            MantleHem();
            Hood();
            Rest = points.ToArray();
            Corners = corners.ToArray();
        }
        static Vector3 Drape(int row,int k)
        {
            var section = Sections[row];
            float angle = k*2*Mathf.PI/Sides;
            float skirt = Mathf.InverseLerp(-1.25f,-4.1f,section.x);
            float twist = row<7 ? 0 : Mathf.Lerp(-.84f,.33f,Mathf.Clamp01((row-7)/3f)) + .25f*skirt;
            float direction = angle+twist;
            float side = Mathf.Sin(direction), front = Mathf.Cos(direction);
            float fold = Mathf.Cos(angle*7f + .65f*Mathf.Sin(angle*2f));
            float depth = section.z;
            float y = section.x, x = section.w, z = 0;
            if (row<=6)
            {
                float arch = Mathf.Max(0,front)*(1-Mathf.Abs(side));
                if (row==HoodTop) y += .30f*arch;
                if (row>=HoodBottom) y -= .32f*arch;
                z = .22f*Mathf.InverseLerp(.8f,1.88f,section.x);
            }
            else
            {
                float mantle = row==11 ? .36f : Mathf.Clamp01((row-7)/3f);
                if (row<=11)
                    y -= mantle*(.81f*side*side + .36f*side + .12f*front);
                else
                    y -= .16f*side;
                float pleat = .045f + .15f*skirt;
                depth += pleat*fold;
                if (k==2 || k==5) depth += .105f;
                x += side*pleat*fold;
                if (row==Sections.Length-1)
                    y -= .24f*(1+fold)*.5f + .30f*side;
            }
            return new Vector3(x+section.y*side,y,z-depth*front);
        }
        static Color Panel(int row,int k)
        {
            if (row<7) return row==5 ? Basalt : Mantle;
            // A narrow selvedge follows the diagonal fall from shoulder to hem.
            if (k==2) return Lip;
            if (k==1 || k==3 || row==10) return Basalt;
            return k%5==0 ? Ridge : Mantle;
        }
        int Node(int row,int k) => rings[row,(k+Sides)%Sides];
        int Add(Vector3 p) { points.Add(p); return points.Count-1; }
        void Tri(int a,int b,int c,Color color)
        {
            corners.Add(a); corners.Add(b); corners.Add(c);
            colors.Add(color); colors.Add(color); colors.Add(color);
        }
        void Quad(int a,int b,int c,int d,Color color)
        {
            Tri(a,b,c,color);
            Tri(a,c,d,color);
        }
        void MantleHem()
        {
            var lip = new int[Sides];
            for (int k=0; k<Sides; k++)
                lip[k] = Add(Vector3.Lerp(points[Node(9,k)], points[Node(10,k)], .87f));
            for (int k=0; k<Sides; k++)
            {
                int j = (k+1)%Sides;
                Quad(Node(9,k),Node(9,j),lip[j],lip[k],Panel(9,k>Sides/2 ? k-Sides : k));
                Quad(lip[k],lip[j],Node(10,j),Node(10,k),Ridge);
            }
        }
        void Clasp(int a,int b,int c,int d)
        {
            // Four folded corners seat a lozenge into the crossed stole.
            var cell = new[]{a,b,c,d};
            Vector3 middle = (points[a]+points[b]+points[c]+points[d])*.25f;
            var bezel = new int[4];
            for (int k=0; k<4; k++)
            {
                Vector3 edge = (points[cell[k]]+points[cell[(k+1)%4]])*.5f;
                bezel[k] = Add(Vector3.Lerp(middle,edge,.88f)+Vector3.back*.055f);
            }
            int gem = Add(middle+Vector3.back*.14f);
            for (int k=0; k<4; k++)
            {
                int j=(k+1)%4;
                Tri(cell[k],cell[j],bezel[k],Mantle);
                Tri(cell[j],bezel[j],bezel[k],Basalt);
                Tri(bezel[k],bezel[j],gem,k<2 ? Lip : Ridge);
            }
        }
        void Hood()
        {
            var rim = new List<int>();
            for (int k=-Cheek; k<=Cheek; k++) rim.Add(Node(HoodTop,k));
            for (int row=HoodTop+1; row<=HoodBottom; row++) rim.Add(Node(row,Cheek));
            for (int k=Cheek-1; k>=-Cheek; k--) rim.Add(Node(HoodBottom,k));
            for (int row=HoodBottom-1; row>HoodTop; row--) rim.Add(Node(row,-Cheek));

            var opening = rim.ToArray();
            var ring = opening;
            // Roll the lip forward, hollow the cowl, then cut the long face and
            // its eye out of the same surface. The mask stands inside the hood.
            void Inset(float width,float height,float centreY,float z,Color pigment)
            {
                var next = new int[ring.Length];
                for (int i=0; i<next.Length; i++)
                {
                    var p = points[opening[i]];
                    float arch = height<.2f ? Mathf.Lerp(1,.10f,Mathf.Clamp01(Mathf.Abs(p.x)/.64f)) : 1;
                    next[i] = Add(new Vector3(p.x*width,centreY+(p.y-.65f)*height*arch,z+.18f*Mathf.Abs(p.x)));
                }
                for (int i=0; i<next.Length; i++)
                {
                    int j=(i+1)%next.Length;
                    Quad(ring[i],ring[j],next[j],next[i],pigment);
                }
                ring=next;
            }
            Inset(.93f,.95f,.65f,-.79f,Ridge);
            Inset(.82f,.83f,.67f,-.30f,Basalt);
            Inset(.63f,.69f,.62f,-.43f,new Color(.065f,.057f,.09f));
            Inset(.47f,.47f,.65f,-.59f,Ridge);
            Inset(.65f,.19f,.76f,-.70f,Mantle);
            Inset(.56f,.155f,.76f,-.65f,Garnet);
            Inset(.095f,.13f,.76f,-.71f,new Color(.90f,.23f,.36f));
            int pupil=Add(new Vector3(0,.76f,-.73f));
            for (int i=0; i<ring.Length; i++) Tri(ring[i],ring[(i+1)%ring.Length],pupil,Basalt);
        }
        public Mesh MakeMesh()
        {
            var mesh = new Mesh { name = "The Witness / veiled oracle" };
            var verts = new Vector3[Corners.Length];
            var indices = new int[verts.Length];
            for (int i=0; i<verts.Length; i++) { verts[i] = Rest[Corners[i]]; indices[i] = i; }
            mesh.vertices = verts;
            mesh.SetTriangles(indices,0);
            mesh.SetColors(colors);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }

    Mesh skin;
    MeshRenderer aura;
    Material auraMaterial;
    RenderTexture silhouette;
    CommandBuffer drawSilhouette;
    Vector3[] nodes, vertices, normals;
    struct Flex { public float shoulder, tip, hem, hood, iris; }
    Flex[] flex;
    public void Assemble()
    {
        skin = Instantiate(Body);
        skin.MarkDynamic();
        GetComponent<MeshFilter>().sharedMesh = skin;
        var rest = Sculpture.Rest;
        nodes = new Vector3[rest.Length];
        vertices = new Vector3[Sculpture.Corners.Length];
        normals = new Vector3[vertices.Length];
        flex = new Flex[rest.Length];
        for (int i=0; i<rest.Length; i++)
        {
            var p = rest[i];
            float span = Mathf.Abs(p.x);
            flex[i] = new Flex {
                shoulder = Mathf.SmoothStep(0,1,Mathf.InverseLerp(.55f,1.7f,span)) * Mathf.InverseLerp(-3f,-1.9f,p.y),
                tip = Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.15f,2.18f,span)),
                hem = Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.72f,-4.44f,p.y)),
                hood = Mathf.SmoothStep(0,1,Mathf.InverseLerp(.50f,1.32f,p.y)) * Mathf.Clamp01(1-span),
                iris = Mathf.InverseLerp(-.59f,-.73f,p.z) * Mathf.Clamp01(1-span/.28f) * Mathf.Clamp01(1-Mathf.Abs(p.y-.76f)/.26f)
            };
        }
        skin.bounds = new Bounds(new Vector3(0,-.9f,0), new Vector3(6.6f,7.8f,4));

        var veil = new GameObject("Witness penumbra", typeof(MeshFilter), typeof(MeshRenderer));
        veil.transform.SetParent(transform, false);
        veil.transform.localPosition = new Vector3(0, -1.1f, .95f);
        veil.transform.localScale = new Vector3(6.4f, 8f, 1);
        veil.GetComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
        aura = veil.GetComponent<MeshRenderer>();
        aura.sortingLayerID = GetComponent<MeshRenderer>().sortingLayerID;
        aura.sortingOrder = GetComponent<MeshRenderer>().sortingOrder - 1;
        auraMaterial = new Material(Resources.Load<Shader>("WitnessPenumbra"));
        aura.sharedMaterial = auraMaterial;

        silhouette = new RenderTexture(64, 80, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
        {
            name = "Witness silhouette", useMipMap = true, autoGenerateMips = false,
            filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp
        };
        silhouette.Create();
        auraMaterial.SetTexture("_Silhouette", silhouette);
        var frame = veil.transform;
        auraMaterial.SetVector("_MaskFrame", new Vector4(frame.localPosition.x, frame.localPosition.y,
            2 / frame.localScale.x, 2 / frame.localScale.y));
        drawSilhouette = new CommandBuffer { name = "Witness silhouette" };
        drawSilhouette.SetRenderTarget(silhouette);
        drawSilhouette.ClearRenderTarget(false, true, Color.clear);
        drawSilhouette.DrawMesh(skin, Matrix4x4.identity, auraMaterial, 0, auraMaterial.FindPass("Silhouette"));
        drawSilhouette.GenerateMips(silhouette);
        Graphics.ExecuteCommandBuffer(drawSilhouette);
    }
    public void Pose(float t, int phase, float strike)
    {
        float beat = t * (1.16f + .09f * phase);
        float breath = Mathf.Sin(beat);
        float shoulder = .23f * breath + (phase-1)*.075f + strike*.23f;
        float sweep = .42f * Mathf.Sin(beat-.85f) + strike*.38f;
        float twist = .21f * Mathf.Cos(t*.87f);
        float tail = .32f*Mathf.Sin(beat-.73f);
        float tailLift = .22f * Mathf.Cos(beat);
        float glance = .068f * Mathf.Sin(t * .92f);
        var rest = Sculpture.Rest;
        for (int i=0; i<rest.Length; i++)
        {
            var p = rest[i];
            var w = flex[i];
            float h = Mathf.Sign(p.x);
            p.x += h * (.12f * w.shoulder * breath - .27f * w.tip * (sweep + .32f)) + tail*w.hem + glance*w.iris;
            p.y += w.shoulder*shoulder + w.tip*sweep + .12f*w.hood*(breath + strike);
            p.z += w.shoulder*(.28f*breath + h*twist) + w.tip*(.42f*sweep - h*twist) + tailLift*w.hem;
            nodes[i] = p;
        }
        var corners = Sculpture.Corners;
        for (int i=0; i<corners.Length; i+=3)
        {
            var a = nodes[corners[i]]; var b = nodes[corners[i+1]]; var c = nodes[corners[i+2]];
            vertices[i] = a; vertices[i+1] = b; vertices[i+2] = c;
            normals[i] = normals[i+1] = normals[i+2] = Vector3.Cross(b-a,c-a).normalized;
        }
        skin.SetVertices(vertices,0,vertices.Length,UnityEngine.Rendering.MeshUpdateFlags.DontRecalculateBounds);
        skin.SetNormals(normals);
        Graphics.ExecuteCommandBuffer(drawSilhouette);
    }
    public void ExtinguishAura() => aura.gameObject.SetActive(false);
    void OnDestroy()
    {
        drawSilhouette?.Release();
        if (silhouette != null) Destroy(silhouette);
        if (skin != null) Destroy(skin);
        if (auraMaterial != null) Destroy(auraMaterial);
    }
}
