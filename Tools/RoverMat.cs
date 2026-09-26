using System.Text;
using UnityEngine;
using UnityEditor;

public static class RoverMat
{
    public static string Check()
    {
        StringBuilder sb = new StringBuilder();
        CharacterController cc = UnityEngine.Object.FindFirstObjectByType<CharacterController>();
        if (cc == null) return "rover nao encontrado";
        foreach (Renderer r in cc.GetComponentsInChildren<Renderer>(true))
        {
            Material m = r.sharedMaterial;
            sb.AppendLine(r.name + " shader=" + (m == null ? "NULL" : m.shader.name)
                + " color=" + (m != null && m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor").ToString() : "?")
                + " baseMap=" + (m != null && m.GetTexture("_BaseMap") != null ? m.GetTexture("_BaseMap").name : "sem textura")
                + " assetPath=" + (m == null ? "-" : (AssetDatabase.GetAssetPath(m) == "" ? "EMBUTIDO" : AssetDatabase.GetAssetPath(m))));
        }
        return sb.ToString();
    }
}
