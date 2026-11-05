using UnityEngine;

public static partial class IndustrialMeshFactory
{
    static readonly Mesh[] heavenBastions=new Mesh[6];
    public static Mesh HeavenBastion(int variant)
    {
        if(heavenBastions[variant]!=null)return heavenBastions[variant];
        var b=new Builder();
        // Deep stepped masonry, with deliberately uneven broken edges and open sky gaps.
        for(int layer=0;layer<3;layer++)
        {
            float w=2.05f-layer*.16f,h=.65f-layer*.10f,z=.22f-layer*.22f;
            BevelArmor(b,new[]{V(-w,-h),V(w*.80f,-h-.10f),V(w,h*.4f),
                V(w*.72f,h),V(-w*.86f,h+.16f)},.22f,layer==0?1:15,z);
        }
        for(int tile=0;tile<8;tile++)
            b.Box(new Vector3(-1.65f+tile*.46f,-.40f,-.52f),new Vector3(.42f,.43f,.07f),tile%3==0?1:15,7);
        // An arch has solid piers and individually cut voussoirs, not a bent tube.
        int arches=variant==0||variant==3?2:1;
        for(int arch=0;arch<arches;arch++)
        {
            float cx=arches==2?(arch==0?-.9f:.9f):0,r=arches==2?.67f:1.04f;
            float spring=.93f+variant%2*.16f;
            for(int side=-1;side<=1;side+=2)
            {
                Vector3 foot=new Vector3(cx+side*r,.17f,-.6f);
                b.Box(foot+Vector3.up*.36f,new Vector3(.28f,.9f,.40f),15,1);
                b.Box(foot+Vector3.down*.10f,new Vector3(.45f,.19f,.52f),1,7);
                b.Box(new Vector3(foot.x,spring,-.6f),new Vector3(.43f,.17f,.47f),15,3);
                b.Box(foot+new Vector3(side*.13f,.35f,.18f),new Vector3(.20f,.76f,.42f),1,7);
            }
            for(int stone=0;stone<11;stone++)
            {
                if(variant%3==1&&stone>6)continue;
                float a=stone*Mathf.PI/11,c=(stone+.94f)*Mathf.PI/11;
                Vector3 P(float angle,float radius)=>new Vector3(cx+Mathf.Cos(angle)*radius,spring+Mathf.Sin(angle)*radius,-.60f);
                BevelArmor(b,new[]{(Vector2)P(a,r-.13f),(Vector2)P(c,r-.13f),(Vector2)P(c,r+.18f),(Vector2)P(a,r+.18f)},.34f,stone%4==0?1:15,-.6f);
                b.Sweep(P(a,r-.17f)+Vector3.back*.20f,P(a+.02f,r-.17f)+Vector3.back*.20f,P(c-.02f,r-.17f)+Vector3.back*.20f,P(c,r-.17f)+Vector3.back*.20f,.025f,.025f,3,false,6);
            }
        }
        // A war in heaven: broken lances, spear-point railing and a fallen marble beam.
        for(int spear=0;spear<9;spear++)
        {
            float x=-1.85f+spear*.45f,h=.58f+(spear%3)*.17f;
            if(spear==variant)h*=.45f;
            var foot=new Vector3(x,-.58f,-.73f);
            var tip=foot+new Vector3((spear%4==0?.12f:0),h,0);
            b.Sweep(foot,Vector3.Lerp(foot,tip,.3f),Vector3.Lerp(foot,tip,.7f),tip,.024f,.024f,7,false,6);
            Feather(b,tip,tip+Vector3.up*.20f,.07f,.035f,3);
        }
        b.Box(new Vector3(0,-.28f,-.73f),new Vector3(3.9f,.045f,.045f),3,7);
        if(variant>=2)
            b.Sweep(new Vector3(-1.3f,-.64f,-.62f),new Vector3(-.65f,-.52f,-.70f),new Vector3(.1f,-.32f,-.71f),new Vector3(.7f,-.17f,-.64f),.17f,.14f,1,false,8);
        return heavenBastions[variant]=b.Finish("Heaven / cut-stone bastion "+variant);
    }
}
