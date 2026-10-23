using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildPursuitAirframe(Builder b)
    {

        Armor(b,new[]{V(0,-1.35f),V(.17f,-.48f),V(1.05f,.74f),V(.72f,.58f),V(.24f,.09f),V(.21f,.65f),V(-.21f,.65f),V(-.24f,.09f),V(-.72f,.58f),V(-1.05f,.74f),V(-.17f,-.48f)},.18f,12);
        Armor(b,new[]{V(-.17f,.5f),V(.17f,.5f),V(.12f,-.55f),V(0,-1.12f),V(-.12f,-.55f)},.22f,11,-.16f);
        Glass(b,0,-.35f,.21f,.48f,-.33f);
        foreach(int s in new[]{-1,1}){Motor(b,s*.43f,.59f,.23f,.64f,12);Barrel(b,s*.65f,.1f,.55f,.08f);}
    }
}
