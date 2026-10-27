using UnityEngine;

public sealed partial class TunnelSawBoss
{

    void HardImpact(Vector2 inward)
    {
        // Every object is born on the physical bounce. Its later curvature is
        // motion, never a timer-triggered secondary emission.
        int quick=miniature?14:phaseTwo?36:28,heavy=miniature?6:phaseTwo?16:12;
        int room=CombatRound.Limit-CombatRound.Live.Count;
        if(room<quick+heavy)
        {
            float ratio=room/(float)(quick+heavy);
            quick=Mathf.FloorToInt(quick*ratio);heavy=Mathf.FloorToInt(heavy*ratio);
        }
        for(int i=0;i<quick;i++)
        {
            var axis=CombatVisuals.Rotate(inward,i*360f/quick+impacts*11);
            var shot=Fire<SimpleRound>(transform.position+(Vector3)axis*Radius*.92f,axis,3.2f,.17f);
            if(shot!=null)shot.Curl((impacts%2==0?1:-1)*(miniature?24:18),2.8f);
        }
        for(int i=0;i<heavy;i++)
        {
            var axis=CombatVisuals.Rotate(Vector2.down,i*360f/heavy);
            Vector3 port=transform.position+(Vector3)axis*Radius*.92f;
            var shot=Fire<SimpleRound>(port,inward,1.5f,miniature?.25f:.38f);
            if(shot!=null)shot.Orbit(miniature?1.15f:2.2f,i*360f/heavy,impacts%2==0?40:-40,8,0,0,0,1.25f);
        }
    }
}
