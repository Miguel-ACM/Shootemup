using TMPro;
using UnityEngine;

/// <summary>Opens the settings page in place of the menu's rows; its BACK row returns.</summary>
public sealed class SettingsEntry : AMenuElement
{
    AMenuElement[] page, hidden;
    float centre;
    AMenuElement[] confirmation;
    SettingsErase erase;

    public static SettingsEntry Create(Transform parent, TMP_FontAsset font, float centre)
    {
        var entry = MenuLayout.Row(parent, "SETTINGS", font).gameObject.AddComponent<SettingsEntry>();
        entry.centre = centre;
        var kinds = new[] { OptionRow.Kind.Music, OptionRow.Kind.Sound, OptionRow.Kind.PsxFilter };
        entry.page = new AMenuElement[kinds.Length + 2];
        for (int i = 0; i < kinds.Length; i++)
            entry.page[i] = OptionRow.Create(parent, kinds[i], font);
        entry.erase = MenuLayout.Row(parent, "DELETE SAVE DATA", font).gameObject.AddComponent<SettingsErase>();
        entry.erase.owner = entry;
        entry.page[kinds.Length] = entry.erase;
        var back = MenuLayout.Row(parent, "BACK", font).gameObject.AddComponent<SettingsBack>();
        back.owner = entry;
        entry.page[kinds.Length + 1] = back;
        var cancel = MenuLayout.Row(parent, "KEEP SAVE", font).gameObject.AddComponent<SettingsErase>();
        var confirm = MenuLayout.Row(parent, "ERASE ALL PROGRESS", font).gameObject.AddComponent<SettingsErase>();
        cancel.owner = confirm.owner = entry;
        cancel.choice = 1;
        confirm.choice = 2;
        entry.confirmation = new AMenuElement[] { cancel, confirm };
        foreach (var row in entry.confirmation)
            row.gameObject.SetActive(false);
        foreach (var row in entry.page)
            row.gameObject.SetActive(false);
        return entry;
    }

    public override void OnAccept()
    {
        hidden = menu.GetComponentsInChildren<AMenuElement>();
        Show(true);
        menu.setCurrentElement(page[0]);
    }

    public void ChooseErase(int choice)
    {
        if (choice == 2)
        {
            if (!MetaProgress.DeleteProgress())
                erase.GetComponentInChildren<TextMeshProUGUI>().text = "ERASE FAILED";
            else if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Title")
            {
                menu.Suspended = true;
                CloudTransition.Load("Title", HangarHub.Leave);
                return;
            }
            else
            {
                Close();
                return;
            }
        }
        foreach (var row in page)
            row.gameObject.SetActive(choice != 0);
        foreach (var row in confirmation)
            row.gameObject.SetActive(choice == 0);
        MenuLayout.Arrange(menu, centre);
        menu.RefreshElements();
        menu.setCurrentElement(choice == 0 ? confirmation[0] : erase);
    }

    public bool TryBack()
    {
        if (hidden == null) return false;
        if (confirmation[0].gameObject.activeSelf) ChooseErase(1);
        else Close();
        return true;
    }

    public void Close()
    {
        if (hidden == null)
            return;
        foreach (var row in confirmation)
            row.gameObject.SetActive(false);
        Show(false);
        hidden = null;
        menu.setCurrentElement(this);
    }

    void Show(bool settings)
    {
        foreach (var row in hidden)
            row.gameObject.SetActive(!settings);
        foreach (var row in page)
            row.gameObject.SetActive(settings);
        MenuLayout.Arrange(menu, centre);
        menu.RefreshElements();
    }
}

public sealed class SettingsBack : AMenuElement
{
    public SettingsEntry owner;

    public override void OnAccept() => owner.Close();

    // Closing the pause menu leaves the page, so it reopens on the main rows.
    void Update()
    {
        if (menu != null && !menu.IsOpen)
            owner.Close();
    }
}

public sealed class SettingsErase : AMenuElement
{
    public SettingsEntry owner;
    public int choice;
    public override void OnAccept() => owner.ChooseErase(choice);
    void Update()
    {
        if (menu != null && !menu.IsOpen)
            owner.Close();
    }
}
