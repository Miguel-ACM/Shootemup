using UnityEngine;

public static partial class IndustrialMeshFactory
{
    static void BuildAbyssHunter(Builder b)
    {
        // Continuous pressure hull: an arched prow, broad shoulders and a tapered stern.
        float[] y={-2.13f,-1.90f,-1.36f,-.65f,.18f,.91f,1.48f,1.95f,2.13f};
        float[] width={.16f,.59f,.99f,1.03f,.86f,.72f,.57f,.31f,.12f};
        float[] depth={.13f,.31f,.49f,.56f,.53f,.43f,.32f,.22f,.10f};
        Vector3 P(int row,int side){float a=side*Mathf.PI/6;return new Vector3(Mathf.Cos(a)*width[row],y[row],Mathf.Sin(a)*depth[row]);}
        for(int row=0;row<y.Length-1;row++)for(int side=0;side<12;side++)
        {
            var face=new[]{P(row,side),P(row,side+1),P(row+1,side+1),P(row+1,side)};
            var normal=Vector3.Cross(face[1]-face[0],face[2]-face[0]);var middle=(face[0]+face[1]+face[2]+face[3])*.25f;middle.y=0;
            if(Vector3.Dot(normal,middle)<0)System.Array.Reverse(face);
            b.Face(row<2 ? 4 : row==6 ? 6 : 0,face);
        }
        for(int s=-1;s<=1;s+=2)
        {
            // Swept ivory shoulders are one shaped shell, not a row of deck buttons.
            b.Prism(new[]{new Vector2(s*.63f,.27f),new Vector2(s*1.31f,-.43f),new Vector2(s*1.32f,-1.11f),new Vector2(s*.84f,-1.67f),new Vector2(s*.54f,-1.41f),new Vector2(s*.88f,-.81f)},.24f,1,new Vector3(0,0,-.29f),4);
            b.Prism(new[]{new Vector2(s*.65f,.52f),new Vector2(s*1.58f,.79f),new Vector2(s*1.45f,.12f),new Vector2(s*.76f,-.13f)},.13f,4,new Vector3(0,0,.03f),6);
            // Recessed launch channels run into the prow, with real barrel depth.
            for(int gun=0;gun<2;gun++)
            {
                float x=s*(.43f+gun*.29f);
                b.Box(new Vector3(x,-1.42f,-.45f),new Vector3(.23f,.64f,.21f),6,11);
                b.Box(new Vector3(x,-1.66f,-.58f),new Vector3(.14f,.26f,.05f),11);
            }
            for(int gill=0;gill<4;gill++)b.Box(new Vector3(s*.79f,-.44f+gill*.19f,-.41f),new Vector3(.24f,.065f,.07f),11);
            b.Box(new Vector3(s*.65f,1.35f,-.05f),new Vector3(.47f,1.08f,.34f),8,11);
            b.Drum(new Vector3(s*.65f,1.49f,-.27f),.34f,.12f,6,10);
            b.Drum(new Vector3(s*.65f,1.49f,-.36f),.27f,.07f,11,10);
            for(int blade=0;blade<3;blade++)
            {
                float a=blade*Mathf.PI*2/3;Vector2 centre=new Vector2(s*.65f,1.49f);
                Vector2 R(float radius,float turn)=>centre+new Vector2(Mathf.Cos(a+turn),Mathf.Sin(a+turn))*radius;
                b.Prism(new[]{R(.05f,0),R(.24f,.25f),R(.25f,.72f),R(.10f,.83f)},.045f,4,new Vector3(0,0,-.42f));
            }
            b.Drum(new Vector3(s*.65f,1.49f,-.45f),.085f,.06f,13,8);
            b.Prism(new[]{new Vector2(s*.24f,1.56f),new Vector2(s*.66f,2.10f),new Vector2(s*.39f,2.30f),new Vector2(s*.11f,1.93f)},.11f,4,Vector3.zero,6);
        }
        // Compact raised sail with a dark visor recessed below its armored brow.
        b.Prism(new[]{new Vector2(-.32f,.94f),new Vector2(-.38f,.25f),new Vector2(-.21f,-.28f),new Vector2(0,-.44f),new Vector2(.21f,-.28f),new Vector2(.38f,.25f),new Vector2(.32f,.94f),new Vector2(0,1.15f)},.27f,4,new Vector3(0,0,-.60f),11);
        b.Ball(new Vector3(0,.42f,-.78f),new Vector3(.29f,.58f,.19f),8);
        b.Box(new Vector3(0,.67f,-.96f),new Vector3(.45f,.12f,.06f),11);
        b.Box(new Vector3(0,.67f,-1.0f),new Vector3(.34f,.055f,.035f),9);
        b.Box(new Vector3(-.11f,.98f,-1.0f),new Vector3(.065f,.16f,.31f),6);
        b.Box(new Vector3(.12f,.92f,-.99f),new Vector3(.055f,.13f,.21f),6);
        b.Prism(new[]{new Vector2(-.18f,-.59f),new Vector2(0,-1.32f),new Vector2(.18f,-.59f)},.13f,6,new Vector3(0,0,-.56f),8);
    }
}
