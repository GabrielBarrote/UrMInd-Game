using System.Text;
using UnityEngine;
using UnityEditor;

public static class SceneTree
{
    public static string Run()
    {
        StringBuilder sb = new StringBuilder();
        foreach (GameObject go in UnityEngine.SceneManagement.SceneManager
                 .GetActiveScene().GetRootGameObjects())
            Walk(go.transform, 0, sb);
        return sb.ToString();
    }

    static void Walk(Transform t, int d, StringBuilder sb)
    {
        int rend = t.GetComponentsInChildren<Renderer>(true).Length;
        sb.AppendLine(new string(' ', d * 2) + t.name
            + " (filhos=" + t.childCount + ", renderers na subarvore=" + rend
            + ", contributeGI=" + GameObjectUtility.AreStaticEditorFlagsSet(t.gameObject, StaticEditorFlags.ContributeGI) + ")");
        if (d >= 2) return;
        foreach (Transform c in t) Walk(c, d + 1, sb);
    }
}
