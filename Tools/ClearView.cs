using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor;

// Abre o corredor de visao atras do ponto de largada.
// A copa das arvores nao tem collider, entao o desvio de obstaculo da camera
// nao a afasta: a unica solucao e nao ter arvore ali.
public static class ClearView
{
    public static string Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return "ERRO: saia do Play mode primeiro";

        InspectionGame g = Object.FindFirstObjectByType<InspectionGame>();
        AnalysisCartController c = Object.FindFirstObjectByType<AnalysisCartController>();
        Vector3 spawn = g.spawnPosition;
        Vector3 cam = spawn + new Vector3(0f, c.cameraOffset.y, c.cameraOffset.z);

        GameObject world = GameObject.Find("City 02/World");
        Transform trees = world.transform.Find("Trees");
        StringBuilder sb = new StringBuilder();
        List<Transform> kill = new List<Transform>();

        foreach (Transform t in trees)
        {
            if (!t.gameObject.activeSelf) continue;
            Vector3 p = t.position;
            // corredor: 7m de cada lado do eixo da camera, de tras dela ate a via
            float dx = Mathf.Abs(p.x - cam.x);
            if (dx > 7f) continue;
            if (p.z < cam.z - 5f || p.z > spawn.z + 2f) continue;
            kill.Add(t);
        }
        foreach (Transform t in kill)
        {
            t.gameObject.SetActive(false);
            sb.AppendLine("   removida " + t.name + " em " + t.position.ToString("F1"));
        }

        sb.AppendLine("spawn=" + spawn.ToString("F1") + " camera=" + cam.ToString("F1"));
        sb.AppendLine("arvores desativadas no corredor: " + kill.Count);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        return sb.ToString();
    }
}
