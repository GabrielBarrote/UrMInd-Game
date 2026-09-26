using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

public static class ApvVerify
{
    public static string Run()
    {
        StringBuilder sb = new StringBuilder();
        ProbeVolumeBakingSet set = AssetDatabase.LoadAssetAtPath<ProbeVolumeBakingSet>(
            "Assets/Detail/CityBakingSet.asset");
        if (set == null) return "ERRO: baking set nao encontrado";

        sb.AppendLine("set: " + set.name + " cenas=" + set.sceneGUIDs.Count);
        sb.AppendLine("minDistanceBetweenProbes=" + set.minDistanceBetweenProbes
            + " simplificationLevels=" + set.simplificationLevels
            + " maxSubdivision=" + set.maxSubdivision);

        // 'bakedSimplificationLevels' so e preenchido quando o bake conclui
        FieldInfo bsl = typeof(ProbeVolumeBakingSet).GetField("bakedSimplificationLevels",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        FieldInfo bmd = typeof(ProbeVolumeBakingSet).GetField("bakedMinDistanceBetweenProbes",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        sb.AppendLine("bakedSimplificationLevels=" + (bsl == null ? "?" : bsl.GetValue(set).ToString())
            + " bakedMinDistance=" + (bmd == null ? "?" : bmd.GetValue(set).ToString()));

        FieldInfo cells = typeof(ProbeVolumeBakingSet).GetField("cellDescs",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (cells != null)
        {
            object v = cells.GetValue(set);
            System.Collections.ICollection c = v as System.Collections.ICollection;
            sb.AppendLine("celulas de probe assadas = " + (c == null ? "?" : c.Count.ToString()));
        }

        sb.AppendLine("ProbeReferenceVolume.isInitialized = " + ProbeReferenceVolume.instance.isInitialized);

        // arquivos de dados gerados em disco
        string[] assets = AssetDatabase.FindAssets("", new string[] { "Assets/Detail" });
        foreach (string g in assets)
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            if (p.Contains("ProbeVolume") || p.EndsWith(".bytes") || p.Contains("BakingSet"))
                sb.AppendLine("   asset: " + p);
        }
        sb.AppendLine("lightmaps na cena = " + LightmapSettings.lightmaps.Length);
        return sb.ToString();
    }
}
