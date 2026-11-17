using UnityEngine;

public static partial class DeepDistrictScenery
{
    static readonly Color Joint=Paint(0x313438), Limestone=Paint(0x817c67),
        FiredBrick=Paint(0x786052), Verdigris=Paint(0x557773), Bronze=Paint(0x986b4d),
        DrainIron=Paint(0x394850), Lamp=Paint(0xd3a064);

    static void Sewer(SolidMesh b,float edge,int index,System.Random random)
    {
        // Each embankment projects into the canal at the sluices, then recedes into shadow.
        for(int side=-1;side<=1;side+=2)
        {
            float Bank(float y) => 3.63f-.30f*Mathf.Cos((y+edge)*Mathf.PI/5)-
                .53f*Mathf.Exp(-Mathf.Pow((Mathf.Abs(y)-5)*.85f,2));
            const int rows=40;
            for(int j=0;j<rows;j++)
            {
                float y=-edge+j*edge*2/rows, y1=y+edge*2/rows;
                float x=Bank(y),x1=Bank(y1);
                Vector2 V(float u,float v) => P(side*u,v);
                b.Face(Joint,.22f,V(x-.20f,y),V(9,y),V(9,y1),V(x1-.20f,y1));
                b.Face(Paint(0x34484a),.25f,V(x-.24f,y),V(x-.08f,y),V(x1-.08f,y1),V(x1-.24f,y1));
                b.Face(Lit(Limestone,.76f+.20f*(float)random.NextDouble()),.28f,
                    V(x-.10f,y+.015f),V(x+.16f,y+.015f),V(x1+.16f,y1-.014f),V(x1-.10f,y1-.014f));
                for(int k=0;k<6;k++)
                {
                    float from=.19f+k*.51f+(j%2)*.25f, to=from+.48f;
                    if(k==0) from=.19f;
                    Color brick=Color.Lerp(FiredBrick,Limestone,(float)random.NextDouble()*.65f);
                    b.Face(Lit(brick,.77f+.22f*(float)random.NextDouble()),.25f,
                        V(x+from,y+.016f),V(x+to,y+.016f),V(x1+to,y1-.016f),V(x1+from,y1-.016f));
                }
            }
            float pipe=side*4.55f;
            Pipe(b,P(pipe,-edge),P(pipe,edge),.20f,.46f,Verdigris);
            Pipe(b,P(pipe+side*.31f,-edge),P(pipe+side*.31f,edge),.085f,.46f,Bronze);
            for(int j=0;j<8;j++)
            {
                float y=-edge+(j+.5f)*2.5f;
                Pipe(b,P(pipe,y-.07f),P(pipe,y+.07f),.24f,.51f,DrainIron);
            }
            Culvert(b,P(side*3.83f,side*4.85f),side);
            if((index+(side>0?1:0))%2==0)
                Pump(b,P(side*3.60f,-side*3.75f));
            else
            {
                // Five descending wet steps down to the water line.
                for(int j=0;j<5;j++)
                {
                    float x=side*(2.69f+j*.22f);
                    b.Block(x,-side*3.75f,.25f,1.62f,.15f+j*.051f,Lit(Limestone,.61f+j*.072f));
                }
            }
        }
        if(index==3)
            Gallery(b,-.35f);
        if(index%2==0)
        {
            // A single corroded siphon spans the water with its hangers and flanged joints.
            var a=P(-4.1f,-.23f); var d=P(4.21f,.74f);
            Rib(b,a+P(0,-.38f),d+P(0,-.38f),.58f,.04f,Paint(0x203739));
            Pipe(b,a,d,.227f,.83f,Verdigris);
            Pipe(b,a+P(0,.38f),d+P(0,.38f),.072f,.85f,Bronze);
            for(int k=-3;k<=3;k++)
            {
                Vector2 c=Vector2.Lerp(a,d,(k+4)/8f);
                Pipe(b,c-P(.063f,.008f),c+P(.063f,.008f),.275f,.91f,Bronze);
                Rib(b,c+P(0,.12f),c+P(-.04f,.45f),.073f,.89f,DrainIron);
            }
        }
    }

