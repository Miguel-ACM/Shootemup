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

    // Picks a random allowed phase other than the current one. Retrying at random until one
    // fits never ends when there is only one phase or every other one is prohibited, which
    // froze the game; in that case the current phase simply repeats.
    private int PickNextPhase(int curr)
    {
        List<int> allowed = new List<int>();
        for (int i = 0; i < shooters.Length; i++)
        {
            if (i != curr && !inProhibited(i, curr))
                allowed.Add(i);
        }
        if (allowed.Count == 0)
            return curr;
        return allowed[Random.Range(0, allowed.Count)];
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
            
            nextPhase = PickNextPhase(curPhase);

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
