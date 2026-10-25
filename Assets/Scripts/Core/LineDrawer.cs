using System;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

[RequireComponent(typeof(LineRenderer))]
public class LineDrawer : MonoBehaviour
{
    LineRenderer lineRenderer;
    LineRenderer core;
    public void SetCoatings(bool fire, bool poison)
    {
        lineRenderer.colorGradient = PlayerShotPalette.Electricity(fire, poison, false);
        core.colorGradient = PlayerShotPalette.Electricity(fire, poison, true);
    }

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        transform.localScale = Vector3.one;
        lineRenderer.sharedMaterial = Resources.Load<Material>("Effects/PSXParticles");
        lineRenderer.sortingLayerName = "Bullet";
        lineRenderer.sortingOrder = 20;
        lineRenderer.widthCurve = AnimationCurve.Linear(0, 1, 1, 1);
        lineRenderer.widthMultiplier = .10f;
        lineRenderer.startColor = new Color(.12f, .42f, 1);
        lineRenderer.endColor = new Color(.25f, .65f, 1);
        lineRenderer.useWorldSpace = true;
        var child = new GameObject("Beam core");
        child.transform.SetParent(transform, false);
        core = child.AddComponent<LineRenderer>();
        core.sharedMaterial = lineRenderer.sharedMaterial;
        core.useWorldSpace = true;
        core.positionCount = 0;
        core.sortingLayerName = "Bullet";
        core.sortingOrder = 21;
        core.widthMultiplier = .028f;
        core.startColor = core.endColor = new Color(.72f, .94f, 1);
        core.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    public void DrawLine(Vector3[] positions, float timeToKill)
    {
        CancelInvoke("KillLine");
        lineRenderer.positionCount = positions.Length;
        lineRenderer.SetPositions(positions);
        core.positionCount = positions.Length;
        core.SetPositions(positions);
        Invoke("KillLine", timeToKill);
    }

    public void DrawLine(Vector2[] positions, float z, float timeToKill, bool jagged = false)
    {
        CancelInvoke("KillLine");
        var points = new List<Vector3>();
        for (int i = 0; i < positions.Length; i++)
        {
            if (jagged && i > 0)
            {
                var direction = (positions[i] - positions[i - 1]).normalized;
                var normal = new Vector2(-direction.y, direction.x);
                for (int segment = 1; segment < 3; segment++)
                {
                    var point = Vector2.Lerp(positions[i - 1], positions[i], segment / 3f) + normal * UnityEngine.Random.Range(-.13f, .13f);
                    points.Add(new Vector3(point.x, point.y, z));
                }
            }
            points.Add(new Vector3(positions[i].x, positions[i].y, z));
        }
        var positions3 = points.ToArray();
        lineRenderer.positionCount = positions3.Length;
        lineRenderer.SetPositions(positions3);
        core.positionCount = positions3.Length;
        core.SetPositions(positions3);
        Invoke("KillLine", timeToKill);
    }

    void KillLine()
    {
        lineRenderer.positionCount = 0;
        core.positionCount = 0;
    }
}
