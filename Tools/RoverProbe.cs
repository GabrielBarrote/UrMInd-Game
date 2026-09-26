using System.Text;
using UnityEngine;
using UnityEditor;

public static class RoverProbe
{
    public static string Structure()
    {
        StringBuilder sb = new StringBuilder();
        CharacterController cc = UnityEngine.Object.FindFirstObjectByType<CharacterController>();
        if (cc == null) return "rover nao encontrado";
        GameObject root = cc.gameObject;

        sb.AppendLine("ROOT '" + root.name + "'");
        sb.AppendLine("  prefab? " + PrefabUtility.GetPrefabAssetType(root)
            + " status=" + PrefabUtility.GetPrefabInstanceStatus(root)
            + " asset=" + (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root)));
        sb.AppendLine("  pos=" + root.transform.position.ToString("F3")
            + " rot=" + root.transform.eulerAngles.ToString("F1")
            + " escala=" + root.transform.localScale.ToString("F3"));
        sb.AppendLine("  CC radius=" + cc.radius + " height=" + cc.height + " center=" + cc.center.ToString("F2"));
        sb.AppendLine("  componentes na raiz:");
        foreach (Component comp in root.GetComponents<Component>())
            sb.AppendLine("     " + comp.GetType().Name);

        sb.AppendLine("  HIERARQUIA:");
        Walk(root.transform, 2, sb);
        return sb.ToString();
    }

    static void Walk(Transform t, int depth, StringBuilder sb)
    {
        string pad = new string(' ', depth * 3);
        foreach (Transform c in t)
        {
            MeshFilter mf = c.GetComponent<MeshFilter>();
            Renderer r = c.GetComponent<Renderer>();
            sb.AppendLine(pad + c.name
                + " | localPos=" + c.localPosition.ToString("F3")
                + " localRot=" + c.localEulerAngles.ToString("F0")
                + " localScale=" + c.localScale.ToString("F3")
                + (mf != null && mf.sharedMesh != null
                    ? " | mesh=" + mf.sharedMesh.name + " verts=" + mf.sharedMesh.vertexCount
                      + " localBounds=" + mf.sharedMesh.bounds.size.ToString("F2")
                    : " | sem mesh")
                + (r != null && r.sharedMaterial != null ? " | mat=" + r.sharedMaterial.name : ""));
            Walk(c, depth + 1, sb);
        }
    }
}
