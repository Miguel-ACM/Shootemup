using UnityEngine;

public static partial class IndustrialMeshFactory
{
    static void BuildDistrictMini(Builder b,int kind)
    {
        if(kind==56)
        {
            b.Ball(Vector3.zero,new Vector3(1.05f,1.25f,.48f),13);
            for(int row=0;row<4;row++)for(int s=-1;s<=1;s+=2)
                b.Prism(new[]{new Vector2(s*.08f,.90f-row*.45f),new Vector2(s*.82f,.92f-row*.45f),new Vector2(s*.95f,.52f-row*.45f),new Vector2(s*.08f,.46f-row*.45f)},.16f,2,new Vector3(0,0,-.45f),11);
            for(int s=-1;s<=1;s+=2)
            {
                for(int j=0;j<3;j++)b.Prism(new[]{new Vector2(s*.83f,.63f-j*.55f),new Vector2(s*1.55f,.32f-j*.52f),new Vector2(s*1.25f,-.18f-j*.52f),new Vector2(s*1.14f,.18f-j*.52f)},.20f,6,Vector3.zero);
                b.Box(new Vector3(s*.70f,-.72f,-.35f),new Vector3(.27f,1.0f,.25f),11);
                b.Drum(new Vector3(s*.70f,-1.12f,-.39f),.19f,.19f,10,8);
            }
            b.Box(new Vector3(0,-.72f,-.57f),new Vector3(.46f,.34f,.15f),9);return;
        }
        if(kind==57)
        {
            b.Drum(Vector3.zero,.69f,.35f,1,12,12);b.Drum(new Vector3(0,0,-.23f),.49f,.14f,10,12);
            for(int s=-1;s<=1;s+=2)
            {
                b.Prism(new[]{new Vector2(s*.42f,.61f),new Vector2(s*1.34f,1.05f),new Vector2(s*1.22f,-.76f),new Vector2(s*.58f,-1.01f)},.26f,12,Vector3.zero,13);
                for(int j=0;j<5;j++)b.Box(new Vector3(s*.90f,.65f-j*.28f,-.19f),new Vector3(.44f,.10f,.10f),1);
                b.Drum(new Vector3(s*.78f,-.65f,-.37f),.23f,.19f,9,10);
            }
            b.Prism(new[]{new Vector2(0,-1.32f),new Vector2(-.28f,-.45f),new Vector2(.28f,-.45f)},.20f,6,Vector3.zero);return;
        }
        if(kind==58)
        {
            b.Ball(new Vector3(0,.32f,0),new Vector3(1.13f,.98f,.50f),8);
            b.Drum(new Vector3(0,.38f,-.45f),.50f,.15f,13,12);b.Drum(new Vector3(0,.38f,-.56f),.33f,.16f,9,12);
            for(int i=-2;i<=2;i++)for(int j=0;j<3;j++)
            {
                float x=i*.40f+Mathf.Sin(j*1.4f+i)*.12f;
                b.Box(new Vector3(x,-.42f-j*.46f,-.05f),new Vector3(.24f,.56f,.22f),j%2==0 ? 6 : 13);
                b.Drum(new Vector3(x,-.65f-j*.46f,-.15f),.16f,.11f,11,8);
            }
            for(int s=-1;s<=1;s+=2)b.Box(new Vector3(s*1.04f,.18f,-.25f),new Vector3(.30f,1.1f,.35f),13);return;
        }
        if(kind==59)
        {
            b.Prism(new[]{new Vector2(0,-1.55f),new Vector2(-.72f,-.71f),new Vector2(-.83f,1.0f),new Vector2(0,1.32f),new Vector2(.83f,1.0f),new Vector2(.72f,-.71f)},.51f,11,Vector3.zero,5);
            for(int s=-1;s<=1;s+=2)
            {
                b.Prism(new[]{new Vector2(s*.13f,1.19f),new Vector2(s*1.22f,.63f),new Vector2(s*.91f,-.27f),new Vector2(s*.55f,-.10f),new Vector2(s*.13f,-.39f)},.08f,15,new Vector3(0,0,-.75f),6);
                for(int j=0;j<3;j++){b.Box(new Vector3(s*.87f,.55f-j*.54f,-.28f),new Vector3(.40f,.28f,.27f),6);b.Drum(new Vector3(s*.94f,.55f-j*.54f,-.43f),.15f,.14f,10,8);}
                b.Ball(new Vector3(s*.24f,-.88f,-.55f),new Vector3(.17f,.19f,.12f),9);
            }
            b.Box(new Vector3(0,-.58f,-.40f),new Vector3(.70f,.9f,.30f),1);return;
        }
        b.Prism(new[]{new Vector2(0,-1.52f),new Vector2(-.57f,-.22f),new Vector2(-.38f,1.13f),new Vector2(.38f,1.13f),new Vector2(.57f,-.22f)},.38f,1,Vector3.zero,11);
        for(int s=-1;s<=1;s+=2)
        {
            b.Box(new Vector3(s*.98f,.12f,0),new Vector3(1.28f,1.78f,.11f),8);
            for(int j=0;j<5;j++)b.Box(new Vector3(s*.98f,-.58f+j*.34f,-.09f),new Vector3(1.16f,.04f,.06f),6);
            b.Box(new Vector3(s*.96f,-.91f,-.22f),new Vector3(.32f,.55f,.36f),11);
            b.Drum(new Vector3(s*.96f,-1.08f,-.40f),.17f,.12f,9,8);
        }
        b.Drum(new Vector3(0,.16f,-.31f),.41f,.19f,13,10);b.Drum(new Vector3(0,.16f,-.45f),.25f,.12f,9,10);
    }
}
