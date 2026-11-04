using UnityEngine;

// Sculpted pearl, aged gold, dark recesses and restrained luminous emitters.
public static class HeavenMaterials
{
    static Material material;
    public static Material Ivory=>material!=null?material:material=new Material(Resources.Load<Shader>("HeavenSurface")){name="Pearl ceramic and brushed celestial gold"};
}
