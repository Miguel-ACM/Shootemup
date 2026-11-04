using UnityEngine;

public static partial class IndustrialMeshFactory
{
    static readonly Mesh[] voidForms=new Mesh[4];
    public static Mesh VoidSovereignBossMesh()=>VoidForm(0);
    static void VoidRib(Builder b,Vector3 a,Vector3 p,Vector3 q,Vector3 end,float width,float depth,int tile=14)
    {
        b.Sweep(a,p,q,end,width,depth,tile);
        b.Sweep(a+Vector3.back*depth,p+Vector3.back*depth*.7f,q+Vector3.back*depth*.55f,end,.014f,.018f,3);
    }
    static void VoidEye(Builder b,Vector3 p,Vector3 size)
    {
        b.Pearl(p+Vector3.forward*.035f,size*1.3f,11,24,12);
        b.Pearl(p,size,8,28,14);
        b.Pearl(p+Vector3.back*size.z*.9f,new Vector3(size.x*.18f,size.y*.82f,size.z*.2f),11,16,10);
    }
    public static Mesh VoidForm(int form)
    {
        if(voidForms[form]!=null)return voidForms[form];var b=new Builder();
        if(form==0)
        {
            // The Veiled Monarch: a carved living mask inside a crown of swept obsidian horns.
            b.Pearl(new Vector3(0,.18f,.12f),new Vector3(.75f,.91f,.48f),11,36,22);
            for(int s=-1;s<=1;s+=2)
            {
                VoidRib(b,new Vector3(s*.13f,1.0f,0),new Vector3(s*1.06f,.75f,-.1f),new Vector3(s*.7f,-.82f,-.3f),new Vector3(0,-1.45f,-.1f),.31f,.23f);
                VoidRib(b,new Vector3(s*.1f,.25f,-.39f),new Vector3(s*.43f,.35f,-.55f),new Vector3(s*.62f,.1f,-.43f),new Vector3(s*.64f,-.42f,-.25f),.11f,.11f);
                VoidEye(b,new Vector3(s*.32f,-.18f,-.44f),new Vector3(.21f,.11f,.085f));
                for(int horn=0;horn<3;horn++)
                {
                    float x=s*(.4f+horn*.23f);
                    VoidRib(b,new Vector3(x,.5f,.1f),new Vector3(s*(1.25f+horn*.24f),.72f+horn*.22f,.1f),new Vector3(s*(.9f+horn*.3f),1.65f+horn*.10f,.0f),new Vector3(s*(.45f+horn*.31f),1.75f+horn*.12f,-.05f),.18f-horn*.025f,.14f);
                }
                b.Sweep(new Vector3(s*.08f,-.4f,-.4f),new Vector3(s*.23f,-.58f,-.5f),new Vector3(s*.13f,-.91f,-.38f),new Vector3(0,-1.15f,-.17f),.09f,.09f,14);
            }
            VoidEye(b,new Vector3(0,-.54f,-.41f),new Vector3(.065f,.21f,.045f));
        }
        else if(form==1)
        {
            // The Severing Saint: a narrow thorax carried by two huge hooked scythes.
            b.Pearl(new Vector3(0,.1f,.02f),new Vector3(.47f,.92f,.40f),11,32,20);
            for(int s=-1;s<=1;s+=2)
            {
                VoidRib(b,new Vector3(s*.3f,.48f,0),new Vector3(s*2.65f,1.65f,.12f),new Vector3(s*2.4f,-1.28f,-.05f),new Vector3(s*1.65f,-.46f,-.22f),.32f,.25f);
                VoidEye(b,new Vector3(s*1.65f,-.46f,-.22f),new Vector3(.09f,.15f,.065f));
                VoidRib(b,new Vector3(s*.3f,-.3f,.12f),new Vector3(s*1.65f,-.6f,.05f),new Vector3(s*1.65f,-1.6f,-.08f),new Vector3(s*.58f,-1.64f,-.07f),.19f,.16f);
                for(int rib=0;rib<5;rib++)
                    VoidRib(b,new Vector3(s*.12f,.65f-rib*.23f,-.28f),new Vector3(s*.68f,.61f-rib*.23f,-.32f),new Vector3(s*.65f,.20f-rib*.22f,-.18f),new Vector3(s*.22f,.14f-rib*.23f,-.12f),.08f,.08f);
                VoidRib(b,new Vector3(s*.22f,.65f,0),new Vector3(s*.62f,1.1f,0),new Vector3(s*.45f,1.48f,-.1f),new Vector3(s*.11f,1.62f,-.1f),.15f,.13f);
            }
            VoidEye(b,new Vector3(0,.44f,-.44f),new Vector3(.19f,.26f,.08f));
        }
        else if(form==2)
        {
            // The Devourer: a deep living mouth, three nested jaws and a flared rib-cage.
            b.Pearl(new Vector3(0,0,.35f),new Vector3(.82f,.92f,.14f),11,32,18);
            b.Halo(Vector3.zero,.92f,.23f,new Vector3(1,1.15f,1),Quaternion.identity,14);
            b.Halo(new Vector3(0,0,-.10f),.70f,.09f,new Vector3(1,1.15f,1),Quaternion.identity,11);
            VoidEye(b,new Vector3(0,0,.16f),new Vector3(.28f,.4f,.08f));
            for(int tooth=0;tooth<18;tooth++)
            {
                float a=tooth*Mathf.PI*2/18;var axis=new Vector3(Mathf.Cos(a),Mathf.Sin(a)*1.15f,0);
                b.Sweep(axis*.83f,axis*.69f+Vector3.back*.14f,axis*.53f+Vector3.back*.20f,axis*(tooth%2==0?.40f:.50f)+Vector3.back*.05f,.10f,.08f,tooth%2==0?14:3);
            }
            for(int s=-1;s<=1;s+=2)
            {
                for(int wing=0;wing<4;wing++)
                    VoidRib(b,new Vector3(s*.68f,.72f-wing*.38f,.15f),new Vector3(s*(1.65f+wing*.16f),1.4f-wing*.43f,.10f),new Vector3(s*(2.2f-wing*.10f),.65f-wing*.55f,.05f),new Vector3(s*(1.2f+wing*.15f),-.2f-wing*.43f,-.12f),.19f,.14f);
                VoidEye(b,new Vector3(s*.75f,-.85f,-.32f),new Vector3(.15f,.16f,.09f));
                VoidRib(b,new Vector3(s*.55f,-.72f,.12f),new Vector3(s*1.0f,-1.7f,.20f),new Vector3(s*.1f,-2.2f,.08f),new Vector3(s*.2f,-1.45f,-.05f),.13f,.10f);
            }
        }
        else
        {
            // The Unmade: eight curved fracture-wings around a luminous wound, not a flat star.
            b.Pearl(new Vector3(0,0,.1f),new Vector3(.58f,.85f,.38f),11,36,22);
            for(int wing=0;wing<8;wing++)
            {
                float a=wing*Mathf.PI/4;var axis=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);var side=new Vector3(-axis.y,axis.x,0);
                float length=wing%2==0?2.15f:1.73f;
                VoidRib(b,axis*.32f,axis*.82f+side*.68f+Vector3.forward*.12f,axis*length+side*.55f,axis*length-side*.09f,.26f,.20f);
                VoidRib(b,axis*.5f,axis*.9f-side*.16f+Vector3.back*.10f,axis*1.5f-side*.25f+Vector3.back*.08f,axis*1.65f,.11f,.10f);
                if(wing%2==0)VoidEye(b,axis*1.65f+Vector3.back*.22f,new Vector3(.095f,.095f,.07f));
            }
            for(int s=-1;s<=1;s+=2)
                VoidRib(b,new Vector3(0,.84f,-.14f),new Vector3(s*.58f,.6f,-.4f),new Vector3(s*.38f,-.65f,-.4f),new Vector3(0,-1.04f,-.14f),.16f,.14f);
            VoidEye(b,new Vector3(0,0,-.34f),new Vector3(.25f,.63f,.14f));
        }
        return voidForms[form]=b.Finish(new[]{"Void / Veiled Monarch","Void / Severing Saint","Void / Devourer","Void / Unmade"}[form]);
    }
}

