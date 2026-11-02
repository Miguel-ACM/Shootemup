using UnityEngine;

public static class SpaceBlackHoleVisual
{
    static Material body, core;
    public static Material Body => body != null ? body : body = Create(false);
    public static Material Core => core != null ? core : core = Create(true);
    static Material Create(bool center)
    {
        var material = new Material(Resources.Load<Shader>("SpaceBlackHole")) { name = center ? "Opaque event horizon" : "Flowing accretion disk" };
        material.SetVector("_Extent", center ? new Vector4(.76f,.76f,0,0) : new Vector4(3,2,0,0));
        material.SetFloat("_CoreOnly", center ? 1 : 0);
        return material;
    }
}
