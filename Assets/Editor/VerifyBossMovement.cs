using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class VerifyBossMovement
{
    public static void Run()
    {
        var prefab=Resources.Load<GameObject>("Enemy/Bosses/stage1boss");
        var mover=prefab.GetComponent<BossMove>();
        Check(mover!=null && prefab.GetComponentsInChildren<FollowPath>(true).Length==0,"Boss still hands off to the drawn path");
        var origin=new Vector3(0,11.52f,7);
        float join=mover.EntranceDuration;
        var centre=mover.PositionAtTime(origin,join);
        Check(mover.PositionAtTime(origin,0)==origin,"Entrance starts with a jump");
        const float epsilon=.001f;
        Check(Vector3.Distance(mover.PositionAtTime(origin,join-epsilon),centre)<.0001f,"Entrance endpoint is discontinuous");
        Check(Vector3.Distance(mover.PositionAtTime(origin,join+epsilon),centre)<.0001f,"Loop start is discontinuous");
        var vBefore=(centre-mover.PositionAtTime(origin,join-.01f))/.01f;
        var vAfter=(mover.PositionAtTime(origin,join+.01f)-centre)/.01f;
        Check((vAfter-vBefore).magnitude<.025f,"Entrance-to-loop velocity snaps");
        foreach(int fps in new[]{30,60,144})
        {
            var previous=origin;float maxLoopStep=0;
            for(int frame=1;frame<=fps*40;frame++)
            {
                float t=frame/(float)fps;var p=mover.PositionAtTime(origin,t);
                Check(float.IsFinite(p.x) && float.IsFinite(p.y) && p.z==origin.z,"Invalid position or changed gameplay depth");
                if(t<=join)Check(p.y<=previous.y+.0001f && p.y>=centre.y-.0001f,"Entrance overshoots or reverses");
                else
                {
                    Check(Mathf.Abs(p.x-centre.x)<=1.2001f && Mathf.Abs(p.y-centre.y)<=1.0001f,"Figure eight escaped its bounds");
                    maxLoopStep=Mathf.Max(maxLoopStep,Vector3.Distance(p,previous));
                }
                previous=p;
            }
            Check(maxLoopStep<2f/fps,"Loop contains a position snap at "+fps+" FPS");
        }
        float start=join+1+.5f; // Fully accelerated, away from the entrance ramp.
        for(int i=0;i<100;i++)
        {
            float t=start+i*.13f;
            Check(Vector3.Distance(mover.PositionAtTime(origin,t),mover.PositionAtTime(origin,t+mover.CycleDuration))<.0001f,"Loop does not close seamlessly");
        }
        string result="PASS: boss prefab has one movement controller; continuous position and velocity at entrance; bounded figure eight; 40-second samples at 30/60/144 FPS; preserved depth; 100 loop-closure checks.";
        Directory.CreateDirectory("FlowReview");File.WriteAllText("FlowReview/BossMovement.txt",result);Debug.Log(result);
    }
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
}
