using System.Collections.Generic;
using UnityEngine;

// Compose routes into trains, ranks and answering pairs on a shared wave timeline.
public sealed class WaveDance
{
    readonly List<WaveUnit> units=new List<WaveUnit>();
    readonly int budget;
    public readonly float Side;
    public readonly int Extra;
    public readonly int Phrase;
    public float Handoff=4.2f;
    public WaveDance(int budget)
    {
        this.budget=budget;int index=Mathf.Max(0,budget-2);
        // Each eight-wave passage visits every composition; later passages reverse it.
        Phrase=index%8;if(index/8%2==1)Phrase=(11-Phrase)%8;
        Side=((LevelRun.Seed+index+(int)LevelRun.Current)&1)==0 ? 1 : -1;
        Extra=Mathf.Min(2,LevelRun.Stage-1);
    }
    public void Solo(string enemy,FlightCurve route,float at=0,float side=1,float lane=0,float seconds=4.8f,float beat=1.1f,bool together=false)
    {
        units.Add(new WaveUnit(enemy,at,Vector3.zero){phrase=FlightPhrase.Flow,curve=route,side=side,lane=lane,
            flightDuration=seconds,fireEvery=beat,syncFire=together,firePhase=at});
    }
    public void Train(string enemy,FlightCurve route,float at,float side,int count,float spacing=.3f,float seconds=4.8f,float lane=0,float beat=1.1f,bool together=false)
    {
        if(enemy=="ScoutInterceptor")spacing=Mathf.Max(.38f,spacing);
        for(int i=0;i<count;i++)Solo(enemy,route,at+i*spacing,side,lane,seconds,beat,together);
    }
    public void Rank(string enemy,FlightCurve route,float at,float side,int count,float width=.25f,float seconds=4.8f,float beat=1.2f)
    {
        for(int i=0;i<count;i++)Solo(enemy,route,at,side,(i-(count-1)*.5f)*width,seconds,beat,true);
    }
    public Wave Build()
    {
        units.Sort((a,b)=>a.time.CompareTo(b.time));
        return new Wave(budget,Handoff,1,ConflictGroup.cg.center,units);
    }
}
