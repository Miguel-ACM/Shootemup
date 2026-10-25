using UnityEngine;

// One hostile hue per district; silhouettes and timing still identify the different attacks.
public static class LevelProjectilePalette
{
    static readonly Material[,] shared=new Material[7,2];
    public static Color Accent(LevelId level)
    {
        switch(level)
        {
            case LevelId.Fields:return new Color(1,.28f,.20f);
            case LevelId.Snow:return new Color(.88f,.10f,.22f);
            case LevelId.City:return new Color(1,.77f,.16f);
            case LevelId.Thunder:return new Color(1,.65f,.12f);
            case LevelId.Tunnel:return new Color(1,.23f,.59f);
            case LevelId.Desert:return new Color(.39f,.24f,1);
            case LevelId.Candyland:return new Color(.65f,.95f,.12f);
            default:return new Color(1,.28f,.20f);
        }
    }
    public static Material Material(bool neutral=false)
    {
        int level=(int)LevelRun.Current,variant=neutral ? 1 : 0;
        if(shared[level,variant]!=null)return shared[level,variant];
        var material=new Material(CombatVisuals.ProjectileMaterial){name=LevelRun.Current+" shared hostile ink"};
        material.SetColor("_SpriteTint",Accent(LevelRun.Current));material.SetFloat("_Neutralize",neutral ? 1 : 0);
        material.SetFloat("_Burn",0);material.SetFloat("_Poison",0);return shared[level,variant]=material;
    }
}
