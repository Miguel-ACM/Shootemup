using UnityEngine;
using UnityEngine.UI;

public class NewGame : AMenuElement
{
    bool starting;
    // Progress lives in the hangar save, so the first row continues it rather than starting over.
    protected override void Start()
    {
        base.Start();
        if (textElement != null)
            textElement.text = "CONTINUE";
    }

    public override void OnAccept()
    {
        Depart(MetaProgress.FlightSetting);
    }

    public void Choose(int option)
    {
        if (option >= 0 && option < 3)
            Depart(option == 0 ? RunDifficulty.Easy : option == 1 ? RunDifficulty.Normal : RunDifficulty.Hard);
    }

    void Depart(RunDifficulty difficulty)
    {
        if (starting) return;
        starting = true;
        menu.StartCoroutine(IntoHangar(difficulty));
    }

    // The menu fades away while the title's tunnel slows to a stop; the hangar then continues from that same view.
    System.Collections.IEnumerator IntoHangar(RunDifficulty difficulty)
    {
        const float Seconds = 2.1f;
        menu.Suspended = true;
        var group = menu.GetComponent<CanvasGroup>();
        var panel = menu.transform.parent.GetComponent<Image>();
        var shade = panel != null ? panel.color : Color.clear;
        var halt = menu.StartCoroutine(LevelEnvironment.HaltTitle(Seconds));
        for (float t = 0; t < Seconds; t += Time.unscaledDeltaTime)
        {
            float fade = Mathf.Clamp01(t / .4f);
            if (group != null)
                group.alpha = 1 - fade;
            if (panel != null)
                panel.color = new Color(shade.r, shade.g, shade.b, shade.a * (1 - Mathf.Clamp01(t / Seconds)));
            yield return null;
        }
        yield return halt;
        // A short jolt as the tunnel locks in place, then the hangar takes over the very same view.
        var camera = UnityEngine.Camera.main;
        if (camera != null)
        {
            var rest = camera.transform.localPosition;
            for (float t = 0; t < .35f; t += Time.unscaledDeltaTime)
            {
                float strength = .14f * (1 - t / .35f);
                camera.transform.localPosition = rest + (Vector3)(Random.insideUnitCircle * strength);
                yield return null;
            }
            camera.transform.localPosition = rest;
        }
        LevelEnvironment.CarryTitle();
        HangarHub.Enter(difficulty);
    }
}

