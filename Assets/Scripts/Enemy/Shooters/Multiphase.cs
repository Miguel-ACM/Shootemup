using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Multiphase : AShooter
{
    [System.Serializable]
    private class ShooterList
    {
        public AShooter[] shooters;
    }

    [SerializeField] private ShooterList[] phases;
    [SerializeField] private float[] life;
    [SerializeField] private float breakTime = 1f;
    private float currBreak = 0;
    ShooterList curShooter;
    private float maxHealth;
    private int currPhase = 0;
    private bool start = false;
    private bool changinPhase = false;


    new public void Start()
    {
        base.Start();
        maxHealth = enemy.health;
        activation = ShootTrigger.onArrive;
    }

    void Update()
    {
        curShooter = phases[0];
        if (start)
        {
            foreach (AShooter shoot in curShooter.shooters)
            {
                shoot.activation = ShootTrigger.onTrigger;
                shoot.Trigger();
            }
            currBreak = 0f;
            start = false;
        }
        
        if (currPhase < life.Length && enemy.health < maxHealth * life[currPhase] )
        {
            if (!changinPhase)
            {
                foreach (AShooter shoot in curShooter.shooters)
                {
                    shoot.Stop();
                }
                enemy.NotifyBossPhaseChange(currPhase);
                changinPhase = true;
            }
            if (currBreak < breakTime)
            {
                currBreak += Time.deltaTime;
            }
            else
            {
                
                changinPhase = false;
                currPhase++;
                curShooter = phases[currPhase];
                foreach (AShooter shoot in curShooter.shooters)
                {
                    shoot.activation = ShootTrigger.onTrigger;
                    shoot.Trigger();
                }
                currBreak = 0f;
            }
            
        }
    }


    protected override IEnumerator Shoot()
    {
        start = true;
        yield return null;
    }


}
