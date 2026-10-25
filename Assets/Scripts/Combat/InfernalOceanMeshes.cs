using UnityEngine;

public static partial class IndustrialMeshFactory
{
    static Mesh crabArm;
    public static Mesh CrabArm()
    {
        if(crabArm==null)
        {
            var b=new Builder();b.Ball(Vector3.zero,new Vector3(.37f,.58f,.26f),11);
            b.Prism(new[]{new Vector2(-.38f,.42f),new Vector2(-.30f,-.45f),new Vector2(0,-.62f),new Vector2(.30f,-.45f),new Vector2(.38f,.42f),new Vector2(0,.60f)},.34f,12,new Vector3(0,0,-.20f),5);
            b.Box(new Vector3(0,.28f,-.41f),new Vector3(.55f,.12f,.08f),6);
            b.Prism(new[]{new Vector2(-.30f,.32f),new Vector2(-.57f,.60f),new Vector2(-.23f,.57f)},.16f,1,Vector3.zero);
            crabArm=b.Finish("Overlapping armored pincer vertebra");
        }
        return crabArm;
    }
    static void BuildSkull(Builder b)
    {
        b.Ball(new Vector3(0,.15f,0),new Vector3(.70f,.72f,.33f),1);
        for(int side=-1;side<=1;side+=2)
        {
            b.Ball(new Vector3(side*.28f,.12f,-.28f),new Vector3(.22f,.23f,.11f),11);
            b.Box(new Vector3(side*.28f,.10f,-.39f),new Vector3(.13f,.12f,.05f),9);
            b.Prism(new[]{new Vector2(side*.43f,.55f),new Vector2(side*.88f,1.02f),new Vector2(side*.67f,.36f)},.18f,1,Vector3.zero);
        }
        b.Prism(new[]{new Vector2(0,-.05f),new Vector2(-.12f,-.30f),new Vector2(.12f,-.30f)},.08f,11,new Vector3(0,0,-.35f));
        b.Box(new Vector3(0,-.43f,-.02f),new Vector3(.75f,.27f,.43f),6);
        for(int i=-2;i<=2;i++)b.Box(new Vector3(i*.14f,-.43f,-.27f),new Vector3(.095f,.23f,.08f),1);
    }
    static void BuildGhostShip(Builder b)
    {
        b.Prism(new[]{new Vector2(0,-1.30f),new Vector2(-.52f,-.70f),new Vector2(-.62f,.55f),new Vector2(-.28f,1.05f),new Vector2(.28f,1.05f),new Vector2(.62f,.55f),new Vector2(.52f,-.70f)},.38f,8,Vector3.zero,11);
        b.Prism(new[]{new Vector2(0,-1.13f),new Vector2(-.37f,-.55f),new Vector2(-.40f,.68f),new Vector2(.40f,.68f),new Vector2(.37f,-.55f)},.13f,6,new Vector3(0,0,-.30f));
        b.Box(new Vector3(0,.32f,-.65f),new Vector3(.12f,1.30f,.80f),11);
        for(int s=-1;s<=1;s+=2)
        {
            b.Prism(new[]{new Vector2(s*.07f,.92f),new Vector2(s*.91f,.39f),new Vector2(s*.70f,-.28f),new Vector2(s*.46f,-.07f),new Vector2(s*.20f,-.38f),new Vector2(s*.07f,-.18f)},.055f,15,new Vector3(0,0,-.95f),8);
            for(int j=0;j<3;j++)b.Box(new Vector3(s*.58f,.43f-j*.42f,-.22f),new Vector3(.28f,.16f,.18f),9);
        }
    }
    static void BuildCinderImp(Builder b)
    {
        b.Prism(new[]{new Vector2(0,-.95f),new Vector2(-.24f,-.14f),new Vector2(-.20f,.53f),new Vector2(0,.83f),new Vector2(.20f,.53f),new Vector2(.24f,-.14f)},.27f,5,Vector3.zero);
        for(int s=-1;s<=1;s+=2)b.Prism(new[]{new Vector2(s*.14f,.40f),new Vector2(s*1.0f,.78f),new Vector2(s*.63f,-.14f),new Vector2(s*.34f,.02f),new Vector2(s*.32f,-.45f)},.16f,12,Vector3.zero,11);
        b.Drum(new Vector3(0,-.17f,-.24f),.18f,.13f,10);
    }
    static void BuildHellCrab(Builder b)
    {
        b.Prism(new[]{new Vector2(-1.78f,.10f),new Vector2(-1.38f,1.05f),new Vector2(-.55f,1.45f),new Vector2(.55f,1.45f),new Vector2(1.38f,1.05f),new Vector2(1.78f,.10f),new Vector2(1.15f,-.95f),new Vector2(.52f,-1.35f),new Vector2(-.52f,-1.35f),new Vector2(-1.15f,-.95f)},.78f,5,Vector3.zero,11);
        // Broad overlapping carapace plates replace the round face-like shell.
        for(int row=0;row<4;row++)
        {
            float y=.78f-row*.38f,w=1.45f-Mathf.Abs(row-1.3f)*.20f;
            b.Prism(new[]{new Vector2(-w,y+.24f),new Vector2(-w*.85f,y-.24f),new Vector2(0,y-.36f),new Vector2(w*.85f,y-.24f),new Vector2(w,y+.24f),new Vector2(0,y+.35f)},.20f, row%2==0 ? 12 : 5,new Vector3(0,0,-.48f-row*.035f),11);
        }
        for(int s=-1;s<=1;s+=2)
        {
            for(int i=0;i<3;i++)b.Prism(new[]{new Vector2(s*(1.12f-i*.18f),.75f+i*.19f),new Vector2(s*(1.75f-i*.32f),1.48f+i*.18f),new Vector2(s*(.75f-i*.18f),.98f+i*.15f)},.25f,1,Vector3.zero);
            b.Drum(new Vector3(s*.67f,-.52f,-.78f),.41f,.17f,11,8);
            for(int j=0;j<3;j++)b.Prism(new[]{new Vector2(s*1.22f,.58f-j*.5f),new Vector2(s*2.08f,.16f-j*.58f),new Vector2(s*1.74f,-.24f-j*.52f),new Vector2(s*1.49f,.07f-j*.48f)},.24f,6,Vector3.zero);
        }
        b.Box(new Vector3(0,-1.04f,-.43f),new Vector3(.76f,.26f,.32f),11);
        for(int i=-2;i<=2;i++)b.Box(new Vector3(i*.15f,-1.03f,-.63f),new Vector3(.10f,.25f,.12f),1);
    }
    static void BuildHellClaw(Builder b)
    {
        b.Ball(new Vector3(0,.30f,0),new Vector3(.56f,.68f,.36f),5);
        b.Prism(new[]{new Vector2(-.48f,.12f),new Vector2(-.67f,-.45f),new Vector2(-.35f,-1.13f),new Vector2(.03f,-1.40f),new Vector2(-.06f,-.76f),new Vector2(-.18f,-.22f)},.40f,12,Vector3.zero,1);
        b.Prism(new[]{new Vector2(.30f,.13f),new Vector2(.67f,-.31f),new Vector2(.48f,-.88f),new Vector2(.12f,-1.17f),new Vector2(.25f,-.48f),new Vector2(.12f,-.12f)},.36f,12,Vector3.zero,1);
        b.Box(new Vector3(0,.13f,-.38f),new Vector3(.26f,.45f,.12f),9);
    }
    static void BuildReefDart(Builder b)
    {
        b.Ball(Vector3.zero,new Vector3(.34f,.92f,.25f),8);
        for(int s=-1;s<=1;s+=2)b.Prism(new[]{new Vector2(s*.18f,.35f),new Vector2(s*.85f,.63f),new Vector2(s*.43f,-.20f)},.10f,6,Vector3.zero);
        b.Prism(new[]{new Vector2(0,.64f),new Vector2(-.37f,1.12f),new Vector2(.37f,1.12f)},.12f,13,Vector3.zero);
        b.Drum(new Vector3(0,-.53f,-.25f),.15f,.09f,10);
    }
    static void BuildManta(Builder b)
    {
        b.Prism(new[]{new Vector2(0,-.94f),new Vector2(-.36f,-.32f),new Vector2(-1.28f,.34f),new Vector2(-.87f,.75f),new Vector2(-.27f,.44f),new Vector2(0,.73f),new Vector2(.27f,.44f),new Vector2(.87f,.75f),new Vector2(1.28f,.34f),new Vector2(.36f,-.32f)},.19f,4,Vector3.zero,11);
        b.Ball(new Vector3(0,.02f,-.16f),new Vector3(.28f,.59f,.20f),13);
        for(int s=-1;s<=1;s+=2)b.Box(new Vector3(s*.66f,.20f,-.16f),new Vector3(.45f,.075f,.09f),9);
        b.Box(new Vector3(0,1.05f,0),new Vector3(.08f,.85f,.08f),6);
    }
    static void BuildBathysphere(Builder b)
    {
        b.Ball(Vector3.zero,new Vector3(.65f,.67f,.48f),13);
        b.Drum(new Vector3(0,-.13f,-.48f),.32f,.13f,11,10);
        b.Drum(new Vector3(0,-.13f,-.57f),.22f,.10f,9,10);
        for(int s=-1;s<=1;s+=2)
        {
            b.Box(new Vector3(s*.70f,.02f,0),new Vector3(.23f,.92f,.25f),8);
            b.Box(new Vector3(s*.70f,-.42f,-.12f),new Vector3(.16f,.30f,.20f),11);
        }
        b.Box(new Vector3(0,.72f,0),new Vector3(.20f,.25f,.25f),6);
    }
}
