using System.Text;
using UnityEngine;
using UnityEditor;

// Remove a queda de arvore por impacto. Os CapsuleCollider continuam: as
// arvores seguem solidas, so nao tombam mais.
public static class NoTreeFall
{
    public static string Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return "ERRO: saia do Play mode primeiro";

        StringBuilder sb = new StringBuilder();

        // 1. tira o componente do rover
        int removed = 0;
        foreach (CartTreeImpact t in Object.FindObjectsByType<CartTreeImpact>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            sb.AppendLine("CartTreeImpact removido de '" + t.gameObject.name + "'");
            Object.DestroyImmediate(t);
            removed++;
        }
        if (removed == 0) sb.AppendLine("nenhum CartTreeImpact na cena");

        // 2. limpa Rigidbody que possa ter sobrado em alguma arvore
        int rbs = 0, cols = 0, standing = 0;
        foreach (MeshFilter mf in Object.FindObjectsByType<MeshFilter>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (mf.sharedMesh == null || !mf.sharedMesh.name.StartsWith("tree")) continue;
            standing++;
            Rigidbody rb = mf.GetComponent<Rigidbody>();
            if (rb != null) { Object.DestroyImmediate(rb); rbs++; }
            if (mf.GetComponent<Collider>() != null) cols++;
        }

        sb.AppendLine("arvores na cena: " + standing);
        sb.AppendLine("Rigidbody removidos: " + rbs);
        sb.AppendLine("arvores que seguem com collider: " + cols);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        return sb.ToString();
    }
}
