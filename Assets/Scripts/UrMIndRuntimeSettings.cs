using UnityEngine;

public static class UrMIndRuntimeSettings
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Apply()
    {
        Screen.SetResolution(1980, 1080, FullScreenMode.FullScreenWindow,
            new RefreshRate { numerator = 60, denominator = 1 });
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 1;
    }
}
