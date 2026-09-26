using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;

public static class SsaoTune
{
    const string RendererPath = "Assets/Settings/PC_Renderer.asset";

    public static string Dump()
    {
        UniversalRendererData data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
        StringBuilder sb = new StringBuilder();
        foreach (ScriptableRendererFeature f in data.rendererFeatures)
        {
            if (!(f is ScreenSpaceAmbientOcclusion)) continue;
            SerializedObject so = new SerializedObject(f);
            SerializedProperty s = so.FindProperty("m_Settings");
            SerializedProperty it = s.Copy();
            SerializedProperty end = s.GetEndProperty();
            while (it.NextVisible(true) && !SerializedProperty.EqualContents(it, end))
                sb.AppendLine("  " + it.name + " (" + it.propertyType + ") = " + Val(it));
        }
        return sb.ToString();
    }

    static string Val(SerializedProperty p)
    {
        if (p.propertyType == SerializedPropertyType.Float) return p.floatValue.ToString();
        if (p.propertyType == SerializedPropertyType.Integer) return p.intValue.ToString();
        if (p.propertyType == SerializedPropertyType.Boolean) return p.boolValue.ToString();
        if (p.propertyType == SerializedPropertyType.Enum)
            return p.enumValueIndex + " (" + string.Join("|", p.enumNames) + ")";
        return "?";
    }

    public static string Apply()
    {
        UniversalRendererData data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
        StringBuilder sb = new StringBuilder();
        foreach (ScriptableRendererFeature f in data.rendererFeatures)
        {
            if (!(f is ScreenSpaceAmbientOcclusion)) continue;
            SerializedObject so = new SerializedObject(f);
            SerializedProperty s = so.FindProperty("m_Settings");

            // 0.95 era grande demais: em objetos de ~1m (o rover) gerava auto-oclusao
            // borrada que parecia textura de lixa. 0.38 serve predio e rover.
            s.FindPropertyRelative("Radius").floatValue = 0.38f;
            s.FindPropertyRelative("Intensity").floatValue = 0.85f;
            s.FindPropertyRelative("DirectLightingStrength").floatValue = 0.45f;
            s.FindPropertyRelative("Falloff").floatValue = 180f;
            // qualidade de amostragem: High
            s.FindPropertyRelative("Samples").enumValueIndex = 0;
            s.FindPropertyRelative("BlurQuality").enumValueIndex = 0;
            s.FindPropertyRelative("Downsample").boolValue = false;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(f);
            sb.AppendLine("SSAO ajustado");
        }
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        return sb.ToString() + Dump();
    }
}
