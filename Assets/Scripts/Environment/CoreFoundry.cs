using UnityEngine;

public static partial class DeepDistrictScenery
{
    static Color Paint(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f);
    static Color Lit(Color c, float value) => new Color(c.r * value, c.g * value, c.b * value);
    static Vector2 P(float x, float y) => new Vector2(x, y);
    static Vector2 Polar(float a) => new Vector2(Mathf.Cos(a), Mathf.Sin(a));

    // The bevel is visible from directly above. Its facets share one district light direction.
    static void Casting(SolidMesh b, Vector2 centre, Vector2[] rim, float z, float bevel, Color color)
    {
        for (int i = 0; i < rim.Length; i++)
        {
            Vector2 a = rim[i], d = rim[(i + 1) % rim.Length];
            Vector2 n = ((a + d) * .5f - centre).normalized;
            b.Face(Lit(color, .72f + .27f * Vector2.Dot(n,P(-.60f,.80f))), z,
                a,d,Vector2.Lerp(d,centre,bevel),Vector2.Lerp(a,centre,bevel));
        }
        var lid = new Vector2[rim.Length];
        for (int i = 0; i < lid.Length; i++) lid[i] = Vector2.Lerp(rim[i],centre,bevel);
        b.Face(color,z+.012f,lid);
    }

    static void Rib(SolidMesh b, Vector2 a, Vector2 d, float width, float z, Color c)
    {
        var n = Vector2.Perpendicular((d-a).normalized)*width*.5f;
        b.Face(c,z,a-n,d-n,d+n,a+n);
    }

    static void Pipe(SolidMesh b, Vector2 a, Vector2 d, float r, float z, Color c)
    {
        Vector2 n = Vector2.Perpendicular((d-a).normalized);
        float light = Vector2.Dot(n,P(-.8f,.6f));
        for (int k=0;k<8;k++)
        {
            float theta = Mathf.PI*(k+.5f)/8;
            Vector2 left = n * (Mathf.Cos(k*Mathf.PI/8)*r);
            Vector2 right = n * (Mathf.Cos((k+1)*Mathf.PI/8)*r);
            float diffuse = .47f + .39f*Mathf.Sin(theta) + light*.24f*Mathf.Cos(theta);
            float spec = Mathf.Pow(Mathf.Max(0,Mathf.Cos(theta-(1.57f-light*.66f))),16);
            b.Face(Lit(c,diffuse+spec*.26f), z+.01f*Mathf.Sin(theta),a+left,d+left,d+right,a+right);
        }
    }

    static void Band(SolidMesh b, Vector2 p, Vector2 radii, float inside, float a, float d, float z, Color c, int pieces)
    {
        for(int i=0;i<pieces;i++)
        {
            Vector2 u = Vector2.Scale(Polar(Mathf.Lerp(a,d,i/(float)pieces)),radii);
            Vector2 v = Vector2.Scale(Polar(Mathf.Lerp(a,d,(i+1f)/pieces)),radii);
            b.Face(c,z,p+u,p+v,p+v*inside,p+u*inside);
        }
    }

    static readonly Color Porcelain=Paint(0xc4c3b6), Pearl=Paint(0xd9d8c9), Alloy=Paint(0x828e8e),
        Gunmetal=Paint(0x465461), Socket=Paint(0x253540), Insulator=Paint(0xbd621f);

