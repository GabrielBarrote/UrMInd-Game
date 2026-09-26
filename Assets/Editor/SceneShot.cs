using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Captura da cena a partir de posicoes arbitrarias, sem entrar em Play.
// Serve para comparar o que a cena contem com o que o build mostra.
public static class SceneShot
{
    static readonly Vector3[] Spots =
    {
        new Vector3(0f, 2.5f, -8f),      // atras do spawn, olhando a FECAP
        new Vector3(0f, 2.5f, -8f),      // mesmo ponto, olhando para tras
        new Vector3(0f, 120f, -2f),      // de cima, visao geral
        new Vector3(-10f, 7.5f, -7f),    // bandeiras de frente
        new Vector3(-2f, 7.5f, -2f),     // bandeiras de lado, para ver o Z
    };

    static readonly Vector3[] Eulers =
    {
        new Vector3(5f, 0f, 0f),
        new Vector3(5f, 180f, 0f),
        new Vector3(70f, 0f, 0f),
        new Vector3(0f, 0f, 0f),
        new Vector3(0f, -60f, 0f),
    };

    public static void Shoot()
    {
        EditorSceneManager.OpenScene("Assets/City - 02 - Day.unity", OpenSceneMode.Single);
        Directory.CreateDirectory("Tools/shots");

        var go = new GameObject("__shotcam");
        var cam = go.AddComponent<Camera>();
        cam.fieldOfView = 60f;
        cam.farClipPlane = 1500f;
        cam.nearClipPlane = 0.3f;

        var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
        var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        cam.targetTexture = rt;

        for (int i = 0; i < Spots.Length; i++)
        {
            go.transform.position = Spots[i];
            go.transform.rotation = Quaternion.Euler(Eulers[i]);
            cam.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            string path = "Tools/shots/diag_" + i + ".png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Debug.Log("[SceneShot] wrote " + path);
        }

        cam.targetTexture = null;
        Object.DestroyImmediate(go);
    }
}
