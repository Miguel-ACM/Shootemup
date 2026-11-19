using System.Collections.Generic;
using UnityEngine;

// A chiseled shell folded around a single eye. Both skins, the cutout walls and
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

    // Sections run from the axial keel to the end of each horn. Each column
    // rakes from its crown position to its hem position. Paired ridges carry
    // the shape of the eye out across the shoulders.
    static readonly float[] CrownX = { 0, .18f, .45f, .72f, .96f, 1.18f, 1.44f, 1.70f, 1.99f, 2.24f, 2.46f };
    static readonly float[] HemX = { 0, .20f, .39f, .62f, .87f, 1.11f, 1.47f, 1.78f, 1.98f, 2.20f, 2.46f };
    static readonly float[] Depth = { .58f, .54f, .46f, .35f, .27f, .23f, .20f, .17f, .145f, .10f, .032f };
    static readonly float[,] Height = {
        { 1.05f, 1.32f, .99f, .81f, .89f, 1.06f, 1.25f, 1.36f, 1.43f, 1.63f, 1.96f },
        { .81f, .84f, .75f, .64f, .66f, .82f, 1.03f, 1.18f, 1.28f, 1.50f, 1.94f },
        { .64f, .61f, .57f, .53f, .49f, .61f, .84f, 1.02f, 1.13f, 1.40f, 1.92f },
        { .51f, .49f, .36f, .32f, .29f, .31f, .45f, .66f, .89f, 1.20f, 1.90f },
        { .07f, .10f, .24f, .13f, .09f, .10f, .18f, .38f, .69f, .99f, 1.88f },
        { -.20f, -.12f, -.01f, -.08f, -.17f, -.10f, .02f, .13f, .44f, .80f, 1.86f },
        { -.65f, -.52f, -.33f, -.29f, -.50f, -.38f, -.22f, -.20f, .09f, .66f, 1.84f },
        { -1.18f, -.95f, -.49f, -.53f, -.77f, -.61f, -.42f, -.43f, -.10f, .49f, 1.82f },
        { -2.05f, -1.54f, -.62f, -.82f, -1.24f, -.77f, -.57f, -.90f, -.35f, .30f, 1.80f }
    };
    static readonly float[] Relief = { .08f, .78f, .54f, 1.12f, .94f, 1.17f, .68f, .83f, .07f };

    sealed class Carving
    {
        readonly List<Vector3> points = new List<Vector3>();
        readonly List<int> corners = new List<int>();
        readonly List<Color> colors = new List<Color>();
        readonly int[,,] grid = new int[2, 21, 9];
        public readonly Vector3[] Rest;
        public readonly int[] Corners;
        public Carving()
        {
            for (int face = 0; face <= 1; face++)
                for (int col = -10; col <= 10; col++)
                    for (int row = 0; row < 9; row++)
                    {
                        int i = Mathf.Abs(col);
                        float v = row / 8f;
                        float x = Mathf.Lerp(CrownX[i], HemX[i], v) * System.Math.Sign(col);
                        float sweep = .13f * Mathf.Abs(x) - .11f * v;
                        float z = face == 0 ? -Relief[row] * Depth[i] : (.20f + .60f * Mathf.Sin(v * Mathf.PI)) * Depth[i];
                        grid[face, col + 10, row] = Add(new Vector3(x, Height[row, i], sweep + z));
                    }
            for (int col = -10; col < 10; col++)
                for (int row = 0; row < 8; row++)
                {
                    int span = col < 0 ? -col - 1 : col;
                    bool window = (span >= 3 && span < 8 && row == 2) || (span >= 5 && span < 9 && row == 5);
                    if (window) continue;
                    int a = Node(0,col,row), b = Node(0,col+1,row), c = Node(0,col+1,row+1), d = Node(0,col,row+1);
                    bool crownRoot = span == 1 && row == 0;
                    bool jawRoot = span == 2 && row == 5;
                    if (crownRoot || jawRoot)
                        Horn(a,b,c,d,col < 0 ? -1 : 1,crownRoot);
                    else if (!(span < 2 && row == 3))
                    {
                        // Broad planed panels, each with an off-centre crest and sloping sides.
                        bool arris = row == 0 || row == 4 || row == 6;
                        Color plane = arris ? Ridge : (row == 2 || row == 5 ? Basalt : Mantle);
                        if (span < 2 && row >= 5) plane = Mantle;
                        Facet(a,b,c,d,plane,span < 2 && row >= 5 ? .29f : arris ? .082f : .032f,span);
                    }
                    Quad(Node(1,col,row+1), Node(1,col+1,row+1), Node(1,col+1,row), Node(1,col,row), Basalt);
                }
            for (int col = -10; col < 10; col++)
            {
                Edge(col+1,0,col,0);
                Edge(col,8,col+1,8);
            }
            for (int row = 0; row < 8; row++)
            {
                Edge(-10,row,-10,row+1);
                Edge(10,row+1,10,row);
            }
            for (int h = -1; h <= 1; h += 2)
            {
                Window(h * 3, h * 8, 2);
                Window(h * 5, h * 9, 5);
            }
            Eye();
            Rest = points.ToArray();
            Corners = corners.ToArray();
        }
        int Add(Vector3 p) { points.Add(p); return points.Count - 1; }
        int Node(int face, int col, int row) => grid[face,col+10,row];
        void Tri(int a, int b, int c, Color color)
        {
            corners.Add(a); corners.Add(b); corners.Add(c);
            colors.Add(color); colors.Add(color); colors.Add(color);
        }
        void Quad(int a, int b, int c, int d, Color color)
        {
            Tri(a,b,c,color);
            Tri(a,c,d,color);
        }
        void Edge(int ax, int ay, int bx, int by) =>
            Quad(Node(0,ax,ay),Node(0,bx,by),Node(1,bx,by),Node(1,ax,ay),Mantle);

        void Facet(int a, int b, int c, int d, Color color, float rise, int span)
        {
            bool right = points[b].x > 0;
            var inner = Vector3.Lerp(points[right ? a : b], points[right ? d : c], .4f);
            var outer = Vector3.Lerp(points[right ? b : a], points[right ? c : d], .63f);
            int ridge = Add(Vector3.Lerp(inner, outer, .55f) + Vector3.back * rise * Depth[span]);
            Tri(a,b,ridge,color * 1.09f);
            Tri(b,c,ridge,color * (right ? .91f : 1.03f));
            Tri(c,d,ridge,color * .82f);
            Tri(d,a,ridge,color * (right ? 1.03f : .91f));
        }

        int[] Rim(int face, int start, int end, int row)
        {
            int count = end - start + 1;
            var ring = new int[count * 2];
            for (int i = 0; i < count; i++)
            {
                ring[i] = Node(face,start+i,row);
                ring[ring.Length-1-i] = Node(face,start+i,row+1);
            }
            return ring;
        }
        void Band(int[] outer, int[] inner, Color color)
        {
            for (int i = 0; i < outer.Length; i++)
            {
                int j = (i + 1) % outer.Length;
                Quad(outer[i],outer[j],inner[j],inner[i],color);
            }
        }
        // Extrude the original four-sided socket toward the viewer before it
        // bends. Each change in section carves another plane into the hook.
        void Horn(int a, int b, int c, int d, int side, bool crown)
        {
            int[] ring = {a,b,c,d};
            Vector3 root = (points[a]+points[b]+points[c]+points[d])*.25f;
            Vector3[] path = crown ? CrownHook : JawHook;
            Vector2[] sizes = crown ? CrownSection : JawSection;
            for (int j=0; j<path.Length-1; j++)
            {
                Vector3 p = path[j];
                Vector3 centre = root + new Vector3(side*p.x,p.y,p.z);
                Vector3 axis = (path[j+1] - (j==0 ? Vector3.zero : path[j-1])).normalized;
                axis.x *= side;
                Vector3 across = Vector3.Cross(axis, Vector3.back).normalized;
                Vector3 bevel = Vector3.Cross(axis, across).normalized;
                float width = sizes[j].x, depth = sizes[j].y;
                Vector3 u = across * width, v = bevel * depth;
                var next = new[]{Add(centre+u),Add(centre+v),Add(centre-u),Add(centre-v)};
                // Match the first cross-section to the nearest socket corners;
                // later sections inherit that ordering along the swept spine.
                if (j == 0)
                {
                    int start = 0;
                    float shortest = float.PositiveInfinity;
                    for (int shift=0; shift<4; shift++)
                    {
                        float length=0;
                        for (int k=0;k<4;k++) length+=(points[ring[(shift+k)%4]]-points[next[k]]).sqrMagnitude;
                        if (length < shortest) { shortest=length; start=shift; }
                    }
                    ring = new[]{ring[start],ring[(start+1)%4],ring[(start+2)%4],ring[(start+3)%4]};
                }
                for (int k=0; k<4; k++)
                    Quad(ring[k],ring[(k+1)%4],next[(k+1)%4],next[k],k==0 ? Ridge : k==3 ? Basalt : Mantle);
                ring = next;
            }
            Vector3 tip = path[path.Length-1];
            int end = Add(root+new Vector3(side*tip.x,tip.y,tip.z));
            for (int k=0;k<4;k++) Tri(ring[k],ring[(k+1)%4],end,k==0 ? Lip : Mantle);
        }
        static readonly Vector3[] CrownHook = {
            new(.025f,.055f,-.18f), new(.15f,.22f,-.33f), new(.24f,.48f,-.36f),
            new(.21f,.69f,-.29f), new(.09f,.86f,-.22f), new(-.15f,.98f,-.13f)
        };
        static readonly Vector2[] CrownSection = { new(.14f,.12f),new(.17f,.14f),new(.13f,.11f),new(.097f,.078f),new(.060f,.044f) };
        static readonly Vector3[] JawHook = {
            new(.055f,-.04f,-.21f),new(.30f,-.13f,-.34f),new(.57f,-.37f,-.37f),
            new(.68f,-.74f,-.32f),new(.61f,-1.10f,-.20f),new(.42f,-1.39f,-.12f),new(.15f,-1.60f,-.10f)
        };
        static readonly Vector2[] JawSection = {new(.16f,.12f),new(.23f,.18f),new(.24f,.21f),new(.17f,.16f),new(.13f,.09f),new(.075f,.045f)};
        // The aperture has a beveled reveal and a real back wall around its edge.
        void Window(int endA, int endB, int row)
        {
            int start = Mathf.Min(endA,endB), end = Mathf.Max(endA,endB);
            var front = Rim(0,start,end,row);
            var back = Rim(1,start,end,row);
            var reveal = new int[front.Length];
            var rear = new int[front.Length];
            int count = end-start+1;
            for (int i = 0; i < front.Length; i++)
            {
                int j = i < count ? i : front.Length-1-i;
                float t = j / (float)(count-1);
                // Pinch the slit to a narrow lancet at either end.
                float opening = .14f + .63f * Mathf.Sin(t * Mathf.PI);
                Vector3 centre = (points[front[j]] + points[front[front.Length-1-j]]) * .5f;
                Vector3 p = Vector3.Lerp(centre, points[front[i]], opening);
                p.x = Mathf.Lerp(points[front[count/2]].x, p.x, .93f);
                reveal[i] = Add(p + Vector3.forward * .025f);
                rear[i] = Add(new Vector3(p.x,p.y, points[back[i]].z-.013f));
            }
            Band(front,reveal,Ridge);
            Band(reveal,rear,Basalt);
            Band(rear,back,Mantle);
        }
        void Eye()
        {
            var outline = Rim(0,-2,2,3);
            int[] ring = outline;
            var centre = new Vector3(0,.29f,-.56f);
            // Cut the lid into the brow, recess the vitreous, then raise a long
            // garnet iris and a polished black pupil from that same opening.
            void Inset(float x, float y, float z, Color pigment)
            {
                var next = new int[ring.Length];
                for (int i = 0; i < next.Length; i++)
                {
                    Vector3 p = points[outline[i]] - centre;
                    next[i] = Add(new Vector3(p.x * x,centre.y+p.y*y,z + .08f * Mathf.Abs(p.x)));
                }
                Band(ring,next,pigment);
                ring = next;
            }
            Inset(.90f,.82f,-.69f,Lip);
            Inset(.82f,.72f,-.57f,Basalt);
            Inset(.46f,.72f,-.66f,new Color(.28f,.11f,.17f));
            Inset(.19f,.68f,-.73f,Garnet);
            Inset(.072f,.48f,-.75f,new Color(1,.28f,.36f));
            int pupil = Add(new Vector3(0,.29f,-.77f));
            for (int i=0; i<ring.Length; i++) Tri(ring[i],ring[(i+1)%ring.Length],pupil,Basalt);
        }
        public Mesh MakeMesh()
        {
            var mesh = new Mesh { name = "The Witness / carved obsidian mantle" };
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
    Vector3[] nodes, vertices, normals;
    struct Flex { public float shoulder, tip, hem, hood, jaw, iris; }
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
                shoulder = Mathf.SmoothStep(0,1,Mathf.InverseLerp(.4f,1.42f,span)),
                tip = Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.05f,2.46f,span)),
                hem = Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.35f,-1.94f,p.y)),
                hood = Mathf.SmoothStep(0,1,Mathf.InverseLerp(.50f,1.32f,p.y)) * Mathf.Clamp01(1-span),
                jaw = Mathf.InverseLerp(-.45f,-1.5f,p.y) * Mathf.InverseLerp(.34f,.83f,span),
                iris = Mathf.InverseLerp(-.59f,-.73f,p.z) * Mathf.Clamp01(1-span/.28f) * Mathf.Clamp01(1-Mathf.Abs(p.y-.29f)/.26f)
            };
        }
        skin.bounds = new Bounds(Vector3.zero, new Vector3(6.6f,6.5f,4));
    }
    public void Pose(float t, int phase, float strike)
    {
        float beat = t * (1.16f + .09f * phase);
        float breath = Mathf.Sin(beat);
        float shoulder = .23f * breath + (phase-1)*.075f + strike*.23f;
        float sweep = .42f * Mathf.Sin(beat-.85f) + strike*.38f;
        float twist = .21f * Mathf.Cos(t*.87f);
        float tail = .21f*Mathf.Sin(beat-.73f);
        float tailLift = .22f * Mathf.Cos(beat);
        float gape = .10f * (1 + breath) + .24f * strike;
        float glance = .068f * Mathf.Sin(t * .92f);
        var rest = Sculpture.Rest;
        for (int i=0; i<rest.Length; i++)
        {
            var p = rest[i];
            var w = flex[i];
            float h = Mathf.Sign(p.x);
            p.x += h * (.12f * w.shoulder * breath - .27f * w.tip * (sweep + .32f) + w.jaw*gape) + tail*w.hem + glance*w.iris;
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
    }
    void OnDestroy() { if (skin != null) Destroy(skin); }
}
