using System.Text;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class LightAudit
{
    public static string Report()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("== LUZES ==");
        foreach (Light l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            sb.AppendLine("  " + l.name + " type=" + l.type + " intensity=" + l.intensity
                          + " color=" + l.color + " shadows=" + l.shadows
                          + " mode=" + l.lightmapBakeType
                          + " rot=" + l.transform.eulerAngles.ToString("F0"));

        sb.AppendLine("== AMBIENTE ==");
        sb.AppendLine("  mode=" + RenderSettings.ambientMode
                      + " intensity=" + RenderSettings.ambientIntensity
                      + " sky=" + RenderSettings.ambientSkyColor
                      + " equator=" + RenderSettings.ambientEquatorColor
                      + " ground=" + RenderSettings.ambientGroundColor);
        sb.AppendLine("  skybox=" + (RenderSettings.skybox == null ? "null" : RenderSettings.skybox.name));
        sb.AppendLine("  fog=" + RenderSettings.fog + " color=" + RenderSettings.fogColor);

        sb.AppendLine("== VOLUMES ==");
        foreach (Volume v in UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsSortMode.None))
        {
            sb.AppendLine("  Volume '" + v.name + "' global=" + v.isGlobal
                          + " weight=" + v.weight + " priority=" + v.priority
                          + " profile=" + (v.sharedProfile == null ? "NULL" : v.sharedProfile.name));
            if (v.sharedProfile == null) continue;
            foreach (VolumeComponent comp in v.sharedProfile.components)
            {
                sb.AppendLine("     - " + comp.GetType().Name + " active=" + comp.active);
            }
        }

        sb.AppendLine("== LIGHTMAPS ==");
        sb.AppendLine("  lightmaps assados: " + LightmapSettings.lightmaps.Length);
        sb.AppendLine("  lightProbes: " + (LightmapSettings.lightProbes == null
            ? "null" : LightmapSettings.lightProbes.count.ToString()));

        int staticCount = 0, totalRenderers = 0;
        foreach (MeshRenderer r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            totalRenderers++;
            if (GameObjectUtility.AreStaticEditorFlagsSet(r.gameObject, StaticEditorFlags.ContributeGI))
                staticCount++;
        }
        sb.AppendLine("  renderers=" + totalRenderers + " com ContributeGI=" + staticCount);

        return sb.ToString();
    }
}
