using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;

public static class TreeProbe
{
    public static string Check()
    {
        StringBuilder sb = new StringBuilder();

        // qual objeto e o cameraRig do controller?
        AnalysisCartController ctrl = UnityEngine.Object.FindFirstObjectByType<AnalysisCartController>();
        sb.AppendLine("cameraRig = " + (ctrl == null || ctrl.cameraRig == null ? "NAO ATRIBUIDO"
            : ctrl.cameraRig.name + " | pai=" + (ctrl.cameraRig.parent == null ? "raiz da cena" : ctrl.cameraRig.parent.name)
            + " | temCamera=" + (ctrl.cameraRig.GetComponent<Camera>() != null)));
        sb.AppendLine("cameraOffset = " + (ctrl == null ? "?" : ctrl.cameraOffset.ToString("F2"))
            + " lookHeight=" + (ctrl == null ? "?" : ctrl.lookHeight.ToString()));

        // arvores em pe: quantas, e qual a estrutura
        int standing = 0, standingWithCol = 0;
        Dictionary<string, int> byMesh = new Dictionary<string, int>();
        List<Transform> samples = new List<Transform>();

        foreach (MeshFilter mf in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
        {
            if (mf.sharedMesh == null) continue;
            string mn = mf.sharedMesh.name;
            if (!mn.StartsWith("tree")) continue;
            if (mf.name.Contains("DERRUBADA")) continue;   // essas sao as caidas
            standing++;
            if (mf.GetComponent<Collider>() != null) standingWithCol++;
            if (!byMesh.ContainsKey(mn)) byMesh[mn] = 0;
            byMesh[mn]++;
            if (samples.Count < 3) samples.Add(mf.transform);
        }

        sb.AppendLine("=== ARVORES EM PE ===");
        sb.AppendLine("total=" + standing + " com collider=" + standingWithCol);
        foreach (KeyValuePair<string, int> kv in byMesh) sb.AppendLine("   mesh '" + kv.Key + "' x" + kv.Value);

        foreach (Transform t in samples)
        {
            MeshFilter mf = t.GetComponent<MeshFilter>();
            Renderer r = t.GetComponent<Renderer>();
            sb.AppendLine("amostra '" + t.name + "' pai=" + (t.parent == null ? "-" : t.parent.name)
                + " escala=" + t.lossyScale.ToString("F2")
                + " meshLocalBounds center=" + mf.sharedMesh.bounds.center.ToString("F3")
                + " size=" + mf.sharedMesh.bounds.size.ToString("F3")
                + " mundoY=" + r.bounds.min.y.ToString("F2") + ".." + r.bounds.max.y.ToString("F2")
                + " filhos=" + t.childCount
                + " static=" + GameObjectUtility.AreStaticEditorFlagsSet(t.gameObject, StaticEditorFlags.BatchingStatic));
        }
        return sb.ToString();
    }
}