    static void Gallery(SolidMesh b,float y)
    {
        // A maintenance crossing. Water remains visible through the foot grating and trusses.
        Rib(b,P(-3.72f,y-.37f),P(3.72f,y-.37f),.77f,.043f,Paint(0x23373a));
        for(int k=-17;k<=17;k++)
        {
            float x=k*.20f;
            Rib(b,P(x,y-.32f),P(x+.10f,y+.29f),.082f,.74f,Lit(DrainIron,1.34f));
        }
        for(int s=-1;s<=1;s+=2)
        {
            float v=y+s*.36f;
            Pipe(b,P(-3.87f,v),P(3.87f,v),.082f,.77f,Verdigris);
            Pipe(b,P(-3.87f,v+.23f),P(3.87f,v+.23f),.052f,.87f,Bronze);
            for(int i=-6;i<=6;i++)
            {
                float x=i*.55f;
                Rib(b,P(x,v),P(x+.27f,v+.23f),.042f,.84f,Bronze);
                Rib(b,P(x+.27f,v+.23f),P(x+.55f,v),.042f,.84f,Bronze);
            }
            b.Block(s*3.65f,y,.87f,1.36f,.56f,Limestone);
            b.Block(s*3.65f,y+.46f,.79f,.33f,.80f,FiredBrick);
            b.Block(s*3.65f,y+.46f,.86f,.32f,.91f,Limestone);
        }
    }

    static void Culvert(SolidMesh b,Vector2 p,int side)
    {
        // Recess the mouth under an arch of wedge-shaped voussoirs and a projecting keystone.
        Vector2 rad=P(1.05f,1.23f);
        Band(b,p,rad,0,0,Mathf.PI*2,.51f,Paint(0x202e34),32);
        for(int j=-3;j<=3;j++)
        {
            float x=j*.195f, half=.83f*Mathf.Sqrt(1-x*x/.65f);
            Pipe(b,p+P(x,-half),p+P(x,half),.037f,.56f,DrainIron);
        }
        for(int k=0;k<15;k++)
        {
            float a=k*Mathf.PI*2/15,d=(k+1)*Mathf.PI*2/15;
            Color stone=Lit(Limestone,.82f+.20f*Mathf.Cos(a-2.15f));
            Vector2 size=k==3||k==4?rad*1.055f:rad;
            Band(b,p,size,.72f,a+.017f,d-.017f,.63f,stone,3);
            Band(b,p,size,.95f,a+.017f,d-.017f,.65f,Lit(stone,1.14f),3);
            Band(b,p,size*.76f,.93f,a+.017f,d-.017f,.65f,Lit(stone,.59f),3);
        }
        Vector2 spout=p+P(-side*.78f,-.46f);
        Pipe(b,spout,spout+P(-side*.48f,0),.205f,.77f,Verdigris);
        Pipe(b,spout+P(-side*.48f,0),spout+P(-side*.60f,0),.24f,.79f,Bronze);
        var lamp=p+P(-side*.72f,.89f);
        Band(b,lamp,P(.19f,.16f),0,0,Mathf.PI*2,.73f,DrainIron,10);
        Band(b,lamp,P(.105f,.09f),0,0,Mathf.PI*2,.76f,Lamp,8);
    }

    static void Pump(SolidMesh b,Vector2 p)
    {
        Band(b,p,P(.91f,1.23f),0,0,2*Mathf.PI,.40f,Joint,24);
        Band(b,p,P(.80f,1.12f),0,0,2*Mathf.PI,.48f,Verdigris,24);
        Band(b,p,P(.66f,.94f),0,0,2*Mathf.PI,.53f,DrainIron,24);
        for(int i=0;i<7;i++)
        {
            Vector2 v=Polar(i*Mathf.PI*2/7);
            Rib(b,p,p+Vector2.Scale(v,P(.67f,.81f)),.076f,.65f,Bronze);
            Band(b,p+Vector2.Scale(v,P(.74f,1.01f)),P(.053f,.053f),0,0,Mathf.PI*2,.59f,Limestone,6);
        }
        Band(b,p,P(.71f,.87f),.87f,0,Mathf.PI*2,.67f,Bronze,32);
        Band(b,p,P(.20f,.23f),0,0,Mathf.PI*2,.69f,Limestone,10);
    }
}
