using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConflictGroup
{
    public enum cg
    {
        t_sides,
        t_center_sides,
        t_center,
        b_sides,
        b_center_sides,
        b_center,
        center,
        trl,
        mrl,
        brl,
        t_following_d3,
        t_following_d2,
        t_following_d1,
        b_following_d1,
        //t_teleporting,
        //c_teleporting,
        //c_sides_teleporting
    }

    public static bool isConflict(cg c1, cg c2)
    {
        // Añadir aquí las reglas especiales de conflictos.
        // Por defecto hay conflicto solo si los dos grupos son iguales, pero habrá reglas especiales
        return c1 == c2;
    }

    public static bool isConflict(cg c1, List<cg> c2)
    {
        foreach (cg c in c2)
        {
            if (isConflict(c1, c))
                return true;
        }
        return false;
    }

}
