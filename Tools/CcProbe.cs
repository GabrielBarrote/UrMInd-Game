using System.Text;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;

public static class CcProbe
{
    public static string Check()
    {
        StringBuilder sb = new StringBuilder();
        CharacterController cc = UnityEngine.Object.FindFirstObjectByType<CharacterController>();
        if (cc == null) sb.AppendLine("sem CharacterController");
        else sb.AppendLine("CC em '" + cc.name + "' radius=" + cc.radius + " height=" + cc.height
            + " center=" + cc.center.ToString("F2") + " stepOffset=" + cc.stepOffset
            + " slopeLimit=" + cc.slopeLimit + " skinWidth=" + cc.skinWidth
            + " layer=" + cc.gameObject.layer);

        // exemplo de arvore derrubada: qual o tamanho do obstaculo
        GameObject root = GameObject.Find("City 02/World/Infrastructure Defects");
        if (root != null)
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name.Contains("DERRUBADA"))
                {
                    Renderer r = t.GetComponent<Renderer>();
                    MeshFilter mf = t.GetComponent<MeshFilter>();
                    sb.AppendLine("arvore '" + t.name + "' rot=" + t.eulerAngles.ToString("F0")
                        + " escala=" + t.lossyScale.ToString("F2")
                        + " boundsMundo Y=" + (r == null ? "?" : r.bounds.min.y.ToString("F2") + ".." + r.bounds.max.y.ToString("F2"))
                        + " tamanhoMundo=" + (r == null ? "?" : r.bounds.size.ToString("F2"))
                        + " meshLocalSize=" + (mf == null || mf.sharedMesh == null ? "?" : mf.sharedMesh.bounds.size.ToString("F2")));
                    break;
                }

        // o Decal feature respeita rendering layers?
        UniversalRendererData data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
        foreach (ScriptableRendererFeature f in data.rendererFeatures)
        {
            if (!(f is DecalRendererFeature)) continue;
            SerializedObject so = new SerializedObject(f);
            SerializedProperty it = so.GetIterator();
            while (it.NextVisible(true))
                sb.AppendLine("  decalFeature." + it.propertyPath + " (" + it.propertyType + ")");
        }
        return sb.ToString();
    }
}
