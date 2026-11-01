using UnityEngine;

/// <summary>Player options shared by the title and pause menus, saved between sessions.</summary>
public static class GameOptions
{
    const string MusicKey = "MusicVolume", SoundKey = "SoundVolume", FilterKey = "PsxFilter";
    public const int Steps = 10;
    public static int Music { get; private set; } = Steps;
    public static int Sound { get; private set; } = Steps;
    public static bool PsxFilter { get; private set; } = true;
    public static float MusicVolume => Music / (float)Steps;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Load()
    {
        Music = Mathf.Clamp(PlayerPrefs.GetInt(MusicKey, Steps), 0, Steps);
        Sound = Mathf.Clamp(PlayerPrefs.GetInt(SoundKey, Steps), 0, Steps);
        PsxFilter = PlayerPrefs.GetInt(FilterKey, 1) == 1;
        // The music source ignores the listener volume, so this scales every other sound.
        AudioListener.volume = Sound / (float)Steps;
    }

    public static void SetMusic(int value)
    {
        Music = Mathf.Clamp(value, 0, Steps);
        PlayerPrefs.SetInt(MusicKey, Music);
        BGMManager.ApplyVolume();
    }

    public static void SetSound(int value)
    {
        Sound = Mathf.Clamp(value, 0, Steps);
        PlayerPrefs.SetInt(SoundKey, Sound);
        AudioListener.volume = Sound / (float)Steps;
    }

    public static void SetPsxFilter(bool enabled)
    {
        PsxFilter = enabled;
        PlayerPrefs.SetInt(FilterKey, enabled ? 1 : 0);
        foreach (var presentation in Object.FindObjectsByType<PSXPresentation>())
            presentation.SetFilter(enabled);
    }
}
