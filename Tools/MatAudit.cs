using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor;

public static class MatAudit
{
    public static string Report()
    {
        Dictionary<Material, int> use = new Dictionary<Material, int>();
        foreach (MeshRenderer r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            foreach (Material m in r.sharedMaterials)
            {
                if (m == null) continue;
                if (!use.ContainsKey(m)) use[m] = 0;
                use[m]++;
            }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("materiais distintos em uso: " + use.Count);
        List<KeyValuePair<Material, int>> list = new List<KeyValuePair<Material, int>>(use);
        list.Sort(delegate (KeyValuePair<Material, int> a, KeyValuePair<Material, int> b)
        { return b.Value.CompareTo(a.Value); });

        for (int i = 0; i < list.Count && i < 25; i++)
        {
            Material m = list[i].Key;
            string path = AssetDatabase.GetAssetPath(m);
            sb.AppendLine("  " + list[i].Value + "x  '" + m.name + "'  shader=" + m.shader.name
                + "  bump=" + (m.HasProperty("_BumpMap") && m.GetTexture("_BumpMap") != null)
                + "  detailNrm=" + (m.HasProperty("_DetailNormalMap") && m.GetTexture("_DetailNormalMap") != null)
                + "  instancing=" + m.enableInstancing
                + "  path=" + (string.IsNullOrEmpty(path) ? "EMBUTIDO NA CENA" : path));
        }
        return sb.ToString();
    }
}
