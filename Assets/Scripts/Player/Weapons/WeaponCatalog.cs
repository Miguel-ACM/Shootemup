using System;
using System.Collections.Generic;
using UnityEngine;

public static class WeaponCatalog
{
    static readonly Dictionary<string, Type> types = new Dictionary<string, Type>(StringComparer.Ordinal) {
        { nameof(DepthCharge), typeof(DepthCharge) },
        { nameof(Backdraft), typeof(Backdraft) },
        { nameof(WindUpCannon), typeof(WindUpCannon) },
        { nameof(NeedleCannon), typeof(NeedleCannon) },
        { nameof(BurstLauncher), typeof(BurstLauncher) },
        { nameof(BeamEmitter), typeof(BeamEmitter) },
        { nameof(DiscThrower), typeof(DiscThrower) },
        { nameof(SwarmRack), typeof(SwarmRack) },
        { nameof(ScatterCannon), typeof(ScatterCannon) },
        { nameof(FlameProjector), typeof(FlameProjector) },
        { nameof(RailDriver), typeof(RailDriver) },
        { nameof(Drill), typeof(Drill) },
    };
    public static AWeapon Create(string key, Transform parent)
    {
        // Original weapon prefabs retain their serialized art and settings.
        var prefab = Resources.Load<GameObject>("Weapons/" + key);
        if (prefab != null)
            return UnityEngine.Object.Instantiate(prefab, parent).GetComponent<AWeapon>();
        if (!types.TryGetValue(key, out var type))
        {
            Debug.LogError("Unknown weapon: " + key);
            return null;
        }
        var go = new GameObject(key);
        go.transform.SetParent(parent, false);
        return (AWeapon)go.AddComponent(type);
    }
}
