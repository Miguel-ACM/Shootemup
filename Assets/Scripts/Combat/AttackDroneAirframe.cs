using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildAttackDroneAirframe(Builder b)
    {

        b.Drum(Vector3.zero,.29f,.25f,11,6);b.Drum(new Vector3(0,0,-.2f),.22f,.17f,12,6);
        for(int i=0;i<3;i++)
        {
            float a=(30+i*120)*Mathf.Deg2Rad;var p=V(Mathf.Cos(a),Mathf.Sin(a));var q=V(-p.y,p.x);
            Armor(b,new[]{p*.2f-q*.1f,p*.85f-q*.12f,p*1.02f,p*.85f+q*.12f,p*.2f+q*.1f},.16f,12);
            b.Drum((Vector3)(p*.7f)+Vector3.back*.17f,.09f,.1f,10,6);
        }
        b.Drum(new Vector3(0,0,-.32f),.09f,.07f,9,6);foreach(int s in new[]{-1,1})Motor(b,s*.32f,.4f,.18f,.46f,11);
    }
}
