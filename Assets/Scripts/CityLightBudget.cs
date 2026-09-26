using System.Collections.Generic;
using UnityEngine;

// Mantem apenas as luzes locais relevantes ao redor da camera. Evita que
// centenas de postes realtime e suas sombras sejam avaliados simultaneamente.
[DefaultExecutionOrder(-70)]
public sealed class CityLightBudget : MonoBehaviour
{
    public int maxLocalLights = 32;
    public int maxShadowLights = 4;
    public float updateInterval = 0.35f;
    public float maximumDistance = 110f;

    sealed class Entry
    {
        public Light light;
        public bool authoredEnabled;
        public LightShadows authoredShadows;
        public float sqrDistance;
    }

    readonly List<Entry> entries = new List<Entry>();
    float nextUpdate;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindAnyObjectByType<CityLightBudget>() != null) return;
        new GameObject("City Light Budget").AddComponent<CityLightBudget>();
    }

    void Start()
    {
        RefreshLights();
        ApplyBudgetNow(GetFocusPosition(), GetNightFactor());
    }

    public void RefreshLights()
    {
        entries.Clear();
        foreach (Light light in FindObjectsByType<Light>(FindObjectsInactive.Include))
        {
            if (light.type == LightType.Directional) continue;
            entries.Add(new Entry
            {
                light = light,
                authoredEnabled = light.enabled,
                authoredShadows = light.shadows,
            });
        }
    }

    void Update()
    {
        if (Time.unscaledTime < nextUpdate) return;
        nextUpdate = Time.unscaledTime + Mathf.Max(0.1f, updateInterval);
        ApplyBudgetNow(GetFocusPosition(), GetNightFactor());
    }

    Vector3 GetFocusPosition()
    {
        Camera camera = Camera.main;
        return camera != null ? camera.transform.position : Vector3.zero;
    }

    static float GetNightFactor()
    {
        return DayNightCycle.Instance != null ? DayNightCycle.Instance.NightFactor : 0f;
    }

    public void ApplyBudgetNow(Vector3 focus, float nightFactor)
    {
        float maxSqr = maximumDistance * maximumDistance;
        for (int i = entries.Count - 1; i >= 0; i--)
        {
            Entry entry = entries[i];
            if (entry.light == null) { entries.RemoveAt(i); continue; }
            entry.sqrDistance = (entry.light.transform.position - focus).sqrMagnitude;
        }
        entries.Sort((a, b) => a.sqrDistance.CompareTo(b.sqrDistance));

        int enabledCount = 0;
        int shadowCount = 0;
        bool localLightsVisible = nightFactor >= 0.2f;
        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];
            bool enable = entry.authoredEnabled && localLightsVisible
                && entry.sqrDistance <= maxSqr && enabledCount < maxLocalLights;
            entry.light.enabled = enable;
            if (!enable) continue;

            enabledCount++;
            bool allowShadow = entry.authoredShadows != LightShadows.None
                && shadowCount < maxShadowLights;
            entry.light.shadows = allowShadow ? entry.authoredShadows : LightShadows.None;
            if (allowShadow) shadowCount++;
        }
    }

    void OnDestroy()
    {
        foreach (Entry entry in entries)
        {
            if (entry.light == null) continue;
            entry.light.enabled = entry.authoredEnabled;
            entry.light.shadows = entry.authoredShadows;
        }
    }
}
