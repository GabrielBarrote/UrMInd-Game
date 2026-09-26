using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;

// Renderiza a cena por uma câmera temporária própria.
// A Scene View não repinta com o Editor em segundo plano, então capture_scene_view
// devolve frames velhos; isto aqui é determinístico.
public static class Render
{
    // args: [x, y, z, pitch, yaw, distance, outputPath]
    public static string Shot(string x, string y, string z,
                              string pitch, string yaw, string dist, string outPath)
    {
        Vector3 target = new Vector3(
            float.Parse(x, System.Globalization.CultureInfo.InvariantCulture),
            float.Parse(y, System.Globalization.CultureInfo.InvariantCulture),
            float.Parse(z, System.Globalization.CultureInfo.InvariantCulture));
        float p = float.Parse(pitch, System.Globalization.CultureInfo.InvariantCulture);
        float ya = float.Parse(yaw, System.Globalization.CultureInfo.InvariantCulture);
        float d = float.Parse(dist, System.Globalization.CultureInfo.InvariantCulture);

        Quaternion rot = Quaternion.Euler(p, ya, 0f);
        Vector3 camPos = target - rot * Vector3.forward * d;

        GameObject camGo = new GameObject("__ShotCam");
        camGo.hideFlags = HideFlags.HideAndDontSave;
        Camera cam = camGo.AddComponent<Camera>();
        UniversalAdditionalCameraData acd = camGo.AddComponent<UniversalAdditionalCameraData>();
        acd.renderPostProcessing = true;
        acd.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        cam.transform.position = camPos;
        cam.transform.rotation = rot;
        cam.fieldOfView = 55f;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 800f;

        int w = 1280, h = 720;
        RenderTexture rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 1;
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;

        string full = Path.Combine(Application.dataPath, "..", outPath);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(full)));
        File.WriteAllBytes(Path.GetFullPath(full), tex.EncodeToPNG());

        cam.targetTexture = null;
        UnityEngine.Object.DestroyImmediate(tex);
        UnityEngine.Object.DestroyImmediate(rt);
        UnityEngine.Object.DestroyImmediate(camGo);

        return "salvo em " + Path.GetFullPath(full) + " camPos=" + camPos.ToString("F2");
    }

    // Atalho: fotografa a n-ésima rachadura de um ângulo de gameplay.
    public static string ShotCrack(string indexStr, string outPath)
    {
        int index = 0;
        int.TryParse(indexStr, out index);
        GameObject root = GameObject.Find("City 02/World/Infrastructure Defects");
        if (root == null) return "ERRO: root de defeitos nao encontrado";

        DefectInfo[] cracks = root.GetComponentsInChildren<DefectInfo>(true)
            .Where(dd => dd.defectType != null && dd.defectType.IndexOf("achadura") >= 0)
            .ToArray();
        if (cracks.Length == 0) return "ERRO: nenhuma rachadura";

        DefectInfo c = cracks[Mathf.Clamp(index, 0, cracks.Length - 1)];
        Vector3 pos = c.transform.position;
        string info = c.name + " (" + c.severity + ") ";
        return info + Shot(
            pos.x.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "0",
            pos.z.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "40", "25", "6", outPath);
    }
}
