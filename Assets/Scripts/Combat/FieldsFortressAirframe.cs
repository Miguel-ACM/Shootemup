using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildFieldsFortressAirframe(Builder b)
    {
        BevelArmor(b,new[]{V(-1.7f,.5f),V(-.58f,1.1f),V(0,.8f),V(.58f,1.1f),V(1.7f,.5f),V(1.36f,-.4f),V(.7f,-.72f),V(0,-.32f),V(-.7f,-.72f),V(-1.36f,-.4f)},.39f,2);
        BevelArmor(b,new[]{V(-.4f,.8f),V(.4f,.8f),V(.57f,-.3f),V(.28f,-.95f),V(0,-1.13f),V(-.28f,-.95f),V(-.57f,-.3f)},.55f,0,-.13f);
        BevelArmor(b,new[]{V(-.35f,.06f),V(.35f,.06f),V(.36f,-.39f),V(-.36f,-.39f)},.17f,6,-.46f);
        b.Box(new Vector3(0,-.25f,-.59f),new Vector3(.53f,.12f,.04f),11,11);
        b.Box(new Vector3(0,-.25f,-.62f),new Vector3(.43f,.055f,.025f),9,11);
        foreach(int s in new[]{-1,1})
        {
            Motor(b,s*1.04f,.35f,.43f,.74f,2);
            BevelArmor(b,new[]{V(s*.6f,-.1f),V(s*1.47f,.1f),V(s*1.3f,-.57f),V(s*.75f,-.9f)},.25f,6,-.24f);
            BevelArmor(b,new[]{V(s*.53f,.55f),V(s*.8f,.81f),V(s*.81f,.2f),V(s*.55f,.05f)},.11f,0,-.25f);
            for(int i=0;i<3;i++)
            {
                float x=s*(.75f+i*.22f),y=-.48f+i*.12f;
                b.Drum(new Vector3(x,y,-.42f),.1f,.14f,11,8,6);b.Drum(new Vector3(x,y,-.51f),.055f,.045f,3,8);
            }
            Barrel(b,s*1.4f,-.35f,.6f,.16f,-.13f);
        }
        Inlet(b,0,.42f,.47f,.38f,-.43f,0);
        Armor(b,new[]{V(-.16f,-.62f),V(.16f,-.62f),V(0,-.93f)},.04f,2,-.43f);
    }
}
