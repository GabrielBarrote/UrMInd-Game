using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

public static class ApvTune
{
    public static string Inspect()
    {
        StringBuilder sb = new StringBuilder();

        // o baking set foi criado automaticamente?
        string[] guids = AssetDatabase.FindAssets("t:ProbeVolumeBakingSet");
        sb.AppendLine("ProbeVolumeBakingSet encontrados: " + guids.Length);
        foreach (string g in guids) sb.AppendLine("   " + AssetDatabase.GUIDToAssetPath(g));

        if (guids.Length > 0)
        {
            UnityEngine.Object set = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guids[0]));
            SerializedObject so = new SerializedObject(set);
            SerializedProperty it = so.GetIterator();
            while (it.NextVisible(true))
            {
                string n = it.propertyPath;
                if (n.Contains("imple") || n.Contains("inDistance") || n.Contains("robeOffset")
                    || n.Contains("enderingLayer") || n.Contains("cenarios") || n.Contains("Dilation"))
                    sb.AppendLine("   prop " + n + " (" + it.propertyType + ") = " + Val(it));
            }
        }

        ProbeVolume[] pvs = UnityEngine.Object.FindObjectsByType<ProbeVolume>(FindObjectsSortMode.None);
        foreach (ProbeVolume pv in pvs)
            sb.AppendLine("ProbeVolume '" + pv.name + "' size=" + pv.size.ToString("F0")
                + " pos=" + pv.transform.position.ToString("F0") + " mode=" + pv.mode);
        return sb.ToString();
    }

    static string Val(SerializedProperty p)
    {
        switch (p.propertyType)
        {
            case SerializedPropertyType.Float: return p.floatValue.ToString();
            case SerializedPropertyType.Integer: return p.intValue.ToString();
            case SerializedPropertyType.Boolean: return p.boolValue.ToString();
            case SerializedPropertyType.Enum: return p.enumValueIndex + " (" + string.Join("|", p.enumNames) + ")";
            default: return "?";
        }
    }

    // Reduz o volume para a area construida e afrouxa a densidade de probes.
    public static string Apply()
    {
        StringBuilder sb = new StringBuilder();

        ProbeVolume pv = UnityEngine.Object.FindFirstObjectByType<ProbeVolume>();
        if (pv == null) return "ERRO: nenhum ProbeVolume";

        // area construida medida: X[-283..263] Z[-87..163], predios ate 43.8m
        pv.transform.position = new Vector3(-10f, 20f, 38f);
        pv.size = new Vector3(575f, 52f, 275f);
        EditorUtility.SetDirty(pv);
        sb.AppendLine("ProbeVolume -> pos=" + pv.transform.position.ToString("F0")
            + " size=" + pv.size.ToString("F0"));

        // densidade: cidade aberta nao precisa de probe a cada metro
        string[] guids = AssetDatabase.FindAssets("t:ProbeVolumeBakingSet");
        if (guids.Length == 0) { sb.AppendLine("AVISO: nenhum baking set ainda"); return sb.ToString(); }

        UnityEngine.Object set = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guids[0]));
        SerializedObject so = new SerializedObject(set);
        SerializedProperty minDist = so.FindProperty("m_MinDistanceBetweenProbes");
        SerializedProperty simp = so.FindProperty("m_SimplificationLevels");
        if (minDist != null) { minDist.floatValue = 3f; sb.AppendLine("minDistanceBetweenProbes -> 3m"); }
        if (simp != null) { simp.intValue = 3; sb.AppendLine("simplificationLevels -> 3"); }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(set);
        AssetDatabase.SaveAssets();
        return sb.ToString();
    }
}
