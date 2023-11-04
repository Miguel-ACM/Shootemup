using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;


public class Wave : ICloneable
{
    // Start is called before the first frame update
    public List<WaveUnit> units;
    public int cost;
    public int weight;
    public ConflictGroup.cg cg;
    public float duration;
    int currEnemy = 0;
    static System.Random rng = new System.Random();
   
    public Wave(int _cost, float _duration, int _weight,  ConflictGroup.cg _conflictGroup, List<WaveUnit> _units)
    {
        units = _units;
        cost = _cost;
        cg = _conflictGroup;
        duration = _duration;
        weight = _weight;
    }

    public WaveUnit getNext()
    {
        if (currEnemy < units.Count)
        {
            currEnemy += 1;
            return units[currEnemy - 1];
        }
        return null;
    }

    public static Wave CombineWaves(List<Wave> waves)
    {
        if (waves.Count == 1)
        {
            return (Wave) waves[0].Clone();
        }
        List<WaveUnit> newUnits = new List<WaveUnit>();
        int newCost = 0;
        float duration = 0;
        float addedTime = 0;
        for (int i = 0; i < waves.Count; i++)
        {
            if (i > 0) addedTime = rng.Next(0, 9) * 0.5f;
            foreach (WaveUnit u in waves[i].units)
            {
                WaveUnit newUnit = (WaveUnit)u.Clone();
                newUnit.time += addedTime;
                newUnits.Add(newUnit);
            }
            
            if (waves[i].duration + addedTime > duration )
            {
                duration = waves[i].duration + addedTime;
            }

            newCost += waves[i].cost;
        }

        newUnits.OrderBy(x => x.time);

        return new Wave(newCost, duration, 0, 0, newUnits);
    }

    override public string ToString()
    {
        string str = string.Format("Wave: duration: {0}\tcost: {1}\tconflictGroup: {2}\n", duration, cost, cg);
        foreach (WaveUnit u in units)
        {
            str += "\t" + u.ToString() + "\n";
        }
        return str;
    }

    public object Clone()
    {
        List<WaveUnit> newUnits = new List<WaveUnit>();
        foreach (WaveUnit u in units)
        {
            newUnits.Add((WaveUnit) u.Clone());
        }
        return new Wave(cost, duration, weight, cg, newUnits);
    }
}
