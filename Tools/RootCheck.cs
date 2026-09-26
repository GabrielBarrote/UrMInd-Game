using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RootCheck
{
    public static string Run()
    {
        StringBuilder sb = new StringBuilder();
        foreach (GameObject go in SceneManager.GetActiveScene().GetRootGameObjects())
            sb.AppendLine(go.name + "  (renderers=" + go.GetComponentsInChildren<Renderer>(true).Length + ")");
        InspectionGame g = Object.FindFirstObjectByType<InspectionGame>();
        sb.AppendLine("InspectionGame: " + (g == null ? "AUSENTE"
            : "ok em '" + g.gameObject.name + "' scanner=" + (g.scanner != null)
              + " menu=" + (g.menuPanel != null) + " hud=" + (g.hudPanel != null)
              + " results=" + (g.resultsPanel != null) + " campo=" + (g.nameField != null)));
        return sb.ToString();
    }
}
