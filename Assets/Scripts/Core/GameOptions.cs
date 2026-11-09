using UnityEngine;

/// <summary>Player options shared by the title and pause menus, saved between sessions.</summary>
public static class GameOptions
{
    public enum WindowMode
    {
        Windowed,
        Fullscreen,
        Borderless
    }

    const string MusicKey = "MusicVolume", SoundKey = "SoundVolume", FilterKey = "PsxFilter", ScreenKey = "ScreenMode";
    public const int Steps = 10;
    public static int Music { get; private set; } = Steps;
    public static int Sound { get; private set; } = Steps;
    public static bool PsxFilter { get; private set; } = true;
    public static WindowMode ScreenMode { get; private set; } = WindowMode.Borderless;
    public static float MusicVolume => Music / (float)Steps;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Load()
    {
        Music = Mathf.Clamp(PlayerPrefs.GetInt(MusicKey, Steps), 0, Steps);
        Sound = Mathf.Clamp(PlayerPrefs.GetInt(SoundKey, Steps), 0, Steps);
        PsxFilter = PlayerPrefs.GetInt(FilterKey, 1) == 1;
        // Without a saved choice, keep whatever mode the player started in.
        if (PlayerPrefs.HasKey(ScreenKey))
            ApplyScreen((WindowMode)Mathf.Clamp(PlayerPrefs.GetInt(ScreenKey), 0, 2));
        else
            ScreenMode = Screen.fullScreenMode == FullScreenMode.Windowed ? WindowMode.Windowed :
                Screen.fullScreenMode == FullScreenMode.ExclusiveFullScreen ? WindowMode.Fullscreen : WindowMode.Borderless;
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

    public static void SetScreenMode(WindowMode mode)
    {
        PlayerPrefs.SetInt(ScreenKey, (int)mode);
        ApplyScreen(mode);
    }

    // Fullscreen and borderless fill the monitor at its native resolution; a window
    // takes three quarters of it, keeping the monitor's shape.
    static void ApplyScreen(WindowMode mode)
    {
        ScreenMode = mode;
        if (Application.isEditor)
            return;
        var monitor = Display.main;
        int width = monitor.systemWidth, height = monitor.systemHeight;
        switch (mode)
        {
            case WindowMode.Windowed:
                Screen.SetResolution(Mathf.RoundToInt(width * .75f), Mathf.RoundToInt(height * .75f), FullScreenMode.Windowed);
                break;
            case WindowMode.Fullscreen:
                Screen.SetResolution(width, height, FullScreenMode.ExclusiveFullScreen);
                break;
            default:
                Screen.SetResolution(width, height, FullScreenMode.FullScreenWindow);
                break;
        }
    }

    public static void SetPsxFilter(bool enabled)
    {
        PsxFilter = enabled;
        PlayerPrefs.SetInt(FilterKey, enabled ? 1 : 0);
        foreach (var presentation in Object.FindObjectsByType<PSXPresentation>())
            presentation.SetFilter(enabled);
    }
}
