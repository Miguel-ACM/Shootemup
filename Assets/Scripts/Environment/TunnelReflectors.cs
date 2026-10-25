using UnityEngine;
public sealed class TunnelReflectors : MonoBehaviour
{
    LineRenderer[] rails;
    void Start()
    {
        rails = new LineRenderer[2];
        for (int i = 0; i < 2; i++)
            rails[i] = CombatVisuals.Line(transform, new Color(1, .67f, .17f, .65f), .07f);
    }

    void Update()
    {
        if (rails == null)
            return;
        for (int i = 0; i < 2; i++)
        {
            rails[i].enabled = LevelRun.Current == LevelId.Tunnel;
            float x = i == 0 ? Camera.LeftLimit + .15f : Camera.RightLimit - .15f;
            rails[i].SetPosition(0, new Vector3(x, Camera.DownLimit, -.2f));
            rails[i].SetPosition(1, new Vector3(x, Camera.UpLimit, -.2f));
        }
    }
}
