using UnityEngine;

public sealed partial class CandylandBackdrop
{
    static readonly Color Canal = new Color(.38f,.24f,.39f), Ripple = new Color(.60f,.40f,.53f),
        Bank = new Color(.46f,.22f,.32f), Leaf = new Color(.42f,.24f,.36f),
        RedLeaf = new Color(.72f,.34f,.44f), Amber = new Color(.79f,.52f,.36f);

    void Estate(Builder b, int section, float length, System.Random random)
    {
        float BankX(float y)
        {
            float a=Turn(section*length+y,length);
            return 6.46f+1.20f*Mathf.Sin(4*a+phase)+.51f*Mathf.Cos(7*a-phase);
        }
        // A narrow stepped bank and a ribbon of still rose water pass under each little bridge.
        b.Pose(Vector2.zero);
        const int steps=42;
        for(int i=0;i<steps;i++)
        {
            float ya=length*(i/(float)steps-.5f), yb=length*((i+1f)/steps-.5f);
            float a=BankX(ya), d=BankX(yb);
            void WaterStrip(float left,float right,float z,Color col) =>
                b.Face(col,new Vector3(a+left,ya,z),new Vector3(d+left,yb,z),new Vector3(d+right,yb,z),new Vector3(a+right,ya,z));
            WaterStrip(-1.07f,1.06f,.019f,Bank);
            WaterStrip(-.91f,.95f,.022f,Canal);
            WaterStrip(-.86f,-.81f,.024f,Ripple);
            WaterStrip(.95f,1.03f,.024f,Amber);
            if(i%7==1 || i%9==3)
                WaterStrip(-.48f+(i%3)*.25f,-.15f+(i%3)*.25f,.025f,Color.Lerp(Canal,Ripple,.58f));
        }

        float bridgeY=section%2==0?2.75f:-3.8f;
        b.Pose(new Vector2(BankX(bridgeY),bridgeY),section%2==0?-10:9);
        WaferBridge(b);
        // The mill road crosses the river; its far end curls into an orchard court.
        float laneX=Lane(Turn(section*length+bridgeY,length));
        var bend=new Vector2((laneX+BankX(bridgeY))*.5f,bridgeY-.80f);
        b.Pose(Vector2.zero);
        Walk(b,new Vector2(laneX,bridgeY-.35f),bend,new Vector2(BankX(bridgeY)-1.2f,bridgeY),.19f);
        Walk(b,new Vector2(BankX(bridgeY)+1.2f,bridgeY),new Vector2(BankX(bridgeY)+2.30f,bridgeY+.25f),
            new Vector2(BankX(bridgeY)+3.1f,bridgeY+1.1f),.16f);
        float mainY=section%2==0?-1.4f:1.6f, mainX=-6.85f+(float)random.NextDouble()*.60f;
        b.Pose(new Vector2(mainX,mainY),-11,1.04f);
        // A biscuit-paved forecourt gives the silhouette of each tall roof its own clear ground.
        b.Footpath(new Vector2(-2,0),new Vector2(.5f,-.85f),new Vector2(3.5f,-1.16f));
        b.Box(new Vector3(0,0,.048f),new Vector3(2.80f,2.23f,.085f),Bank);
        b.Box(new Vector3(0,0,.095f),new Vector3(2.66f,2.10f,.038f),Amber);
        if(section%2==0)
            Windmill(b);
        else
            Bakehouse(b);
        b.Pose(Vector2.zero);
        float laneY=mainY-1.55f;
        Walk(b,new Vector2(mainX,mainY-.88f),new Vector2(mainX+2.2f,laneY-.28f),
            new Vector2(Lane(Turn(section*length+laneY,length)),laneY),.22f);
        // Raised kitchen gardens occupy the two ends of the estate, away from the forecourt.
        for(int end=-1;end<=1;end+=2)
        {
            float y=end*(6.24f+.32f*(section%2));
            b.Pose(new Vector2(-7.39f,y),end*11+section*2,1.09f);
            GardenBeds(b,section+end);
        }
        // Pruned rows follow the outside banks. Gaps open onto the bridge and the mill yard.
        for(int i=0;i<9;i++)
        {
            float y=(i-4)*2.11f;
            for(int row=0;row<2;row++)
            {
                if(Mathf.Abs(y-bridgeY)>1.25f)
                {
                    b.Pose(new Vector2(BankX(y)+1.50f+row*1.07f,y+.29f*row),random.Next(-24,24),.70f+.16f*row);
                    OrchardTree(b,(i+row+section)%4==0);
                }
                if(i%2==0 && Mathf.Abs(y-mainY)>2.55f && Mathf.Abs(Mathf.Abs(y)-6.4f)>1.35f)
                {
                    b.Pose(new Vector2(mainX-.43f+row*1.40f,y+.39f*row),random.Next(-23,24),.83f);
                    OrchardTree(b,(i+row)%3==0);
                }
            }
        }
    }

