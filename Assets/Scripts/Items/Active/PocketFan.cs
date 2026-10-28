using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class PocketFan : AActiveItem
{
    void Awake()
    {
        SetData("Pocket fan", "Make some room", "Blow nearby bullets upward. They remain dangerous.", ItemType.active);
        cost = 20;
        cd = 2.5f;
    }

    public override float OnPress(float mana)
    {
        float valid = CheckValidPress(mana);
        if (valid < 0)
            return valid;
        var wind = s.GetComponent<PocketWind>() ?? s.gameObject.AddComponent<PocketWind>();
        wind.Begin();
        lastCD = 0;
        return mana - cost;
    }
}
public sealed class PocketWind : MonoBehaviour
{
    public static PocketWind Current
    {
        get; private set;
    }

    public float Remaining
    {
        get; private set;
    }

    LineRenderer[] lines;
    public void Begin()
    {
        Current = this;
        Remaining = 1.2f;
        if (lines == null)
        {
            lines = new LineRenderer[5];
            for (int i = 0; i < lines.Length; i++)
                lines[i] = CombatVisuals.Line(transform, new Color(.55f, .85f, .8f, .35f), .035f);
        }
        ItemSound.Play("fan", .18f);
    }

    public static Vector3 Velocity(Vector3 p)
    {
        if (Current == null || Current.Remaining <= 0 || GameRules.playerDisabled || WorldTimeStop.Frozen)
            return Vector3.zero;
        float distance = Vector2.Distance(p, Current.transform.position);
        return distance < 3.2f ? Vector3.up * Mathf.Lerp(12, 7, distance / 3.2f) : Vector3.zero;
    }

    void Update()
    {
        if (GameRules.playerDisabled)
        {
            Remaining = 0;
        }
        if (!WorldTimeStop.Frozen)
            Remaining = Mathf.Max(0, Remaining - Time.deltaTime);
        if (lines == null)
            return;
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            line.enabled = Remaining > 0;
            if (!line.enabled)
                continue;
            float phase = Mathf.Repeat((1.2f - Remaining) * 2 + i * .17f, 1);
            var p = transform.position + new Vector3((i - 2) * .52f, -1 + phase * 3, -2);
            line.positionCount = 3;
            line.SetPosition(0, p);
            line.SetPosition(1, p + new Vector3(.08f, .3f, 0));
            line.SetPosition(2, p + Vector3.up * .65f);
        }
    }

    void OnDestroy()
    {
        if (Current == this)
            Current = null;
    }
}
