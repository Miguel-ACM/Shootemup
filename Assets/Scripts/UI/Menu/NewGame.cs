using UnityEngine;
using UnityEngine.UI;

public class NewGame : AMenuElement
{
    bool starting;
    protected override void Start()
    {
        base.Start();
        if (textElement != null)
            textElement.text = "PLAY";
    }

    public override void OnAccept()
    {
        SaveSelectMenu.Show(this, menu, textElement.font);
    }

    public void PlaySelectedSlot() => Depart(MetaProgress.FlightSetting);

    public void Choose(int option)
    {
        if (option >= 0 && option < 3)
            Depart(option == 0 ? RunDifficulty.Easy : option == 1 ? RunDifficulty.Normal : RunDifficulty.Hard);
    }

    void Depart(RunDifficulty difficulty)
    {
        if (starting) return;
        starting = true;
        var files = FindAnyObjectByType<SaveSelectMenu>();
        if (files != null) files.DismissForDeparture();
        menu.StartCoroutine(IntoHangar(difficulty));
    }

    // Fade the menu while its tunnel is still moving. The camera carries the flight into the dock.
    System.Collections.IEnumerator IntoHangar(RunDifficulty difficulty)
    {
        const float Seconds = .65f;
        menu.Suspended = true;
        var group = menu.GetComponent<CanvasGroup>();
        float startAlpha = group != null ? group.alpha : 0;
        if (group != null)
            group.interactable = group.blocksRaycasts = false;
        var panel = menu.transform.parent.GetComponent<Image>();
        var shade = panel != null ? panel.color : Color.clear;
        for (float elapsed = 0; elapsed < Seconds;)
        {
            yield return null;
            elapsed += Time.unscaledDeltaTime;
            if (group != null)
                group.alpha = Mathf.SmoothStep(startAlpha, 0, elapsed / .4f);
            if (panel != null)
                panel.color = new Color(shade.r, shade.g, shade.b, Mathf.SmoothStep(shade.a, 0, elapsed / Seconds));
        }
        while (!LevelEnvironment.TitleReady)
            yield return null;
        LevelEnvironment.CarryTitle();
        HangarHub.Enter(difficulty);
    }
}

