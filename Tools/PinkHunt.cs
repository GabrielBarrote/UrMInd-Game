using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor;

public static class PinkHunt
{
    public static string Find()
    {
        StringBuilder sb = new StringBuilder();
        HashSet<Material> seen = new HashSet<Material>();
        foreach (MeshRenderer r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            foreach (Material m in r.sharedMaterials)
            {
                if (m == null) { sb.AppendLine("MATERIAL NULO em " + r.name); continue; }
                if (!seen.Add(m)) continue;
                bool bad = m.shader == null || !m.shader.isSupported
                           || m.shader.name.Contains("InternalError")
                           || m.shader.name.Contains("Hidden/");
                if (bad)
                    sb.AppendLine("SHADER RUIM: material '" + m.name + "' shader='"
                        + (m.shader == null ? "null" : m.shader.name) + "' em objeto '" + r.name + "'");
            }
        }
        Material grass = AssetDatabase.LoadAssetAtPath<Material>("Assets/City 02/Commons/Materials/Grass.mat");
        sb.AppendLine("Grass.mat shader = " + (grass == null ? "asset nao existe"
            : grass.shader.name + " supported=" + grass.shader.isSupported));
        sb.AppendLine("(vazio acima = nenhum material quebrado)");
        return sb.ToString();
    }
}
