using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class WaveUnit : ICloneable
{
    public float time;
    public string enemy;
    public Vector3 position;

    public WaveUnit(string _enemy, float _time, Vector3 _position)
    {
        enemy = _enemy;
        time = _time;
        position = _position;
    }

    public object Clone()
    {
        return new WaveUnit(enemy, time, new Vector3(position.x, position.y));
    }

    override public string ToString()
    {
        return string.Format("WaveUnit: enemy: {0}\ttime: {1}\tposition: {2}", enemy, time, position.ToString());
    }

    public GameObject GetUnit()
    {
        return Resources.Load<GameObject>("Enemy/" + enemy);
    }
}
