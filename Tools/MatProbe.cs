using System.Text;
using UnityEngine;
using UnityEditor;

public static class MatProbe
{
    public static string Check()
    {
        StringBuilder sb = new StringBuilder();
        string[] names = new string[] { "Kenney_Roads", "Kenney_Commercial", "Kenney_Cars" };
        foreach (string n in names)
        {
            string p = "Assets/Detail/Materials/" + n + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (m == null) { sb.AppendLine(n + ": MATERIAL NULO"); continue; }
            sb.AppendLine(n + ": shader=" + (m.shader == null ? "NULL" : m.shader.name)
                + " isSupported=" + (m.shader != null && m.shader.isSupported)
                + " passCount=" + m.passCount
                + " baseMap=" + (m.GetTexture("_BaseMap") == null ? "NULO" : m.GetTexture("_BaseMap").name)
                + " detailPack=" + (m.GetTexture("_DetailPack") == null ? "NULO" : m.GetTexture("_DetailPack").name)
                + " renderQueue=" + m.renderQueue);
            for (int i = 0; i < m.passCount; i++)
                sb.Append(" [" + i + "]" + m.GetPassName(i));
            sb.AppendLine();
        }
        sb.AppendLine("ShaderUtil anything compiling = " + ShaderUtil.anythingCompiling);
        return sb.ToString();
    }
}
