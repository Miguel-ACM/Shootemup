using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class WavePool : MonoBehaviour
{
    System.Random rnd = new System.Random();

    List<Wave> wavePool = new List<Wave>()
    {
        // 2 Balones desde abajo a los lados

        new Wave(3, 13f, 10, ConflictGroup.cg.center, new List<WaveUnit>(){
            { new WaveUnit("Biggie", 0, new Vector3(0f, 2f)) }
        }),

        new Wave(3, 13f, 10, ConflictGroup.cg.center, new List<WaveUnit>(){
            { new WaveUnit("Biggie", 0, new Vector3(0f, 2.4f)) }
        }),

        new Wave(5, 13f, 10, ConflictGroup.cg.t_sides, new List<WaveUnit>(){
            { new WaveUnit("Biggie", 0, new Vector3(-0.7f, 2.2f)) },
            { new WaveUnit("Biggie", 0, new Vector3(0.7f, 2.2f))}
        }),

        new Wave(1, 5f, 1, ConflictGroup.cg.b_sides, new List<WaveUnit>(){
            { new WaveUnit("enemy1", 0, new Vector3(-0.8f, -1.2f)) },
            { new WaveUnit("enemy1", 0, new Vector3(0.8f, -1.2f)) },
            { new WaveUnit("enemy1", 0, new Vector3(-0.8f, -1.5f)) },
            { new WaveUnit("enemy1", 0, new Vector3(0.8f, -1.5f)) }
        }),

        // 2 balones normales a los lados
        new Wave(1, 5f, 1, ConflictGroup.cg.t_sides, new List<WaveUnit>(){
            { new WaveUnit("enemy1", 0, new Vector3(-0.8f, 1.2f)) },
            { new WaveUnit("enemy1", 0, new Vector3(0.8f, 1.2f)) },
            { new WaveUnit("enemy1", 0, new Vector3(-0.8f, 1.5f)) },
            { new WaveUnit("enemy1", 0, new Vector3(0.8f, 1.5f)) }
        }),

        // 3 balones normales a los lados
        new Wave(1, 5f, 1, ConflictGroup.cg.t_sides, new List<WaveUnit>(){
            { new WaveUnit("enemy1", 0, new Vector3(-0.8f, 1.2f)) },
            { new WaveUnit("enemy1", 0, new Vector3(0.8f, 1.2f)) },
            { new WaveUnit("enemy1", 0, new Vector3(-0.8f, 1.5f)) },
            { new WaveUnit("enemy1", 0, new Vector3(0.8f, 1.5f)) },
            { new WaveUnit("enemy1", 0, new Vector3(-0.8f, 1.8f)) },
            { new WaveUnit("enemy1", 0, new Vector3(0.8f, 1.8f)) }
        }),

        // Balones en el centro
        new Wave(1, 5f, 1, ConflictGroup.cg.t_center_sides, new List<WaveUnit>(){
            { new WaveUnit("enemy1", 0, new Vector3(0f, 1.2f)) },
            { new WaveUnit("enemy1", 0, new Vector3(-0.25f, 1.5f)) },
            { new WaveUnit("enemy1", 0, new Vector3(0.25f, 1.5f)) },
            { new WaveUnit("enemy1", 0, new Vector3(-0.25f, 1.8f)) },
            { new WaveUnit("enemy1", 0, new Vector3(0.25f, 1.8f)) },
            { new WaveUnit("enemy1", 0, new Vector3(0f, 2.1f)) },
        }),

        // Balones que se mueven en el x
        new Wave(1, 5f, 1, ConflictGroup.cg.trl, new List<WaveUnit>(){
            { new WaveUnit("enemy1", 0, new Vector3(-1.2f, 0.9f)) },
            { new WaveUnit("enemy1", 0, new Vector3(1.2f, 0.9f)) },
            { new WaveUnit("enemy1", 0, new Vector3(-1.5f, 0.75f)) },
            { new WaveUnit("enemy1", 0, new Vector3(1.5f, 0.75f)) },
            { new WaveUnit("enemy1", 0, new Vector3(-1.8f, 0.6f)) },
            { new WaveUnit("enemy1", 0, new Vector3(1.8f, 0.6f)) }
        }),

        // Balones que se mueven en el x 2
        new Wave(1, 5f, 1, ConflictGroup.cg.trl, new List<WaveUnit>(){
            { new WaveUnit("enemy1", 0, new Vector3(-1.8f, 0.9f)) },
            { new WaveUnit("enemy1", 0, new Vector3(1.8f, 0.9f)) },
            { new WaveUnit("enemy1", 0, new Vector3(-1.5f, 0.75f)) },
            { new WaveUnit("enemy1", 0, new Vector3(1.5f, 0.75f)) },
            { new WaveUnit("enemy1", 0, new Vector3(-1.2f, 0.6f)) },
            { new WaveUnit("enemy1", 0, new Vector3(1.2f, 0.6f)) }
        }),

        // Un unico balon central
        new Wave(1, 2f, 1, ConflictGroup.cg.t_center, new List<WaveUnit>(){
            { new WaveUnit("enemy1", 0, new Vector3(0f, 1.4f)) },
        }),

        // Waver central
        new Wave(2, 8f, 1, ConflictGroup.cg.t_center, new List<WaveUnit>(){
            { new WaveUnit("Waver", 0, new Vector3(0f, 2.8f)) }
        }),

        // Waver laterales
        new Wave(3, 8f, 1, ConflictGroup.cg.t_sides, new List<WaveUnit>(){
            { new WaveUnit("Waver", 0, new Vector3(0.8f, 2.8f)) },
            { new WaveUnit("Waver", 0, new Vector3(-0.8f, 2.8f)) },
        })


    };

    public Wave getWave(string mode)
    {
        if (mode == "miniboss")
            return DistrictWaves.Miniboss();
        Wave w = null;
        bool isInt = int.TryParse(mode, out int value);
        if (isInt)
        {
            return DistrictWaves.Create(value);
        }
        else
        {
            switch (mode)
            {
                case "shop":
                    w = _getWave(1);
                    break;
                case "deal":
                    w = _getWave(2);
                    break;
                case "boss":
                    w = _getWave(2);
                    break;
            }
        }
        return w;
    }

    Wave _getWave(int capacity)
    {
        var used = new List<int>();
        var groups = new List<ConflictGroup.cg>();
        var selected = new List<Wave>();
        while (capacity > 0)
        {
            var eligible = new List<int>();
            int totalWeight = 0;
            for (int i = 0; i < wavePool.Count; i++)
            {
                var candidate = wavePool[i];
                if (used.Contains(i) || candidate.cost > capacity || ConflictGroup.isConflict(candidate.cg, groups))
                    continue;
                eligible.Add(i);
                totalWeight += Mathf.Max(1, candidate.weight);
            }
            // Some conflict combinations cannot spend the entire budget. Keep the
            // formations already selected instead of retrying until the stage crashes.
            if (eligible.Count == 0)
                break;
            int roll = rnd.Next(totalWeight);
            int chosen = eligible[eligible.Count - 1];
            foreach (int index in eligible)
            {
                roll -= Mathf.Max(1, wavePool[index].weight);
                if (roll < 0)
                {
                    chosen = index;
                    break;
                }
            }
            var formation = wavePool[chosen];
            used.Add(chosen);
            groups.Add(formation.cg);
            selected.Add(formation);
            capacity -= formation.cost;
        }
        return Wave.CombineWaves(selected);
    }
}
