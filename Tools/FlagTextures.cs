using System.IO;
using UnityEngine;
using UnityEditor;

// Bandeiras dos mastros da entrada, desenhadas proceduralmente.
public static class FlagTextures
{
    const string Dir = "Assets/Fecap/Textures";

    public static string Run()
    {
        Directory.CreateDirectory(Dir);
        Brazil();
        SaoPaulo();
        Fecap();
        AssetDatabase.Refresh();
        foreach (string f in new string[] { "flag_br.png", "flag_sp.png", "flag_fecap.png" })
        {
            TextureImporter ti = AssetImporter.GetAtPath(Dir + "/" + f) as TextureImporter;
            if (ti == null) continue;
            ti.textureType = TextureImporterType.Default;
            ti.sRGBTexture = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.SaveAndReimport();
        }
        return "3 bandeiras geradas";
    }

    static void Write(Texture2D t, string name)
    {
        t.Apply();
        File.WriteAllBytes(Dir + "/" + name, t.EncodeToPNG());
        Object.DestroyImmediate(t);
    }

    static void Brazil()
    {
        int W = 420, H = 294;
        Texture2D t = new Texture2D(W, H, TextureFormat.RGBA32, false, true);
        Color green = new Color(0.00f, 0.60f, 0.32f);
        Color yellow = new Color(1.00f, 0.87f, 0.16f);
        Color blue = new Color(0.02f, 0.14f, 0.50f);
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float u = x / (float)W, v = y / (float)H;
                Color c = green;
                // losango: |u-.5|/a + |v-.5|/b <= 1
                float d = Mathf.Abs(u - 0.5f) / 0.42f + Mathf.Abs(v - 0.5f) / 0.42f;
                if (d <= 1f) c = yellow;
                float dx = (u - 0.5f) * W, dy = (v - 0.5f) * H;
                if (dx * dx + dy * dy <= 58f * 58f) c = blue;
                t.SetPixel(x, y, c);
            }
        Write(t, "flag_br.png");
    }

    static void SaoPaulo()
    {
        int W = 420, H = 294;
        Texture2D t = new Texture2D(W, H, TextureFormat.RGBA32, false, true);
        Color black = new Color(0.07f, 0.07f, 0.09f);
        Color white = new Color(0.95f, 0.95f, 0.95f);
        Color red = new Color(0.78f, 0.09f, 0.12f);
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                int band = Mathf.FloorToInt(y / (float)H * 13f);
                Color c = (band % 2 == 0) ? white : black;
                // canton vermelho no canto superior esquerdo
                if (x < W * 0.42f && y > H * 0.58f)
                {
                    c = red;
                    float u = (x / (W * 0.42f)) - 0.5f;
                    float v = ((y - H * 0.58f) / (H * 0.42f)) - 0.5f;
                    if (Mathf.Abs(u) / 0.42f + Mathf.Abs(v) / 0.42f <= 1f) c = white;
                }
                t.SetPixel(x, y, c);
            }
        Write(t, "flag_sp.png");
    }

    static void Fecap()
    {
        int W = 420, H = 294;
        Texture2D t = new Texture2D(W, H, TextureFormat.RGBA32, false, true);
        Color green = new Color(0.02f, 0.36f, 0.24f);
        Color white = new Color(0.95f, 0.96f, 0.95f);
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                Color c = green;
                float dx = x - W * 0.5f, dy = y - H * 0.5f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                if (r < 82f && r > 70f) c = white;      // anel do brasao
                t.SetPixel(x, y, c);
            }
        Write(t, "flag_fecap.png");
    }
}
