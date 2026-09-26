using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;

public static class CamAudit
{
    public static string Report()
    {
        StringBuilder sb = new StringBuilder();
        foreach (Camera c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            UniversalAdditionalCameraData d = c.GetComponent<UniversalAdditionalCameraData>();
            sb.AppendLine(c.name + " enabled=" + c.enabled + " depth=" + c.depth
                + " postFX=" + (d == null ? "SEM UACD" : d.renderPostProcessing.ToString())
                + " AA=" + (d == null ? "?" : d.antialiasing.ToString())
                + " renderType=" + (d == null ? "?" : d.renderType.ToString()));
        }
        return sb.ToString();
    }

    // Liga post-processing nas cameras de gameplay.
    public static string EnablePostFX()
    {
        StringBuilder sb = new StringBuilder();
        foreach (Camera c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            UniversalAdditionalCameraData d = c.GetComponent<UniversalAdditionalCameraData>();
            if (d == null) d = c.gameObject.AddComponent<UniversalAdditionalCameraData>();
            d.renderPostProcessing = true;
            d.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            EditorUtility.SetDirty(c.gameObject);
            sb.AppendLine("postFX ligado em " + c.name);
        }
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        return sb.ToString();
    }
}
