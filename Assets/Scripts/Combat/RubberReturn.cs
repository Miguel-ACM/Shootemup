using UnityEngine;
public static class RubberReturn
{
    public static bool AtTop(Vector3 p)
    {
        var camera=UnityEngine.Camera.main;
        float top=camera!=null ? camera.transform.position.y+camera.orthographicSize-.2f : Camera.UpLimit-.2f;
        return p.y>=top && CombatUpgrades.Current!=null && CombatUpgrades.Current.Count(UpgradeKind.RubberBand)>0;
    }
}
