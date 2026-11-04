using UnityEngine;

public static partial class IndustrialMeshFactory
{
    static readonly Mesh[] voidForms=new Mesh[4];
    public static Mesh VoidSovereignBossMesh()=>VoidForm(0);
    public static Mesh VoidForm(int form)
    {
        if(voidForms[form]!=null)return voidForms[form];var b=new Builder();
        if(form==0)
        {
            // An elongated, hollow-eyed mask suspended in chipped obsidian petals.
            BevelArmor(b,new[]{V(0,-1.4f),V(-.68f,-.35f),V(-.78f,.62f),V(0,1.35f),V(.78f,.62f),V(.68f,-.35f)},.65f,11);
            for(int s=-1;s<=1;s+=2)
            {
                b.Prism(new[]{V(s*.15f,.48f),V(s*.58f,.25f),V(s*.42f,-.35f),V(s*.15f,-.17f)},.12f,8,new Vector3(0,0,-.38f),3);
                b.Prism(new[]{V(s*.55f,.6f),V(s*1.0f,1.35f),V(s*.9f,.15f),V(s*.63f,-.5f)},.22f,14,Vector3.zero,11);
            }
            b.Prism(new[]{V(-.08f,-.2f),V(0,-1.02f),V(.08f,-.2f)},.06f,3,new Vector3(0,0,-.4f),11);
        }
        else if(form==1)
        {
            // The mask unfolds into a broad, articulated pair of hooked blades.
            BevelArmor(b,new[]{V(0,-.9f),V(-.65f,-.4f),V(-.55f,.65f),V(0,1.1f),V(.55f,.65f),V(.65f,-.4f)},.7f,11);
            for(int s=-1;s<=1;s+=2)
            {
                b.Prism(new[]{V(s*.45f,.55f),V(s*1.55f,1.1f),V(s*2.2f,.45f),V(s*1.65f,-.75f),V(s*1.4f,-.4f),V(s*1.55f,.35f),V(s*.55f,.12f)},.32f,14,Vector3.zero,11);
                b.Prism(new[]{V(s*.62f,.38f),V(s*1.5f,.72f),V(s*1.9f,.4f),V(s*1.64f,-.46f),V(s*1.66f,.36f)},.08f,3,new Vector3(0,0,-.24f),11);
                b.Drum(new Vector3(s*1.65f,-.46f,-.22f),.13f,.10f,8,6,3);
                b.Prism(new[]{V(s*.15f,.4f),V(s*.42f,.22f),V(s*.19f,-.22f)},.08f,9,new Vector3(0,0,-.42f),3);
            }
        }
        else if(form==2)
        {
            // A deep open maw; the dark cavity is real negative space between the jaw plates.
            b.Prism(new[]{V(-1.15f,.12f),V(-1,.92f),V(0,1.35f),V(1,.92f),V(1.15f,.12f),V(.55f,.48f),V(-.55f,.48f)},.55f,14,Vector3.zero,11);
            b.Prism(new[]{V(-1.15f,-.06f),V(-.95f,-.95f),V(0,-1.3f),V(.95f,-.95f),V(1.15f,-.06f),V(.55f,-.45f),V(-.55f,-.45f)},.5f,11,Vector3.zero,14);
            for(int s=-1;s<=1;s+=2)for(int i=0;i<4;i++)
            {
                float x=s*(.18f+i*.22f);
                b.Prism(new[]{V(x-.10f,.5f),V(x,.07f+(i%2)*.1f),V(x+.10f,.5f)},.2f,3,new Vector3(0,0,-.2f),11);
                b.Prism(new[]{V(x-.10f,-.45f),V(x,-.05f-(i%2)*.12f),V(x+.10f,-.45f)},.2f,3,new Vector3(0,0,-.2f),11);
                b.Prism(new[]{V(s*.9f,.5f-i*.3f),V(s*(1.7f+i*.17f),.82f-i*.5f),V(s*(1.45f+i*.13f),-.2f-i*.4f)},.17f,14,new Vector3(0,0,.1f),11);
            }
            for(int s=-1;s<=1;s+=2)b.Drum(new Vector3(s*.75f,-.85f,-.32f),.17f,.10f,8,6,3);
        }
        else
        {
            // The final shape contracts into an eight-point wound with four long, visible firing tips.
            BevelArmor(b,new[]{V(-.55f,-.55f),V(-.8f,0),V(-.55f,.55f),V(0,.8f),V(.55f,.55f),V(.8f,0),V(.55f,-.55f),V(0,-.8f)},.7f,11);
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI/4;Vector2 axis=new Vector2(Mathf.Cos(a),Mathf.Sin(a)),side=new Vector2(-axis.y,axis.x);
                float length=i%2==0?1.85f:1.23f;
                b.Prism(new[]{axis*.45f-side*.22f,axis*length,axis*.85f+side*.3f,axis*.5f+side*.2f},.25f,14,new Vector3(0,0,-.06f),11);
                b.Prism(new[]{axis*.62f-side*.035f,axis*(length-.12f),axis*.62f+side*.055f},.06f,3,new Vector3(0,0,-.23f),11);
            }
            b.Prism(new[]{V(-.29f,0),V(0,-.40f),V(.29f,0),V(0,.40f)},.15f,8,new Vector3(0,0,-.48f),3);
            b.Prism(new[]{V(-.055f,0),V(0,-.31f),V(.055f,0),V(0,.31f)},.02f,11,new Vector3(0,0,-.58f),11);
        }
        return voidForms[form]=b.Finish(new[]{"Void / unblinking mask","Void / folding mantis","Void / devouring maw","Void / final wound"}[form]);
    }
}
