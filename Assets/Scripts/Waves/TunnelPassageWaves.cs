using System.Collections.Generic;
using UnityEngine;

public static class TunnelPassageWaves
{
    public static bool HasPassage(int budget) => budget==6||budget==10||budget==16;
    public static Wave Create(int budget, int previewLayout = 0)
    {
        int layout=budget==6?1:budget==10?2:3;
        if(((LevelRun.Seed >> (layout-1))&1)!=0)layout+=3;
        if(previewLayout>0)layout=previewLayout;
        var units=new List<WaveUnit>();
        string[] actors={nameof(MaintenanceDrone),budget==6?nameof(BaffleSpinner):nameof(RivetMantis),nameof(WallTurret),nameof(BaffleSpinner)};
        float tempo=WaveComposer.Tempo;
        for(int phrase=0;phrase<4;phrase++)
        {
            float lane=(phrase==1?-1:phrase==2?1:0)*.55f;
            var curve=new FlightCurve("Tunnel passage / "+phrase,Vector2.right,0,
                lane/Camera.RightLimit,1.24f,
                lane*.85f/Camera.RightLimit,.85f,
                -lane*.65f/Camera.RightLimit,.5f,
                lane*.7f/Camera.RightLimit,.12f,
                lane/Camera.RightLimit,-.24f);
            int count=phrase==2?2:3;
            float start=phrase*2.5f;
            for(int i=0;i<count;i++)
                units.Add(new WaveUnit(actors[phrase],(start+i*.72f)/tempo,Vector3.zero){
                    phrase=FlightPhrase.Flow,curve=curve,flightDuration=7.5f,side=1,syncFire=true,firePhase=i*.72f,
                    fireTimes=new[]{2.1f,3.4f,4.8f,6.1f},passageRoute=true,
                    part="Solid passage / "+actors[phrase],passage=phrase==0&&i==0?layout:0
                });
        }
        return new Wave(budget,16.5f/tempo,1,ConflictGroup.cg.center,units){parts=new[]{"Solid passage","Threaded procession","Crossing baffles","Rail battery","Exit spinners"}};
    }
}
