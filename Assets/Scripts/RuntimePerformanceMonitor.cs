using Unity.Profiling;
using UnityEngine;

// Telemetria local e removivel: escreve uma amostra no Player.log a cada 10 s.
// Nao envia dados pela rede.
public sealed class RuntimePerformanceMonitor : MonoBehaviour
{
    const float SampleSeconds = 10f;
    static ProfilerRecorder memoryRecorder;
    float elapsed;
    int frames;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindAnyObjectByType<RuntimePerformanceMonitor>() != null) return;
        new GameObject("Local Performance Monitor").AddComponent<RuntimePerformanceMonitor>();
    }

    void OnEnable()
    {
        memoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Used Memory");
    }

    void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        frames++;
        if (elapsed < SampleSeconds) return;
        float fps = frames / elapsed;
        float frameMs = elapsed * 1000f / frames;
        float memoryMb = memoryRecorder.Valid ? memoryRecorder.LastValue / (1024f * 1024f) : -1f;
        Light[] lights = FindObjectsByType<Light>();
        int activeLights = 0;
        for (int i = 0; i < lights.Length; i++)
            if (lights[i].enabled && lights[i].gameObject.activeInHierarchy) activeLights++;
        Debug.Log($"[UrMInd Performance] fps={fps:F1} frameMs={frameMs:F2} usedMemoryMB={memoryMb:F1} lights={lights.Length} activeLights={activeLights}");
        elapsed = 0f;
        frames = 0;
    }

    void OnDisable()
    {
        memoryRecorder.Dispose();
    }
}
