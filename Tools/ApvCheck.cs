using System;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class ApvCheck
{
    public static string Run()
    {
        StringBuilder sb = new StringBuilder();

        Type pv = typeof(ProbeVolume);
        sb.AppendLine("ProbeVolume disponivel: " + (pv != null) + " -> " + (pv == null ? "" : pv.FullName));

        UniversalRenderPipelineAsset rp = UniversalRenderPipeline.asset;
        SerializedObject so = new SerializedObject(rp);
        SerializedProperty lps = so.FindProperty("m_LightProbeSystem");
        sb.AppendLine("m_LightProbeSystem = " + (lps == null ? "campo ausente"
            : lps.enumValueIndex + " (" + string.Join("|", lps.enumNames) + ")"));

        SerializedProperty budget = so.FindProperty("m_ProbeVolumeMemoryBudget");
        sb.AppendLine("m_ProbeVolumeMemoryBudget = " + (budget == null ? "-" : budget.enumValueIndex.ToString()));

        // estado atual da iluminacao
        sb.AppendLine("lightmaps assados = " + LightmapSettings.lightmaps.Length);
        Light sun = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)
            .FirstOrDefault(l => l.type == LightType.Directional);
        sb.AppendLine("sol: " + (sun == null ? "ausente"
            : sun.name + " intensity=" + sun.intensity + " mode=" + sun.lightmapBakeType
              + " shadows=" + sun.shadows));
        try { sb.AppendLine("LightingSettings = " + (Lightmapping.lightingSettings == null ? "null" : Lightmapping.lightingSettings.name)); } catch (System.Exception e) { sb.AppendLine("LightingSettings: nenhum asset atribuido (" + e.GetType().Name + ")"); }

        // quantas ProbeVolume ja existem
        sb.AppendLine("ProbeVolumes na cena = " +
            UnityEngine.Object.FindObjectsByType<ProbeVolume>(FindObjectsSortMode.None).Length);
        return sb.ToString();
    }
}
