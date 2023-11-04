using System;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

[RequireComponent(typeof(LineDrawer))]
public class LineDrawer : MonoBehaviour
{
    LineRenderer lineRenderer;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    public void DrawLine(Vector3[] positions, float timeToKill)
    {
        lineRenderer.positionCount = positions.Length;
        lineRenderer.SetPositions(positions);
        Invoke("KillLine", timeToKill);
    }

    public void DrawLine(Vector2[] positions, float z, float timeToKill)
    {
        lineRenderer.positionCount = positions.Length;
        Vector3[] positions3 = new Vector3[positions.Length];
        for (int i = 0; i < positions.Length; i++ )
        {
            positions3[i] = new Vector3(positions[i].x, positions[i].y, z);
        }
        lineRenderer.SetPositions(positions3);
        Invoke("KillLine", timeToKill);
    }

    void KillLine()
    {
        lineRenderer.positionCount = 0;
    }
}