    static void Walk(Builder b,Vector2 a,Vector2 bend,Vector2 d,float radius)
    {
        Vector2 prev=a;
        for(int i=1;i<=10;i++)
        {
            float t=i/10f;
            Vector2 next=(1-t)*(1-t)*a+2*t*(1-t)*bend+t*t*d;
            Vector2 startN=Vector2.Perpendicular(Vector2.Lerp(bend-a,d-bend,(i-1)/10f).normalized);
            Vector2 endN=Vector2.Perpendicular(Vector2.Lerp(bend-a,d-bend,t).normalized);
            void Strip(float w,float z,Color col)
            {
                Vector3 V(Vector2 v)=>new Vector3(v.x,v.y,z);
                b.Face(col,V(prev-startN*w),V(next-endN*w),V(next+endN*w),V(prev+startN*w));
            }
            Strip(radius+.075f,.029f,Bank);
            Strip(radius,.033f,SugarLane);
            prev=next;
        }
    }

    static void GardenBeds(Builder b,int variety)
    {
        Color soil=Color.Lerp(Bank,Cocoa,.40f);
        b.Box(new Vector3(0,0,.035f),new Vector3(3.19f,2.09f,.045f),Amber);
        for(int s=-1;s<=1;s+=2)
        {
            b.Box(new Vector3(s*.80f,0,.079f),new Vector3(1.45f,1.91f,.075f),soil);
            for(int row=0;row<4;row++)
            {
                float x=s*.80f+(row-1.5f)*.29f;
                b.Box(new Vector3(x,0,.14f),new Vector3(.15f,1.57f,.11f),Leaf);
                for(int j=0;j<6;j++)
                {
                    float y=(j-2.5f)*.25f;
                    Color c=(row+variety)%2==0?RedLeaf:Amber;
                    b.Face(c,new Vector3(x-.11f,y,.21f),new Vector3(x,y-.10f,.23f),
                        new Vector3(x+.11f,y,.21f),new Vector3(x,y+.09f,.26f));
                }
            }
        }
    }

    static void WaferBridge(Builder b)
    {
        b.Shadow(.22f,-.15f,1.64f,.52f);
        for(int i=-5;i<=5;i++)
        {
            float x=i*.25f, h=.14f+.30f*(1-x*x/1.9f);
            b.Box(new Vector3(x,0,h),new Vector3(.26f,.85f,.15f),i%2==0?Biscuit:Amber);
            for(int s=-1;s<=1;s+=2)
            {
                if(i%2==0) b.Box(new Vector3(x,s*.40f,h+.23f),new Vector3(.093f,.10f,.40f),Icing);
                if(i==5) continue;
                float nextH=.14f+.30f*(1-(x+.25f)*(x+.25f)/1.9f);
                b.Pipe(new[]{new Vector3(x,s*.40f,h+.42f),new Vector3(x+.25f,s*.40f,nextH+.42f)},.047f,Peppermint);
            }
        }
    }

