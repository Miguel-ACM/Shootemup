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
                    LevelRun.Advance(final);Check(LevelRun.ReturnsToPlains,"Final district must return to Plains");
                    bool rejected=false;
                    try{LevelRun.GetChoices(out left,out right);}catch(InvalidOperationException){rejected=true;}
                    Check(rejected,"Final tier must not offer duplicate routes");Reject(LevelId.Snow);
                    LevelRun.Advance(LevelId.Fields);
                    Check(LevelRun.Seed==seed,"Route transition reset the run seed");
                }
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
