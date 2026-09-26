using UnityEngine;
using UnityEditor;

public static class Reimport
{
    public static string ShaderAndMaterials()
    {
        AssetDatabase.ImportAsset("Assets/Detail/Shaders/KenneyDetailInput.hlsl",
            ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset("Assets/Detail/Shaders/KenneyDetail.shader",
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.Refresh();
        Shader sh = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Detail/Shaders/KenneyDetail.shader");
        if (sh == null) return "ERRO: shader nao carregou";
        int n = sh.GetPropertyCount();
        bool hasPack = false, hasScale = false;
        for (int i = 0; i < n; i++)
        {
            string nm = sh.GetPropertyName(i);
            if (nm == "_DetailPack") hasPack = true;
            if (nm == "_DetailScale") hasScale = true;
        }
        return "reimportado. props=" + n + " _DetailPack=" + hasPack + " _DetailScale=" + hasScale
             + " erros=" + ShaderUtil.GetShaderMessageCount(sh);
    }
}
