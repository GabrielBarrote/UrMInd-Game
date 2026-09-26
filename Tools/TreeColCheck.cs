using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class TreeColCheck
{
    public static string Run()
    {
        StringBuilder sb = new StringBuilder();
        Dictionary<string, int> noCol = new Dictionary<string, int>();
        int total = 0, withCol = 0;
        List<string> sample = new List<string>();

        foreach (MeshFilter mf in Object.FindObjectsByType<MeshFilter>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (mf.sharedMesh == null || !mf.sharedMesh.name.StartsWith("tree")) continue;
            total++;
            if (mf.GetComponent<Collider>() != null) { withCol++; continue; }

            string parent = mf.transform.parent == null ? "(raiz)" : mf.transform.parent.name;
            string grand = mf.transform.parent != null && mf.transform.parent.parent != null
                ? mf.transform.parent.parent.name : "-";
            string key = grand + " / " + parent;
            if (!noCol.ContainsKey(key)) noCol[key] = 0;
            noCol[key]++;
            if (sample.Count < 6)
                sample.Add(mf.name + "  pai=" + parent + "  ativo=" + mf.gameObject.activeInHierarchy);
        }

        sb.AppendLine("total=" + total + " comCollider=" + withCol + " semCollider=" + (total - withCol));
        foreach (KeyValuePair<string, int> kv in noCol)
            sb.AppendLine("   sem collider sob: " + kv.Key + "  x" + kv.Value);
        foreach (string s in sample) sb.AppendLine("   ex: " + s);
        return sb.ToString();
    }
}
