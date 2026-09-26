using System.Text;
using UnityEngine;
using UnityEditor;

public static class PropProbe
{
    public static string Check()
    {
        StringBuilder sb = new StringBuilder();
        Shader sh = Shader.Find("City/Kenney Detail");
        if (sh == null) return "shader nulo";
        sb.AppendLine("propriedades de textura e float do shader:");
        int n = sh.GetPropertyCount();
        for (int i = 0; i < n; i++)
        {
            string nm = sh.GetPropertyName(i);
            if (nm.Contains("Detail") || nm.Contains("Macro") || nm.Contains("Smooth"))
                sb.AppendLine("   " + nm + " (" + sh.GetPropertyType(i) + ")");
        }

        Material m = AssetDatabase.LoadAssetAtPath<Material>("Assets/Detail/Materials/Kenney_Roads.mat");
        sb.AppendLine("HasProperty _DetailPack = " + m.HasProperty("_DetailPack"));
        sb.AppendLine("HasProperty _DetailScale = " + m.HasProperty("_DetailScale"));
        sb.AppendLine("_DetailScale valor = " + (m.HasProperty("_DetailScale") ? m.GetFloat("_DetailScale").ToString() : "n/a"));
        sb.AppendLine("_DetailStrength valor = " + (m.HasProperty("_DetailStrength") ? m.GetFloat("_DetailStrength").ToString() : "n/a"));

        Texture2D pack = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Detail/Textures/detail_pack.png");
        sb.AppendLine("pack carregado = " + (pack == null ? "NULO" : pack.name + " " + pack.width + "x" + pack.height));
        return sb.ToString();
    }
}
