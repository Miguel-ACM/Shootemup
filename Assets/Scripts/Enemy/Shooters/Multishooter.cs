using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Multishooter : AShooter
{

    [System.Serializable]
    private class shooterList
    {
        public AShooter[] shooters;
    }

    [System.Serializable]
    private class minMax
    {
        public float min;
        public float max;
    }

    [System.Serializable]
    private class intArray
    {
        public int[] arr;
    }

    [SerializeField] private shooterList[] shooters;
    [SerializeField] private minMax[] minmaxTimes;
    [SerializeField] private float[] breaks;
    [SerializeField] private intArray[] prohibitedTransitions;
    [SerializeField] private bool fixedFirstPhase;
    private int curPhase = -1;

    void Update()
    {
    }
    // Start is called before the first frame update

    private bool inProhibited(int next, int curr)
    {
        bool prohibited = false;
        foreach (int elem in prohibitedTransitions[curr].arr)
        {
            if (elem == next)
                prohibited = true;
        }
        return prohibited;
    }

    protected override IEnumerator Shoot()
    {
        if (fixedFirstPhase)
        {
            curPhase = 0;
        }
        else
        {
            curPhase = Random.Range(0, shooters.Length);
        }
        int nextPhase;

        while (true)
        {
            foreach (AShooter shooter in shooters[curPhase].shooters)
            {
                shooter.Trigger();
            }
            float minmaxtime = Random.Range(minmaxTimes[curPhase].min, minmaxTimes[curPhase].max);
            yield return new WaitForSeconds(minmaxtime);
            
            nextPhase = Random.Range(0, shooters.Length);
            while (nextPhase == curPhase || inProhibited(nextPhase, curPhase))
            {
                nextPhase = Random.Range(0, shooters.Length);
            }

            foreach (AShooter shooter in shooters[curPhase].shooters)
            {
                shooter.Stop();
            }

            yield return new WaitForSeconds(breaks[curPhase]);

            curPhase = nextPhase;

        }
    }

    public override void Stop()
    {
        foreach (AShooter shooter in shooters[curPhase].shooters)
        {
            shooter.Stop();
        }
        base.Stop();
    }


}
