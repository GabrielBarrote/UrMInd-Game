using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Diagnostico de cena para batchmode. Nao altera nada, so relata.
public static class SceneDiag
{
    public static void Report()
    {
        var scene = EditorSceneManager.OpenScene("Assets/City - 02 - Day.unity", OpenSceneMode.Single);
        var sb = new StringBuilder();
        sb.AppendLine("=== DIAG INICIO ===");
        sb.AppendLine("scene=" + scene.path + " roots=" + scene.rootCount);

        foreach (var root in scene.GetRootGameObjects())
        {
            int total = 0, visible = 0, shadowsOnly = 0, rendOff = 0, inactive = 0;
            var rs = root.GetComponentsInChildren<Renderer>(true);
            Bounds b = new Bounds();
            bool hasB = false;
            foreach (var r in rs)
            {
                total++;
                if (!r.gameObject.activeInHierarchy) { inactive++; continue; }
                if (!r.enabled) { rendOff++; continue; }
                if (r.shadowCastingMode == ShadowCastingMode.ShadowsOnly) { shadowsOnly++; continue; }
                visible++;
                if (!hasB) { b = r.bounds; hasB = true; } else b.Encapsulate(r.bounds);
            }
            sb.AppendLine(string.Format(
                "ROOT '{0}' active={1} pos={2} renderers={3} visible={4} inactive={5} rendererOff={6} shadowsOnly={7} bounds={8}",
                root.name, root.activeSelf, root.transform.position, total, visible, inactive, rendOff, shadowsOnly,
                hasB ? b.ToString() : "n/a"));
        }

        sb.AppendLine("=== DIAG FIM ===");
        Debug.Log(sb.ToString());
    }
}
