using System.Collections.Generic;
using UnityEngine;

/// <summary>One seeded, periodic landscape plan shared by terrain, water and vegetation.</summary>
public sealed class PSXValleyLayout
{
    public const float Period=96;
    public struct Plant { public float x,y,radius,angle; public int kind,seed; }
    public readonly List<Plant> Plants=new List<Plant>();
    public readonly float BridgeY,LookoutY;
    readonly float phase,phase2;
    public PSXValleyLayout(int seed)
    {
        var rng=new System.Random(seed ^ 0x51a7);
        phase=Range(rng,-.65f,.65f);phase2=Range(rng,0,6.28f);
        BridgeY=Range(rng,3,10);LookoutY=Range(rng,43,58);
        // Independent grove centres, sizes and gaps: never rows or mirrored pairs.
        for(int group=0;group<37;group++)
        {
            float y=Range(rng,-24,72),side=rng.Next(2)==0 ? -1 : 1;
            float x=Centre(y)+side*(HalfWidth(y)+Range(rng,side<0 ? 2.15f : .85f,4.8f));
            int count=rng.Next(2,7);float spread=Range(rng,.7f,1.65f);
            for(int i=0;i<count;i++)
            {
                float a=Range(rng,0,Mathf.PI*2),r=Mathf.Sqrt(Range(rng,0,1))*spread;
                float tx=x+Mathf.Cos(a)*r,ty=Wrap(y+Mathf.Sin(a)*r);
                float radius=Range(rng,.38f,.85f);
                TryPlant(tx,ty,radius,rng.NextDouble()<.22 ? 1 : 0,rng);
            }
        }
        // Smaller riparian plants hug either bank, with uneven runs and bare gravel gaps.
        for(int i=0;i<155;i++)
        {
            float y=Range(rng,-24,72),side=rng.Next(2)==0 ? -1 : 1;
            float x=Centre(y)+side*(HalfWidth(y)+Range(rng,.24f,.49f));
            TryPlant(x,y,Range(rng,.12f,.24f),2,rng);
        }
    }
    public float Centre(float y)=>1.3f*Mathf.Sin(y*Mathf.PI*4/Period+phase)+.44f*Mathf.Sin(y*Mathf.PI*6/Period+phase2);
    public float HalfWidth(float y)=>1.42f+.31f*Mathf.Sin(y*Mathf.PI*4/Period+phase2)+.13f*Mathf.Sin(y*Mathf.PI*10/Period+phase);
    public float BankWidth(float y,int side)=>.30f+.17f*(1+Mathf.Sin(y*Mathf.PI*8/Period+side+phase2));
    // Small erosion scallops share the landscape's period, including the recycled end section.
    public float ShoreX(float y,int side)
    {
        float pocket=.5f+.5f*Mathf.Sin(y*Mathf.PI*8/Period+phase2+side*1.9f);
        float erosion=.11f*Mathf.Sin(y*Mathf.PI*16/Period+phase2+side*1.7f)+
            .045f*Mathf.Sin(y*Mathf.PI*38/Period+phase+side*.8f)-.27f*Mathf.Pow(pocket,4);
        return Centre(y)+side*(HalfWidth(y)+erosion);
    }
    public static float Wrap(float y)=>Mathf.Repeat(y+24,Period)-24;
    public static float DistanceY(float a,float b)=>Mathf.Abs(Mathf.Repeat(a-b+Period/2,Period)-Period/2);
    public bool Clear(float x,float y,float radius)
    {
        if(Mathf.Abs(x)+radius>6.8f || Mathf.Abs(x-Centre(y))<HalfWidth(y)+radius+.12f)return false;
        float road=Centre(y)-HalfWidth(y)-1.15f;
        if(Mathf.Abs(x-road)<radius+.56f)return false;
        if(DistanceY(y,BridgeY)<radius+2.5f)return false;
        float lookoutX=Centre(LookoutY)+HalfWidth(LookoutY)+1.6f;
        return DistanceY(y,LookoutY)>radius+1.3f || Mathf.Abs(x-lookoutX)>radius+1.05f;
    }
    void TryPlant(float x,float y,float radius,int kind,System.Random rng)
    {
        if(!Clear(x,y,radius))return;
        foreach(var other in Plants)
        {
            float dx=x-other.x,dy=DistanceY(y,other.y),gap=(radius+other.radius)*.80f;
            if(dx*dx+dy*dy<gap*gap)return;
        }
        Plants.Add(new Plant{x=x,y=y,radius=radius,kind=kind,angle=Range(rng,0,360),seed=rng.Next()});
    }
    static float Range(System.Random rng,float lo,float hi)=>Mathf.Lerp(lo,hi,(float)rng.NextDouble());
}
