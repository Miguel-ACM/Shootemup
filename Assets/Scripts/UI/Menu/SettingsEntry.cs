using TMPro;
using UnityEngine;

/// <summary>Opens the settings page in place of the menu's rows; its BACK row returns.</summary>
public sealed class SettingsEntry : AMenuElement
{
    AMenuElement[] page, hidden;
    float centre;

    public static SettingsEntry Create(Transform parent, TMP_FontAsset font, float centre)
    {
        var entry = MenuLayout.Row(parent, "SETTINGS", font).gameObject.AddComponent<SettingsEntry>();
        entry.centre = centre;
        var kinds = new[] { OptionRow.Kind.Music, OptionRow.Kind.Sound, OptionRow.Kind.PsxFilter };
        entry.page = new AMenuElement[kinds.Length + 1];
        for (int i = 0; i < kinds.Length; i++)
            entry.page[i] = OptionRow.Create(parent, kinds[i], font);
        var back = MenuLayout.Row(parent, "BACK", font).gameObject.AddComponent<SettingsBack>();
        back.owner = entry;
        entry.page[kinds.Length] = back;
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

    public void Close()
    {
        if (hidden == null)
            return;
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
