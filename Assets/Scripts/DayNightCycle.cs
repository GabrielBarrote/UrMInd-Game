using UnityEngine;
using UnityEngine.InputSystem;

// Ciclo visual leve aplicado sobre a cena principal. F6 avanca uma fase para
// demonstracoes; o ciclo automatico continua sem trocar/recarregar a cena.
[DefaultExecutionOrder(-80)]
public sealed class DayNightCycle : MonoBehaviour
{
    public enum Phase { Day, Sunset, Night }

    public float cycleSeconds = 240f;
    public bool automatic = true;
    public Phase startPhase = Phase.Day;

    public static DayNightCycle Instance { get; private set; }
    public float NightFactor { get; private set; }
    public Phase CurrentPhase { get; private set; }

    Light sun;
    float phaseTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindAnyObjectByType<DayNightCycle>() != null) return;
        new GameObject("Day Night Cycle").AddComponent<DayNightCycle>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        sun = RenderSettings.sun;
        if (sun == null)
        {
            foreach (Light light in FindObjectsByType<Light>())
                if (light.type == LightType.Directional) { sun = light; break; }
        }
        SetPhase(startPhase);
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.f6Key.wasPressedThisFrame)
            SetPhase((Phase)(((int)CurrentPhase + 1) % 3));

        if (!automatic || cycleSeconds <= 0f) return;
        phaseTime += Time.unscaledDeltaTime;
        float segment = cycleSeconds / 3f;
        if (phaseTime >= segment)
        {
            phaseTime -= segment;
            SetPhase((Phase)(((int)CurrentPhase + 1) % 3));
        }
    }

    public void SetPhase(Phase phase)
    {
        CurrentPhase = phase;
        phaseTime = 0f;

        Color ambient;
        Color fog;
        Color sunColor;
        float intensity;
        Vector3 rotation;

        switch (phase)
        {
            case Phase.Sunset:
                NightFactor = 0.45f;
                ambient = new Color(0.34f, 0.22f, 0.24f);
                fog = new Color(0.55f, 0.30f, 0.24f);
                sunColor = new Color(1f, 0.47f, 0.24f);
                intensity = 0.65f;
                rotation = new Vector3(12f, -35f, 0f);
                break;
            case Phase.Night:
                NightFactor = 1f;
                ambient = new Color(0.035f, 0.055f, 0.11f);
                fog = new Color(0.025f, 0.04f, 0.075f);
                sunColor = new Color(0.42f, 0.55f, 0.85f);
                intensity = 0.12f;
                rotation = new Vector3(25f, 145f, 0f);
                break;
            default:
                NightFactor = 0f;
                ambient = new Color(0.48f, 0.52f, 0.58f);
                fog = new Color(0.67f, 0.77f, 0.86f);
                sunColor = new Color(1f, 0.95f, 0.84f);
                intensity = 1.15f;
                rotation = new Vector3(48f, -30f, 0f);
                break;
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = ambient;
        RenderSettings.fogColor = fog;
        if (sun != null)
        {
            sun.color = sunColor;
            sun.intensity = intensity;
            sun.transform.rotation = Quaternion.Euler(rotation);
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
