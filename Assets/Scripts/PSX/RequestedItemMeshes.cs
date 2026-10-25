using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static bool RequestedItem(Builder b,string kind)
    {
        switch(kind)
        {
            case "HealthPacks":
                foreach(int side in new[]{-1,1})
                {
                    var p=new Vector3(side*.2f,side*.08f,side*.07f);
                    b.Box(p,new Vector3(.34f,.48f,.22f),1);
                    b.Box(p+Vector3.back*.13f,new Vector3(.23f,.07f,.025f),3);
                    b.Box(p+Vector3.back*.13f,new Vector3(.07f,.23f,.025f),3);
                    b.Box(p+Vector3.up*.27f,new Vector3(.13f,.06f,.15f),6);
                }
                return true;
            case "EnergyCore":
                b.Ball(Vector3.zero,new Vector3(.23f,.33f,.23f),9);
                foreach(int side in new[]{-1,1})
                {
                    b.Box(new Vector3(side*.3f,0,0),new Vector3(.1f,.46f,.3f),6);
                    b.Box(new Vector3(0,side*.36f,0),new Vector3(.56f,.13f,.35f),11);
                    b.Box(new Vector3(0,side*.42f,0),new Vector3(.18f,.08f,.2f),13);
                }
                b.Drum(new Vector3(0,0,-.25f),.1f,.05f,1,8);return true;
            case "DoubleTrouble":
                foreach(int side in new[]{-1,1})
                {
                    b.Box(new Vector3(side*.23f,0,0),new Vector3(.22f,.57f,.22f),6);
                    b.Box(new Vector3(side*.23f,.32f,0),new Vector3(.27f,.12f,.27f),11);
                    b.Box(new Vector3(side*.23f,-.28f,.04f),new Vector3(.16f,.22f,.18f),3);
                }
                b.Box(new Vector3(0,-.08f,0),new Vector3(.3f,.12f,.14f),13);return true;
            case "FocusTime":
                b.Box(new Vector3(0,-.05f,0),new Vector3(.82f,.12f,.48f),13,2);
                foreach(int side in new[]{-1,1})b.Box(new Vector3(side*.3f,-.32f,.1f),new Vector3(.09f,.48f,.09f),2);
                b.Box(new Vector3(.06f,.08f,-.05f),new Vector3(.43f,.08f,.31f),7);
                b.Box(new Vector3(.06f,.14f,-.05f),new Vector3(.39f,.05f,.29f),1);
                b.Box(new Vector3(.06f,.17f,-.05f),new Vector3(.02f,.025f,.3f),13);
                b.Box(new Vector3(.17f,.17f,-.14f),new Vector3(.035f,.025f,.16f),3);
                b.Drum(new Vector3(-.28f,.15f,.04f),.07f,.12f,6,8);return true;
            case "BouncingBall":
                b.Ball(Vector3.zero,new Vector3(.33f,.33f,.33f),3);
                b.Drum(new Vector3(0,0,-.29f),.15f,.08f,10,12);
                b.Box(new Vector3(0,0,-.34f),new Vector3(.035f,.25f,.025f),1);
                b.Box(new Vector3(0,0,-.34f),new Vector3(.25f,.035f,.025f),1);return true;
            case "BarrelRoll":
                b.Prism(new[]{new Vector2(0,.36f),new Vector2(.32f,-.15f),new Vector2(.08f,-.09f),new Vector2(0,-.25f),new Vector2(-.08f,-.09f),new Vector2(-.32f,-.15f)},.13f,1,Vector3.zero,6);
                for(int i=0;i<10;i++){float a=i*Mathf.PI/6;var p=new Vector3(Mathf.Cos(a)*.43f,Mathf.Sin(a)*.43f,.02f);b.Box(p,new Vector3(.1f,.09f,.1f),9);}
                b.Prism(new[]{new Vector2(-.16f,-.36f),new Vector2(.07f,-.48f),new Vector2(-.12f,-.57f)},.12f,9,Vector3.zero);return true;
            case "Poison":
                b.Prism(new[]{new Vector2(-.32f,-.32f),new Vector2(.32f,-.32f),new Vector2(.34f,-.2f),new Vector2(.1f,.22f),new Vector2(.1f,.39f),new Vector2(-.1f,.39f),new Vector2(-.1f,.22f),new Vector2(-.34f,-.2f)},.26f,2,Vector3.zero,2);
                b.Box(new Vector3(0,.43f,0),new Vector3(.23f,.1f,.29f),13);
                b.Drum(new Vector3(0,-.12f,-.15f),.13f,.025f,1,8);
                foreach(int side in new[]{-1,1})b.Drum(new Vector3(side*.048f,-.1f,-.17f),.035f,.02f,11,6);
                b.Box(new Vector3(0,-.2f,-.17f),new Vector3(.09f,.04f,.02f),11);
                b.Box(new Vector3(-.16f,.01f,-.15f),new Vector3(.025f,.12f,.02f),14);return true;
            case "GasolineLeak":
                b.Prism(new[]{new Vector2(-.27f,-.34f),new Vector2(.27f,-.34f),new Vector2(.27f,.23f),new Vector2(.12f,.34f),new Vector2(-.27f,.23f)},.3f,3,Vector3.zero,12);
                b.Box(new Vector3(-.16f,.37f,0),new Vector3(.13f,.12f,.22f),11);
                b.Box(new Vector3(.14f,.36f,0),new Vector3(.24f,.06f,.12f),6);
                b.Box(new Vector3(.25f,.3f,0),new Vector3(.04f,.16f,.12f),6);
                b.Box(new Vector3(0,0,-.17f),new Vector3(.36f,.045f,.025f),13);
                b.Box(new Vector3(0,0,-.17f),new Vector3(.045f,.38f,.025f),13);
                b.Ball(new Vector3(-.29f,-.43f,0),new Vector3(.06f,.1f,.06f),3);return true;
            case "BadGlasses":
                Glasses(b,true);return true;
            case "QualityGlasses":
                Glasses(b,false);return true;
            case "EmpPulse":
                b.Drum(Vector3.zero,.3f,.2f,11,12,6);
                b.Drum(new Vector3(0,0,-.13f),.2f,.06f,9,12);
                b.Drum(new Vector3(0,0,-.18f),.12f,.06f,1,8);
                for(int i=0;i<8;i++){float a=i*Mathf.PI/4;var p=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);b.Box(p*.37f,new Vector3(.11f,.11f,.16f),9);}
                b.Box(new Vector3(0,.49f,0),new Vector3(.07f,.2f,.08f),6);
                return true;
            case "ShrinkerJuice":
                b.Prism(new[]{new Vector2(-.21f,-.37f),new Vector2(.21f,-.37f),new Vector2(.24f,.12f),new Vector2(.1f,.25f),new Vector2(.1f,.44f),new Vector2(-.1f,.44f),new Vector2(-.1f,.25f),new Vector2(-.24f,.12f)},.27f,2,Vector3.zero,2);
                b.Box(new Vector3(0,.45f,0),new Vector3(.24f,.12f,.31f),13);
                b.Box(new Vector3(0,-.08f,-.15f),new Vector3(.29f,.2f,.025f),1);
                b.Prism(new[]{new Vector2(-.09f,.06f),new Vector2(.09f,.06f),new Vector2(0,-.07f)},.02f,2,new Vector3(0,-.08f,-.18f));
                b.Box(new Vector3(-.14f,.08f,-.15f),new Vector3(.035f,.2f,.02f),14);
                return true;
        }
        return false;
    }
    static void Glasses(Builder b,bool broken)
    {
        foreach(int side in new[]{-1,1})
        {
            b.Drum(new Vector3(side*.23f,0,0),.22f,.1f,broken ? 11 : 13,8);
            b.Drum(new Vector3(side*.23f,0,-.065f),.16f,.025f,broken ? 8 : 9,8);
            b.Box(new Vector3(side*.43f,.06f,.16f),new Vector3(.045f,.08f,.36f),broken ? 11 : 13);
        }
        b.Box(new Vector3(0,.02f,-.02f),new Vector3(.15f,.045f,.06f),broken ? 11 : 13);
        if(broken){b.Box(new Vector3(.23f,0,-.09f),new Vector3(.018f,.28f,.015f),1);b.Box(new Vector3(.29f,.03f,-.09f),new Vector3(.13f,.018f,.015f),1);}
    }
}
