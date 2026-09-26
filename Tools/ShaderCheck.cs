using System.Text;
using UnityEngine;
using UnityEditor;

public static class ShaderCheck
{
    public static string Check()
    {
        AssetDatabase.Refresh();
        string path = "Assets/Detail/Shaders/KenneyDetail.shader";
        Shader sh = AssetDatabase.LoadAssetAtPath<Shader>(path);
        StringBuilder sb = new StringBuilder();
        if (sh == null) return "ERRO: shader nao importou de " + path;

        sb.AppendLine("shader: '" + sh.name + "'");
        sb.AppendLine("erros de compilacao: " + ShaderUtil.GetShaderMessageCount(sh));
        ShaderMessage[] msgs = ShaderUtil.GetShaderMessages(sh);
        for (int i = 0; i < msgs.Length && i < 15; i++)
            sb.AppendLine("  [" + msgs[i].severity + "] " + msgs[i].message
                          + " | " + msgs[i].messageDetails + " (" + msgs[i].file + ":" + msgs[i].line + ")");

        return sb.ToString();
    }
}
