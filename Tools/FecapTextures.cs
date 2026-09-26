using System.IO;
using UnityEngine;
using UnityEditor;

// Texturas proceduraisl do predio FECAP: tijolo aparente e bloco de vidro.
// Ambas tileaveis em 1m x 1m, para o tiling ser setado em metros no material.
public static class FecapTextures
{
    const string Dir = "Assets/Fecap/Textures";

    static void Save(Texture2D t, string path)
    {
        File.WriteAllBytes(path, t.EncodeToPNG());
        Object.DestroyImmediate(t);
    }

    static float Hash(int x, int y, int seed)
    {
        int h = x * 374761393 + y * 668265263 + seed * 1442695040;
        h = (h ^ (h >> 13)) * 1274126177;
        return ((h ^ (h >> 16)) & 0x7fffffff) / (float)0x7fffffff;
    }

    static float Smooth(float a, float b, float x)
    {
        float t = Mathf.Clamp01((x - a) / Mathf.Max(1e-5f, b - a));
        return t * t * (3f - 2f * t);
    }

    // ------------------------------------------------------------- TIJOLO

    public static string Brick()
    {
        Directory.CreateDirectory(Dir);
        int S = 1024;
        int cols = 5;      // tijolos por metro na horizontal
        int rows = 12;     // fiadas por metro
        float mortar = 0.055f;   // espessura da junta, em fracao da celula

        Texture2D alb = new Texture2D(S, S, TextureFormat.RGBA32, true, true);
        Color[] ap = new Color[S * S];
        float[,] height = new float[S, S];

        for (int y = 0; y < S; y++)
        {
            float fy = (float)y / S * rows;
            int row = Mathf.FloorToInt(fy);
            float ty = fy - row;

            // fiada alternada deslocada meio tijolo (amarracao corrida).
            // rows par garante que o padrao fecha no topo sem costura.
            float shift = (row % 2 == 0) ? 0f : 0.5f;

            for (int x = 0; x < S; x++)
            {
                float fx = (float)x / S * cols + shift;
                int col = Mathf.FloorToInt(fx);
                float tx = fx - col;

                // distancia ate a junta mais proxima nos dois eixos
                float dx = Mathf.Min(tx, 1f - tx);
                float dy = Mathf.Min(ty, 1f - ty);
                float mx = Smooth(0f, mortar, dx);
                float my = Smooth(0f, mortar * (cols / (float)rows), dy);
                float brickMask = Mathf.Min(mx, my);

                // variacao de tom por tijolo
                float v = Hash(col, row, 7);
                float v2 = Hash(col * 3 + 1, row * 5 + 2, 19);

                Color brick = new Color(
                    Mathf.Lerp(0.42f, 0.62f, v),
                    Mathf.Lerp(0.16f, 0.25f, v * 0.7f + v2 * 0.3f),
                    Mathf.Lerp(0.11f, 0.17f, v2));
                // granulado fino dentro do tijolo
                float grain = (Hash(x, y, 91) - 0.5f) * 0.06f;
                brick.r += grain; brick.g += grain; brick.b += grain;

                Color mortarC = new Color(0.74f, 0.72f, 0.68f);
                float mg = (Hash(x / 2, y / 2, 55) - 0.5f) * 0.05f;
                mortarC.r += mg; mortarC.g += mg; mortarC.b += mg;

                Color c = Color.Lerp(mortarC, brick, brickMask);
                ap[y * S + x] = new Color(c.r, c.g, c.b, 1f);
                height[x, y] = brickMask;      // tijolo sobressai, junta afunda
            }
        }
        alb.SetPixels(ap); alb.Apply();
        Save(alb, Dir + "/brick_albedo.png");
        SaveNormalFrom(height, S, 2.4f, Dir + "/brick_normal.png");

        AssetDatabase.Refresh();
        Config(Dir + "/brick_albedo.png", true, false);
        Config(Dir + "/brick_normal.png", false, true);
        return "tijolo gerado";
    }

    // ------------------------------------------------------ BLOCO DE VIDRO

    public static string GlassBlock()
    {
        Directory.CreateDirectory(Dir);
        int S = 1024;
        int n = 5;               // blocos por metro
        float joint = 0.045f;

        Texture2D alb = new Texture2D(S, S, TextureFormat.RGBA32, true, true);
        Color[] ap = new Color[S * S];
        float[,] height = new float[S, S];

        for (int y = 0; y < S; y++)
        {
            float fy = (float)y / S * n;
            int row = Mathf.FloorToInt(fy);
            float ty = fy - row;
            for (int x = 0; x < S; x++)
            {
                float fx = (float)x / S * n;
                int col = Mathf.FloorToInt(fx);
                float tx = fx - col;

                float dx = Mathf.Min(tx, 1f - tx);
                float dy = Mathf.Min(ty, 1f - ty);
                float d = Mathf.Min(dx, dy);
                float face = Smooth(joint, joint * 3.2f, d);   // chanfro do bloco

                // o vidro tem leve variacao e um brilho diagonal interno
                float diag = Mathf.Sin((tx + ty) * Mathf.PI) * 0.5f + 0.5f;
                float v = Hash(col, row, 31);
                Color glass = Color.Lerp(
                    new Color(0.62f, 0.72f, 0.76f),
                    new Color(0.80f, 0.88f, 0.90f),
                    diag * 0.7f + v * 0.3f);

                Color frame = new Color(0.70f, 0.70f, 0.68f);
                Color c = Color.Lerp(frame, glass, face);
                ap[y * S + x] = new Color(c.r, c.g, c.b, 1f);
                height[x, y] = face;
            }
        }
        alb.SetPixels(ap); alb.Apply();
        Save(alb, Dir + "/glassblock_albedo.png");
        SaveNormalFrom(height, S, 3.0f, Dir + "/glassblock_normal.png");

        AssetDatabase.Refresh();
        Config(Dir + "/glassblock_albedo.png", true, false);
        Config(Dir + "/glassblock_normal.png", false, true);
        return "bloco de vidro gerado";
    }

    // -------------------------------------------------------------- utils

    static void SaveNormalFrom(float[,] h, int S, float strength, string path)
    {
        Texture2D n = new Texture2D(S, S, TextureFormat.RGBA32, true, true);
        Color[] np = new Color[S * S];
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                int xm = (x - 1 + S) % S, xp = (x + 1) % S;
                int ym = (y - 1 + S) % S, yp = (y + 1) % S;
                float dx = (h[xp, y] - h[xm, y]) * strength;
                float dy = (h[x, yp] - h[x, ym]) * strength;
                Vector3 v = new Vector3(-dx, -dy, 1f).normalized;
                np[y * S + x] = new Color(v.x * .5f + .5f, v.y * .5f + .5f, v.z * .5f + .5f, 1f);
            }
        n.SetPixels(np); n.Apply();
        Save(n, path);
    }

    static void Config(string path, bool srgb, bool isNormal)
    {
        TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) return;
        ti.textureType = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        ti.sRGBTexture = srgb;
        ti.wrapMode = TextureWrapMode.Repeat;
        ti.mipmapEnabled = true;
        ti.anisoLevel = 8;
        ti.SaveAndReimport();
    }
}
