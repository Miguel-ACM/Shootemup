using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class WaveUnit : ICloneable
{
    public float time;
    public string enemy;
    public Vector3 position;
    public FlightPhrase phrase;
    public FlightCurve curve;
    public bool syncFire;
    public bool miniboss;
    // A tougher, ringed ship with an extra accent and a coin burst (WaveComposer.MarkElites).
    public bool elite;
    // ELITE HUNT: this ship carries the hunt's mark (chosen when the wave starts).
    public bool huntTarget;
    public int coreBankTiles;
    public int passage;
    public bool passageRoute;
    public float firePhase;
    public float[] fireTimes;
    public float formationDepth;
    public bool fixedDepth;
    public float faradayDoor;
    public string part;
    public float side = 1, lane, flightDuration = 6, fireEvery = .8f;
    public int HullCount => enemy == nameof(TransitCube) ? 0 : enemy == nameof(FaradayPylon) ? 2 : 1;

    public WaveUnit(string _enemy, float _time, Vector3 _position)
    {
        enemy = _enemy;
        time = _time;
        position = _position;
    }

    public object Clone()
    {
        return new WaveUnit(enemy, time, position) { phrase = phrase, curve = curve, syncFire = syncFire, miniboss = miniboss, elite = elite, huntTarget = huntTarget, coreBankTiles = coreBankTiles, passage = passage, passageRoute = passageRoute, firePhase = firePhase, fireTimes = fireTimes, formationDepth = formationDepth, fixedDepth = fixedDepth, faradayDoor = faradayDoor, part = part, side = side, lane = lane, flightDuration = flightDuration, fireEvery = fireEvery };
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
