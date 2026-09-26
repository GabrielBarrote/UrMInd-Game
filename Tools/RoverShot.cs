using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;

public static class RoverShot
{
    // Fotografa o rover de um angulo 3/4 proximo, para julgar a geometria.
    public static string Run(string pitch, string yaw, string dist, string outPath)
    {
        CharacterController cc = Object.FindFirstObjectByType<CharacterController>();
        if (cc == null) return "rover nao encontrado";
        Vector3 target = cc.transform.position + Vector3.up * 0.55f;

        float p = float.Parse(pitch, System.Globalization.CultureInfo.InvariantCulture);
        float ya = float.Parse(yaw, System.Globalization.CultureInfo.InvariantCulture);
        float d = float.Parse(dist, System.Globalization.CultureInfo.InvariantCulture);

        Quaternion rot = Quaternion.Euler(p, ya, 0f);
        GameObject camGo = new GameObject("__RoverCam");
        camGo.hideFlags = HideFlags.HideAndDontSave;
        Camera cam = camGo.AddComponent<Camera>();
        UniversalAdditionalCameraData acd = camGo.AddComponent<UniversalAdditionalCameraData>();
        acd.renderPostProcessing = true;
        acd.antialiasing = AntialiasingMode.None;
        cam.transform.position = target - rot * Vector3.forward * d;
        cam.transform.rotation = rot;
        cam.fieldOfView = 40f;
        cam.nearClipPlane = 0.03f;

        int w = 1280, h = 720;
        RenderTexture rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;

        string full = Path.GetFullPath(Path.Combine(Application.dataPath, "..", outPath));
        Directory.CreateDirectory(Path.GetDirectoryName(full));
        File.WriteAllBytes(full, tex.EncodeToPNG());

        cam.targetTexture = null;
        Object.DestroyImmediate(tex); Object.DestroyImmediate(rt); Object.DestroyImmediate(camGo);
        return "salvo " + full;
    }
}
