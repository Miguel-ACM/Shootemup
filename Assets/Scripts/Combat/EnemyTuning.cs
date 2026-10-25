using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[Serializable]
public sealed class EnemyValueOverride
{
    public string field;
    public bool enabled;
    public float value;
}
[Serializable]
public sealed class EnemyTuningProfile
{
    public string enemy;
    public bool enabled=true;
    public float movement=1,projectileSpeed=1,projectileDamage=1;
    public bool useFormationScore;
    public float[] formationAccents={.31f,.46f,.62f};
    public List<EnemyValueOverride> values=new List<EnemyValueOverride>();
}

[CreateAssetMenu(menuName="Shootemup/Enemy Tuning")]
public sealed class EnemyTuning : ScriptableObject
{
    public List<EnemyTuningProfile> enemies=new List<EnemyTuningProfile>();
    static EnemyTuning current;
    static int revision;
    static readonly Dictionary<Type,FieldInfo[]> fields=new Dictionary<Type,FieldInfo[]>();
    public static EnemyTuning Current {get{if(current==null)current=Resources.Load<EnemyTuning>("EnemyTuning");return current;}}
    public static int Revision=>revision;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset(){current=null;revision++;}
    void OnValidate(){revision++;}
    public void Changed(){revision++;}
    public EnemyTuningProfile Find(string enemy)
    {
        foreach(var profile in enemies)if(profile.enemy==enemy)return profile;
        return null;
    }
    public static EnemyTuningProfile For(string enemy)
    {
        var result=Current!=null ? Current.Find(enemy) : null;return result!=null && result.enabled ? result : null;
    }
    public static float MovementFor(string enemy)=>Mathf.Clamp(For(enemy)?.movement ?? 1,.35f,2.5f);
    public static FieldInfo[] Fields(Type type)
    {
        if(fields.TryGetValue(type,out var result))return result;
        var list=new List<FieldInfo>();
        for(var t=type;t!=null && t!=typeof(MonoBehaviour);t=t.BaseType)
            foreach(var field in t.GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly))
                if((field.FieldType==typeof(float) || field.FieldType==typeof(int)) && Attribute.IsDefined(field,typeof(EnemyTunableAttribute)))list.Add(field);
        return fields[type]=list.ToArray();
    }
    public static void Apply(DistrictEnemy enemy,PatternMover mover)
    {
        var profile=For(enemy.GetType().Name);if(profile==null)return;
        ApplyValues(enemy,profile,"");if(mover!=null)ApplyValues(mover,profile,"mover.");
        enemy.SetTuning(Mathf.Clamp(profile.movement,.35f,2.5f),Mathf.Clamp(profile.projectileSpeed,.25f,3),Mathf.Clamp(profile.projectileDamage,.1f,5));
    }
    static void ApplyValues(object target,EnemyTuningProfile profile,string prefix)
    {
        foreach(var field in Fields(target.GetType()))foreach(var value in profile.values)
        {
            if(!value.enabled || value.field!=prefix+field.Name || !float.IsFinite(value.value))continue;
            var range=(EnemyTunableAttribute)Attribute.GetCustomAttribute(field,typeof(EnemyTunableAttribute));
            float number=Mathf.Clamp(value.value,range.Minimum,range.Maximum);
            if(field.FieldType==typeof(int))field.SetValue(target,Mathf.RoundToInt(number));else field.SetValue(target,number);
            break;
        }
    }
    public static void TuneFormation(string enemy,List<WaveUnit> units,float seconds,ref float[] score)
    {
        var profile=For(enemy);if(profile==null)return;
        float rate=Mathf.Clamp(profile.movement,.35f,2.5f);
        if(profile.useFormationScore && profile.formationAccents!=null && profile.formationAccents.Length>0)
        {
            score=new float[profile.formationAccents.Length];
            for(int i=0;i<score.Length;i++)score[i]=Mathf.Clamp(profile.formationAccents[i],.15f,.78f)*seconds;
            Array.Sort(score);
        }
        if(Mathf.Approximately(rate,1))return;
        var scaled=new float[score.Length];for(int i=0;i<score.Length;i++)scaled[i]=score[i]/rate;score=scaled;
        foreach(var unit in units){unit.time/=rate;unit.firePhase/=rate;unit.flightDuration/=rate;}
    }
}
