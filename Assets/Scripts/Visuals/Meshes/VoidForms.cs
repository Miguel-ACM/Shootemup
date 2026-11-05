using UnityEngine;

public static partial class IndustrialMeshFactory
{
    static readonly Mesh[] voidForms=new Mesh[4],voidCores=new Mesh[4];
    static readonly Mesh[,] voidLimbs=new Mesh[4,4];
    public static Mesh VoidSovereignBossMesh()=>VoidForm(0);
    public static Mesh VoidForm(int form)
    {
        if(voidForms[form]!=null)return voidForms[form];
        int count=form==3?4:2;var parts=new CombineInstance[count+1];
        parts[0]=new CombineInstance{mesh=VoidCore(form),transform=Matrix4x4.identity};
        for(int i=0;i<count;i++)parts[i+1]=new CombineInstance{mesh=VoidLimb(form,i),transform=Matrix4x4.identity};
        var mesh=new Mesh{name="Void / fractured obsidian form "+form};mesh.CombineMeshes(parts);return voidForms[form]=mesh;
    }
    static void VoidEye(Builder b,Vector3 p,Vector3 size)
    {
        CutStone(b,p+Vector3.forward*.035f,Vector3.Scale(size,new Vector3(1.45f,1.45f,.65f)),11,6);
        CutStone(b,p,size,8,6);
        CutStone(b,p+Vector3.back*size.z,new Vector3(size.x*.18f,size.y*.85f,.012f),11,4);
    }
    public static Mesh VoidCore(int form)
    {
        if(voidCores[form]!=null)return voidCores[form];var b=new Builder();
        if(form==0)
        {
            CutStone(b,new Vector3(0,.15f,.12f),new Vector3(.68f,.88f,.40f),11,6,Mathf.PI/6);
            for(int s=-1;s<=1;s+=2)
            {
                BevelArmor(b,new[]{V(s*.05f,.65f),V(s*.57f,.46f),V(s*.67f,-.31f),V(s*.17f,-1.15f),V(0,-1.38f)},.27f,14,-.12f,.22f);
                CutBlade(b,new[]{new Vector3(s*.06f,.08f,-.38f),new Vector3(s*.49f,.08f,-.40f),new Vector3(s*.65f,-.34f,-.20f)},.095f,.09f,14);
                VoidEye(b,new Vector3(s*.32f,-.18f,-.44f),new Vector3(.21f,.10f,.055f));
            }
            VoidEye(b,new Vector3(0,-.65f,-.36f),new Vector3(.055f,.19f,.035f));
        }
        else if(form==1)
        {
            CutStone(b,Vector3.zero,new Vector3(.42f,.94f,.34f),11,6,Mathf.PI/6);
            for(int s=-1;s<=1;s+=2)for(int rib=0;rib<4;rib++)
            {
                float y=.6f-rib*.29f;
                CutBlade(b,new[]{new Vector3(s*.04f,y,-.32f),new Vector3(s*.48f,y-.13f,-.26f),new Vector3(s*.23f,y-.35f,-.13f)},.095f,.10f,14);
            }
            CutBlade(b,new[]{new Vector3(0,.60f,0),new Vector3(-.26f,1.13f,-.06f),new Vector3(0,1.63f,-.10f)},.14f,.13f,14);
            VoidEye(b,new Vector3(0,.44f,-.44f),new Vector3(.17f,.24f,.06f));
        }
        else if(form==2)
        {
            // A deep octagonal throat with jagged ivory-black teeth, not a torus.
            CutStone(b,new Vector3(0,0,.25f),new Vector3(.91f,1.03f,.11f),11,8);
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI/4;var u=new Vector3(Mathf.Cos(a),Mathf.Sin(a)*1.15f,0);var v=new Vector3(-Mathf.Sin(a),Mathf.Cos(a),0);
                CutBlade(b,new[]{u*.89f+v*.28f,u*.98f,u*.89f-v*.28f},.13f,.16f,14);
                CutBlade(b,new[]{u*.81f,u*.62f+Vector3.back*.16f,u*.43f+Vector3.back*.05f},.11f,.10f,3);
            }
            VoidEye(b,new Vector3(0,0,.06f),new Vector3(.23f,.33f,.055f));
        }
        else
        {
            CutStone(b,new Vector3(0,0,.08f),new Vector3(.59f,.88f,.28f),11,6);
            for(int s=-1;s<=1;s+=2)
                CutBlade(b,new[]{new Vector3(0,.92f,-.10f),new Vector3(s*.48f,.33f,-.32f),new Vector3(s*.34f,-.54f,-.34f),new Vector3(0,-1.06f,-.1f)},.16f,.17f,14);
            VoidEye(b,new Vector3(0,0,-.56f),new Vector3(.20f,.61f,.10f));
        }
        return voidCores[form]=b.Finish("Void / carved central target "+form);
    }
    public static Mesh VoidLimb(int form,int index)
    {
        if(voidLimbs[form,index]!=null)return voidLimbs[form,index];var b=new Builder();int s=index==0?-1:1;
        Vector3 P(float x,float y,float z=0)=>new Vector3(s*x,y,z);
        if(form==0)
        {
            for(int horn=0;horn<3;horn++)
            {
                float x=.39f+horn*.22f;
                CutBlade(b,new[]{P(x,.47f,.08f),P(1.03f+horn*.22f,.91f+horn*.10f),P(.92f+horn*.27f,1.51f+horn*.15f),P(.47f+horn*.31f,1.78f+horn*.14f,-.06f)},.18f-horn*.025f,.16f,14);
                CutBlade(b,new[]{P(x,.49f,-.09f),P(1.03f+horn*.22f,.91f+horn*.10f,-.15f),P(.92f+horn*.27f,1.51f+horn*.15f,-.09f)},.014f,.012f,3);
            }
        }
        else if(form==1)
        {
            CutBlade(b,new[]{P(.30f,.42f),P(1.60f,1.13f,.08f),P(2.22f,.65f),P(2.10f,-.63f,-.06f),P(1.65f,-.46f,-.22f)},.30f,.24f,14);
            CutBlade(b,new[]{P(.30f,-.33f,.12f),P(1.32f,-.66f),P(1.40f,-1.37f),P(.59f,-1.68f,-.08f)},.17f,.15f,14);
            VoidEye(b,P(1.65f,-.46f,-.22f),new Vector3(.085f,.14f,.045f));
            CutStone(b,P(.45f,.46f,-.15f),new Vector3(.20f,.22f,.12f),14,6);
        }
        else if(form==2)
        {
            for(int rib=0;rib<4;rib++)
                CutBlade(b,new[]{P(.68f,.74f-rib*.38f,.14f),P(1.44f+rib*.14f,1.10f-rib*.42f,.07f),P(2.02f-rib*.08f,.54f-rib*.51f),P(1.24f+rib*.12f,-.16f-rib*.42f,-.12f)},.17f,.15f,14);
            CutBlade(b,new[]{P(.52f,-.66f,.10f),P(.92f,-1.47f),P(.22f,-1.97f,-.05f)},.16f,.13f,14);
            VoidEye(b,P(.75f,-.85f,-.32f),new Vector3(.14f,.16f,.065f));
        }
        else
        {
            float a=index==0?Mathf.PI:index==1?0:index==2?Mathf.PI*.5f:-Mathf.PI*.5f;
            var u=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);var v=new Vector3(-u.y,u.x,0);
            CutBlade(b,new[]{u*.31f,u*.94f+v*.42f+Vector3.forward*.08f,u*1.85f+v*.40f,u*2.22f-v*.10f},.24f,.22f,14);
            CutBlade(b,new[]{u*.49f,u*.88f-v*.34f,u*1.38f-v*.64f,u*1.54f-v*.38f},.14f,.16f,14);
            CutBlade(b,new[]{u*.40f+Vector3.back*.1f,u*1.19f+Vector3.back*.12f,u*1.65f+Vector3.back*.20f},.08f,.1f,14);
            VoidEye(b,u*1.65f+Vector3.back*.22f,new Vector3(.09f,.09f,.05f));
        }
        return voidLimbs[form,index]=b.Finish("Void / articulated fracture "+form+" / "+index);
    }
}
