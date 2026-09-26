using UnityEngine;
using UnityEditor;

public static class SetSpawn
{
    public static string Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return "ERRO: saia do Play mode primeiro";
        InspectionGame g = Object.FindFirstObjectByType<InspectionGame>();
        if (g == null) return "InspectionGame ausente";
        g.spawnPosition = new Vector3(0f, 0.20f, -2.0f);
        g.spawnYaw = 0f;
        EditorUtility.SetDirty(g);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        return "spawn = " + g.spawnPosition.ToString("F2") + " yaw=" + g.spawnYaw
             + " (fachada FECAP em z=7, distancia " + (7f - g.spawnPosition.z).ToString("F1") + "m)";
    }
}
