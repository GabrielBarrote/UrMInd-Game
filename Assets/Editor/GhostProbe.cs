using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Lista as ocorrencias sem geometria visivel e diz por que: objeto inativo, pai
// inativo, renderer desligado ou nenhum renderer.
public static class GhostProbe
{
    public static void Report()
    {
        EditorSceneManager.OpenScene("Assets/City - 02 - Day.unity", OpenSceneMode.Single);
        var sb = new StringBuilder();
        sb.AppendLine("=== FANTASMAS INICIO ===");

        foreach (var d in Object.FindObjectsByType<DefectInfo>(FindObjectsInactive.Exclude))
        {
            var rs = d.GetComponentsInChildren<Renderer>(true);
            bool vis = false;
            foreach (var r in rs)
                if (r.enabled && r.gameObject.activeInHierarchy) { vis = true; break; }
            if (vis) continue;

            int off = 0, inactive = 0;
            foreach (var r in rs)
            {
                if (!r.gameObject.activeInHierarchy) inactive++;
                else if (!r.enabled) off++;
            }

            string parents = "";
            for (Transform t = d.transform; t != null; t = t.parent)
                if (!t.gameObject.activeSelf) parents += t.name + " ";

            sb.AppendLine(d.name
                + " | tipo=" + d.defectType
                + " | code=" + d.code
                + " | renderers=" + rs.Length
                + " inativos=" + inactive
                + " off=" + off
                + " | paisDesligados=[" + parents.Trim() + "]"
                + " | pos=" + d.transform.position);
        }

        sb.AppendLine("=== FANTASMAS FIM ===");
        Debug.Log(sb.ToString());
    }
}
