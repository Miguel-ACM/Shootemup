using System;

// Pure route/difficulty checks, also runnable without starting the game.
public static class VerifyRunProgression
{
    public static void Run()
    {
        try
        {
            Check((int)LevelId.Fields==0 && (int)LevelId.Tunnel==1 && (int)LevelId.Snow==2 &&
                (int)LevelId.Thunder==3 && (int)LevelId.City==4,"Serialized district IDs changed");
            foreach(var middle in new[]{LevelId.Snow,LevelId.City})
            foreach(var final in new[]{LevelId.Thunder,LevelId.Tunnel})
            {
                LevelRun.Reset();int seed=LevelRun.Seed;
                Check(LevelRun.Current==LevelId.Fields && LevelRun.Stage==1 && LevelRun.Loop==1,"New run must start at Plains");
                Check(LevelRun.EnemyHealthScale==1 && LevelRun.EnemyTempoScale==1 && LevelRun.EnemyShotSpeedScale==1,"Fresh run retained difficulty");
                float previousHealth=0,previousTempo=0,previousShots=0;
                for(int loop=1;loop<=100;loop++)
                {
                    Check(LevelRun.Loop==loop && LevelRun.Stage==(loop-1)*3+1,"Loop or stage advanced incorrectly");
                    Check(LevelRun.EnemyHealthScale>previousHealth,"Health stopped scaling across loops");
                    Check(LevelRun.EnemyTempoScale>=previousTempo && LevelRun.EnemyShotSpeedScale>=previousShots,"Loop speeds decreased");
                    Check(LevelRun.EnemyTempoScale<=1.5f && LevelRun.EnemyShotSpeedScale<=1.4f,"Readability speed caps exceeded");
                    if(loop==2)Check(Near(LevelRun.EnemyHealthScale,1.66f) && Near(LevelRun.EnemyTempoScale,1.08f) && Near(LevelRun.EnemyShotSpeedScale,1.06f),"First loop escalation is incorrect");
                    previousHealth=LevelRun.EnemyHealthScale;previousTempo=LevelRun.EnemyTempoScale;previousShots=LevelRun.EnemyShotSpeedScale;
                    LevelRun.GetChoices(out var left,out var right);
                    Check(left==LevelId.Snow && right==LevelId.City,"Plains must offer Snow/City");
                    Reject(LevelId.Thunder);Reject(LevelId.Fields);
                    LevelRun.Advance(middle);
                    Check(LevelRun.EnemyHealthScale>previousHealth && LevelRun.Loop==loop,"Middle tier difficulty is incorrect");
                    LevelRun.GetChoices(out left,out right);
                    Check(left==LevelId.Thunder && right==LevelId.Tunnel,"Middle tier must offer Thunder/Tunnel");
                    Reject(LevelId.Fields);Reject(middle);
                    LevelRun.Advance(final);Check(!LevelRun.ReturnsToPlains,"Third district must offer the desert extension");
                    LevelRun.GetChoices(out left,out right);
                    Check(left==LevelId.Desert && right==LevelId.Fields,"Third district must offer Desert or Plains");Reject(LevelId.Snow);
                    LevelRun.Advance(LevelId.Fields);
                    Check(LevelRun.Seed==seed,"Route transition reset the run seed");
                }
            }
            LevelRun.Reset();
            for(int loop=1;loop<=50;loop++)
            {
                LevelRun.Advance(LevelId.City);LevelRun.Advance(LevelId.Tunnel);LevelRun.Advance(LevelId.Desert);
                Check(LevelRun.Loop==loop && LevelRun.Stage==loop*4,"Desert advanced loop early");
                Check(LevelRun.ReturnsToPlains,"Desert must finish the extended loop");
                Reject(LevelId.Desert);Reject(LevelId.Thunder);LevelRun.Advance(LevelId.Fields);
                Check(LevelRun.Loop==loop+1,"Extended loop did not increment exactly once");
            }
            foreach(var third in new[]{LevelId.Thunder,LevelId.Tunnel})
            {
                LevelRun.Reset();LevelRun.Advance(LevelId.Snow);LevelRun.Advance(third);
                var options=LevelRun.GetOptions();
                Check(options.Length==3 && options[0]==LevelId.Desert && options[1]==LevelId.Candyland && options[2]==LevelId.Fields,"Third district lost a route");
                int seed=LevelRun.Seed;LevelRun.Advance(LevelId.Candyland);
                Check(LevelRun.Stage==4 && LevelRun.ReturnsToPlains && LevelRun.Seed==seed,"Candyland route resets progression");
                Reject(LevelId.Desert);LevelRun.Advance(LevelId.Fields);
                Check(LevelRun.Stage==5 && LevelRun.Loop==2 && LevelRun.Seed==seed,"Candyland did not finish the loop");
            }
        }
        finally{LevelRun.Reset();}
        Check(LevelRun.Stage==1 && LevelRun.Loop==1 && LevelRun.EnemyTempoScale==1,"New run did not reset difficulty");
    }
    static bool Near(float a,float b)=>Math.Abs(a-b)<.0001f;
    static void Reject(LevelId next)
    {
        var current=LevelRun.Current;int stage=LevelRun.Stage;bool rejected=false;
        try{LevelRun.Advance(next);}catch(InvalidOperationException){rejected=true;}
        Check(rejected && LevelRun.Current==current && LevelRun.Stage==stage,"Invalid route mutated the run");
    }
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
}