    static void OrchardTree(Builder b,bool ripe)
    {
        b.Shadow(.13f,-.12f,.53f,.36f);
        b.Box(new Vector3(0,0,.30f),new Vector3(.13f,.13f,.60f),Cocoa);
        for(int k=0;k<3;k++)
        {
            float a=k*2.0944f;
            Vector3 p=new Vector3(.23f*Mathf.Cos(a),.19f*Mathf.Sin(a),.62f+k*.07f);
            b.Ball(p,new Vector3(.38f,.33f,.31f),k==2||ripe?RedLeaf:Leaf);
        }
        b.Ball(new Vector3(-.06f,.04f,.93f),new Vector3(.30f,.28f,.24f),ripe?Amber:RedLeaf);
    }

    // Profiles are revolved into fluted biscuit walls and contrasting folds of fondant.
    static void Tier(Builder b,int sides,float bottom,float top,float r0,float r1,Color c)
    {
        for(int k=0;k<sides;k++)
        {
            float a=k*Mathf.PI*2/sides, d=(k+1)*Mathf.PI*2/sides;
            Vector3 V(float t,float r,float h)=>new Vector3(Mathf.Cos(t)*r,Mathf.Sin(t)*r,h);
            b.Face(Shade(c,.78f+.21f*Mathf.Cos(a+2)),V(a,r0,bottom),V(d,r0,bottom),V(d,r1,top),V(a,r1,top));
            b.Face(c,V(a,r1,top),V(d,r1,top),new Vector3(0,0,top));
        }
    }

    static void Windmill(Builder b)
    {
        b.Shadow(.29f,-.24f,1.07f,.80f);
        Tier(b,10,.12f,1.68f,.85f,.62f,Biscuit);
        Tier(b,10,.43f,.52f,.84f,.84f,MilkChocolate);
        Tier(b,10,1.57f,1.69f,.78f,.79f,Icing);
        Tier(b,10,1.69f,2.27f,.85f,.16f,Peppermint);
        b.Box(new Vector3(0,-.74f,.49f),new Vector3(.30f,.09f,.62f),Cocoa);
        Vector3 hub=new Vector3(0,-.78f,1.52f);
        for(int k=0;k<4;k++)
        {
            float a=.53f+k*Mathf.PI/2;
            var axis=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
            var cross=new Vector3(-axis.z,0,axis.x);
            b.Pipe(new[]{hub,hub+axis*1.60f},.049f,Cocoa);
            b.Face(Icing,hub+axis*.51f,hub+axis*1.55f,hub+axis*1.47f+cross*.35f,hub+axis*.62f+cross*.25f);
            for(int j=0;j<4;j++)
            {
                var p=hub+axis*(.65f+j*.22f)+new Vector3(0,-.018f,0);
                b.Pipe(new[]{p,p+cross*(.25f+j*.021f)},.023f,Biscuit);
            }
        }
        b.Ball(hub,new Vector3(.16f,.11f,.16f),Amber);
    }

    static void Bakehouse(Builder b)
    {
        House(b,true,true,RedLeaf);
        for(int s=-1;s<=1;s+=2)
        {
            Vector3 p=new Vector3(s*.94f,.23f,.64f);
            b.Box(p,new Vector3(.58f,.67f,1.10f),Biscuit);
            b.Box(p+Vector3.forward*.59f,new Vector3(.71f,.77f,.15f),Icing);
            b.Ball(p+Vector3.forward*.67f,new Vector3(.36f,.38f,.49f),RedLeaf);
        }
        Tier(b,8,1.34f,2.10f,.46f,.44f,Biscuit);
        b.Disc(new Vector3(0,-.43f,1.75f),.22f,Cocoa,12);
        b.Disc(new Vector3(0,-.445f,1.75f),.17f,Icing,12);
        b.Pipe(new[]{new Vector3(-.09f,-.47f,1.79f),new Vector3(0,-.47f,1.75f),new Vector3(.02f,-.47f,1.88f)},.024f,Cocoa);
        Tier(b,8,2.09f,2.65f,.54f,.10f,RedLeaf);
        b.Ball(new Vector3(0,0,2.68f),new Vector3(.12f,.12f,.15f),Amber);
    }
}
