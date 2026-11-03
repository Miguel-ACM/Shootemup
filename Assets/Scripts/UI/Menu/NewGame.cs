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

    // Fade the menu while its tunnel is still moving. The camera carries the flight into the dock.
    System.Collections.IEnumerator IntoHangar(RunDifficulty difficulty)
    {
        const float Seconds = .65f;
        menu.Suspended = true;
        while (!LevelEnvironment.TitleReady)
            yield return null;
        var group = menu.GetComponent<CanvasGroup>();
        var panel = menu.transform.parent.GetComponent<Image>();
        var shade = panel != null ? panel.color : Color.clear;
        for (float elapsed = 0; elapsed < Seconds;)
        {
            yield return null;
            elapsed += Time.unscaledDeltaTime;
            if (group != null)
                group.alpha = Mathf.SmoothStep(1, 0, elapsed / .4f);
            if (panel != null)
                panel.color = new Color(shade.r, shade.g, shade.b, Mathf.SmoothStep(shade.a, 0, elapsed / Seconds));
        }
        LevelEnvironment.CarryTitle();
        HangarHub.Enter(difficulty);
    }
}