    static void Core(SolidMesh b,float edge,int index)
    {
        b.Rect(-4.1f,-edge,4.1f,edge,.015f,Alloy);
        int room = index;
        for(int s=-1;s<=1;s+=2)
        {
            for(int j=0;j<4;j++)
            {
                float y=-edge+2.5f+5*j;
                // Interlocking chamfered floor armour, rotated scars and recessed inspection slots.
                var rim=new[]{P(s*.05f,y-2.47f),P(s*2.53f,y-2.47f),P(s*3.95f,y-1.83f),
                    P(s*3.95f,y+2.46f),P(s*1.05f,y+2.46f),P(s*.05f,y+1.89f)};
                Casting(b,P(s*2,y),rim,.055f,.036f,Lit(Porcelain,.95f+.025f*((j+index)%3)));
                for(int l=0;l<3;l++)
                    Rib(b,P(s*(.38f+.14f*l),y-2.29f),P(s*(.51f+.14f*l),y-2.05f),.054f,.11f,Alloy);
                Rib(b,P(s*1.17f,y+2.21f),P(s*2.58f,y+2.21f),.045f,.11f,Alloy);
                if ((j+index+(s>0?1:0))%2==0)
                {
                    Vector2 panel=P(s*1.66f,y+.65f);
                    Vector2[] lid={panel+P(-.56f,-.55f),panel+P(.55f,-.55f),panel+P(.55f,.31f),
                        panel+P(.31f,.55f),panel+P(-.56f,.55f)};
                    Casting(b,panel,lid,.14f,.075f,Alloy);
                    for(int l=0;l<5;l++)
                    {
                        float v=panel.y-.32f+l*.14f;
                        Rib(b,P(panel.x-.33f,v),P(panel.x+.32f,v+.08f),.062f,.18f,Gunmetal);
                    }
                    for(int l=-1;l<=1;l+=2)
                        Band(b,panel+P(l*.41f,-.42f),P(.039f,.039f),0,0,Mathf.PI*2,.18f,Pearl,6);
                }
                b.Rect(s*2.11f-.28f,y-2.26f,s*2.11f+.28f,y-2.18f,.11f,Insulator);
            }
            float rail=s*4.2f;
            b.Rect(rail-.43f,-edge,rail+.43f,edge,.17f,Socket);
            for(int i=0;i<10;i++)
            {
                float y=-edge+1+2*i;
                b.Block(rail,y,.82f,.71f,.22f,Gunmetal);
                b.Block(rail,y,.70f,.32f,.24f,Alloy);
                b.Block(s*4.94f,y,1.1f,1.88f,.38f,Porcelain);
            }
            Pipe(b,P(rail-.13f,-edge),P(rail-.13f,edge),.089f,.57f,Pearl);
            Pipe(b,P(rail+.13f,-edge),P(rail+.13f,edge),.089f,.57f,Pearl);
            b.Rect(rail-.031f,-edge,rail+.031f,edge,.54f,Insulator);
            float x=s*3.45f;
            // Three individually insulated lines turn inward to seat in each transformer.
            for(int k=0;k<3;k++)
            {
                float strand=x-s*k*.14f;
                Pipe(b,P(strand,-edge),P(strand,-5.18f),.061f,.27f,k==1?Insulator:Gunmetal);
                Pipe(b,P(strand,-5.18f),P(strand-s*.65f,-4.31f),.061f,.27f,k==1?Insulator:Gunmetal);
                Pipe(b,P(strand-s*.65f,-4.31f),P(strand-s*.65f,4.31f),.061f,.27f,k==1?Insulator:Gunmetal);
                Pipe(b,P(strand-s*.65f,4.31f),P(strand,5.18f),.061f,.27f,k==1?Insulator:Gunmetal);
                Pipe(b,P(strand,5.18f),P(strand,edge),.061f,.27f,k==1?Insulator:Gunmetal);
            }
            for(int j=-1;j<=1;j+=2)
            {
                Vector2 c=P(s*3.32f,j*6.4f);
                Turbine(b,c,.78f,.78f,.63f);
            }
        }
        if(room%2==0)
        {
            // A deep direct-drive generator under a divided ceramic retaining ring.
            Turbine(b,P(room==0?-.74f:.74f,-.2f),2.32f,2.68f,.64f);
        }
        else
        {
            // Linear accelerator. Orange winding packs sit inside a long open inspection bay.
            float x=room==1?-.68f:.68f;
            var c=P(x,0);
            Vector2[] shell={c+P(-1.24f,-2.70f),c+P(-.76f,-3.26f),c+P(.94f,-3.04f),
                c+P(1.27f,-2.52f),c+P(1.27f,2.55f),c+P(.75f,3.18f),c+P(-.92f,3.02f),c+P(-1.24f,2.52f)};
            Casting(b,c,shell,.64f,.16f,Gunmetal);
            b.Rect(x-.84f,-2.64f,x+.84f,2.62f,.69f,Socket);
            for(int i=-7;i<=7;i++)
            {
                float y=i*.33f;
                Pipe(b,P(x-.68f,y-.13f),P(x+.67f,y+.13f),.12f,.75f,i%4==0?Porcelain:Insulator);
            }
            Pipe(b,P(x,-2.86f),P(x,2.86f),.21f,.81f,Alloy);
            for(int s=-1;s<=1;s+=2)
                for(int i=-1;i<=1;i++)
                    b.Block(x+s*.99f,i*1.69f,.39f,1.54f,.88f,Pearl);
        }
    }

    static void Turbine(SolidMesh b,Vector2 p,float rx,float ry,float z)
    {
        var radius=P(rx,ry);
        Band(b,p,radius,0,0,Mathf.PI*2,z,Socket,40);
        b.Rotor=new Vector4(p.x,p.y,rx/ry,p.x<0?.62f:-.49f);
        for(int i=0;i<16;i++)
        {
            float a=i*Mathf.PI/8;
            Vector2 At(float f,float angle)=>p+Vector2.Scale(Polar(angle),radius)*f;
            b.Face(i%2==0?Gunmetal:Alloy,z+.035f,At(.19f,a),At(.80f,a+.22f),At(.76f,a+.45f),At(.31f,a+.32f));
            b.Face(Alloy,z+.04f,At(.19f,a),At(.80f,a+.22f),At(.80f,a+.25f),At(.21f,a+.045f));
        }
        b.Rotor=Vector4.zero;
        for(int j=0;j<8;j++)
        {
            float a=j*Mathf.PI/4;
            Band(b,p,radius,.81f,a+.018f,a+.77f,z+.08f,j%2==0?Porcelain:Pearl,5);
            Band(b,p,radius*.835f,.95f,a+.022f,a+.762f,z+.09f,Alloy,5);
            if(j==1||j==5) Band(b,p,radius*.96f,.95f,a+.14f,a+.55f,z+.10f,Insulator,4);
        }
        Band(b,p,radius*.266f,0,0,Mathf.PI*2,z+.12f,Alloy,16);
        Band(b,p+P(-.025f,.03f),radius*.16f,0,0,Mathf.PI*2,z+.13f,Porcelain,12);
    }
}
