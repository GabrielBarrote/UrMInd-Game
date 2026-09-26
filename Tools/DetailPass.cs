using System;
using System.IO;
using UnityEngine;
using UnityEditor;

// Gerador procedural de texturas de detalhe (rachaduras, buracos, grão de asfalto).
// Executado via `unity command run_script`. Não faz parte do build.
public static class DetailPass
{
    const string TexDir = "Assets/Detail/Textures";

    // ---------------------------------------------------------------- utils

    static void EnsureDir()
    {
        Directory.CreateDirectory(TexDir);
    }

    static float Smoothstep(float a, float b, float x)
    {
        if (b - a < 1e-6f) return x < a ? 0f : 1f;
        float t = Mathf.Clamp01((x - a) / (b - a));
        return t * t * (3f - 2f * t);
    }

    // Carimba um disco macio no campo de altura, acumulando pelo máximo.
    static void Stamp(float[,] h, int size, float cx, float cy, float radius, float depth)
    {
        int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - radius) - 1);
        int x1 = Mathf.Min(size - 1, Mathf.CeilToInt(cx + radius) + 1);
        int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - radius) - 1);
        int y1 = Mathf.Min(size - 1, Mathf.CeilToInt(cy + radius) + 1);

        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d > radius) continue;
                // núcleo cheio até 35% do raio, decaindo suave até a borda
                float v = 1f - Smoothstep(radius * 0.35f, radius, d);
                v *= depth;
                if (v > h[x, y]) h[x, y] = v;
            }
        }
    }

    // Força de retorno quando a posição normalizada entra na margem de 18%.
    static float EdgePush(float p)
    {
        if (p < 0.18f) return (0.18f - p) / 0.18f * 0.40f;
        if (p > 0.82f) return -(p - 0.82f) / 0.18f * 0.40f;
        return 0f;
    }

    // Caminhante de fratura: anda numa direção com jitter, afinando nas pontas,
    // e ramifica recursivamente. É o que dá a silhueta orgânica de rachadura.
    static void Walk(float[,] h, int size, System.Random rng,
                     Vector2 pos, Vector2 dir, float length,
                     float startRadius, int depthLevel,
                     float branchChance, Vector2 confine, int[] budget)
    {
        if (depthLevel > 3 || length < 6f) return;

        float step = 1.4f;
        int steps = Mathf.Max(2, Mathf.RoundToInt(length / step));
        dir = dir.normalized;

        for (int i = 0; i < steps; i++)
        {
            float t = (float)i / steps;
            // afina nas duas pontas
            float taper = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
            taper = Mathf.Pow(taper, 0.45f);
            float r = Mathf.Max(0.8f, startRadius * taper);

            Stamp(h, size, pos.x, pos.y, r, 0.55f + 0.45f * taper);

            // jitter angular — desvio pequeno e correlacionado
            float jitter = ((float)rng.NextDouble() - 0.5f) * 0.30f;
            float ca = Mathf.Cos(jitter);
            float sa = Mathf.Sin(jitter);
            dir = new Vector2(dir.x * ca - dir.y * sa, dir.x * sa + dir.y * ca).normalized;

            // contenção: empurra de volta quando se aproxima da borda, para a
            // fratura não escapar da textura e deixar o decal com o centro vazio
            dir.x += EdgePush(pos.x / size) * confine.x;
            dir.y += EdgePush(pos.y / size) * confine.y;
            dir = dir.normalized;

            pos += dir * step;

            if (pos.x < -20f || pos.x > size + 20f || pos.y < -20f || pos.y > size + 20f) return;

            // ramificação
            bool canBranch = i > steps / 6 && i < steps * 5 / 6;
            if (canBranch && budget[0] > 0 && rng.NextDouble() < branchChance && depthLevel < 3)
            {
                budget[0]--;
                float side = rng.NextDouble() < 0.5 ? 1f : -1f;
                float ang = side * (0.5f + (float)rng.NextDouble() * 0.7f);
                float bc = Mathf.Cos(ang);
                float bs = Mathf.Sin(ang);
                Vector2 bdir = new Vector2(dir.x * bc - dir.y * bs, dir.x * bs + dir.y * bc);
                Walk(h, size, rng, pos, bdir,
                     length * (0.30f + (float)rng.NextDouble() * 0.30f),
                     startRadius * 0.55f, depthLevel + 1,
                     branchChance * 0.85f, new Vector2(1f, 1f), budget);
            }
        }
    }

    // Ruído de valor suave, para quebrar as bordas e variar o albedo.
    static float ValueNoise(System.Random seedRng, float[,] cache, int size, int x, int y)
    {
        return cache[x, y];
    }

    static float[,] MakeNoise(int size, int seed, int cells)
    {
        System.Random rng = new System.Random(seed);
        float[,] grid = new float[cells + 1, cells + 1];
        for (int j = 0; j <= cells; j++)
            for (int i = 0; i <= cells; i++)
                grid[i, j] = (float)rng.NextDouble();

        float[,] outp = new float[size, size];
        float scale = (float)cells / size;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float fx = x * scale;
                float fy = y * scale;
                int ix = Mathf.FloorToInt(fx);
                int iy = Mathf.FloorToInt(fy);
                float tx = fx - ix;
                float ty = fy - iy;
                tx = tx * tx * (3f - 2f * tx);
                ty = ty * ty * (3f - 2f * ty);
                int ix1 = Mathf.Min(ix + 1, cells);
                int iy1 = Mathf.Min(iy + 1, cells);
                float a = Mathf.Lerp(grid[ix, iy], grid[ix1, iy], tx);
                float b = Mathf.Lerp(grid[ix, iy1], grid[ix1, iy1], tx);
                outp[x, y] = Mathf.Lerp(a, b, ty);
            }
        }
        return outp;
    }

    static void SavePng(Texture2D tex, string path)
    {
        byte[] png = tex.EncodeToPNG();
        File.WriteAllBytes(path, png);
        UnityEngine.Object.DestroyImmediate(tex);
    }

    // ------------------------------------------------------------- entradas

    public static void GenerateCrackTextures()
    {
        EnsureDir();
        int size = 1024;

        for (int variant = 0; variant < 3; variant++)
        {
            System.Random rng = new System.Random(20260924 + variant * 77);
            float[,] h = new float[size, size];

            // variante 0 = leve (BAIXA), 1 = média, 2 = severa/couro-de-jacaré (ALTA)
            // orçamento total de ramificações por variante — controle direto da
            // densidade, ao contrário da probabilidade por passo que explode
            int[] budgetByVariant = new int[] { 7, 20, 45 };
            int[] secondaryByVariant = new int[] { 1, 3, 5 };
            int[] budget = new int[] { budgetByVariant[variant] };
            float branch = 0.06f;

            // fratura principal atravessando a textura no eixo Y; confinada em X
            Vector2 start = new Vector2(size * (0.4f + (float)rng.NextDouble() * 0.2f), -10f);
            Walk(h, size, rng, start, new Vector2(((float)rng.NextDouble() - 0.5f) * 0.4f, 1f),
                 size * 1.25f, 11f + variant * 2.5f, 0, branch, new Vector2(1f, 0f), budget);

            // fraturas secundárias independentes, confinadas nos dois eixos
            for (int k = 0; k < secondaryByVariant[variant]; k++)
            {
                Vector2 s2 = new Vector2(size * (0.25f + 0.5f * (float)rng.NextDouble()),
                                         size * (0.25f + 0.5f * (float)rng.NextDouble()));
                float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
                Walk(h, size, rng, s2, new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)),
                     size * (0.25f + 0.25f * (float)rng.NextDouble()), 6.0f, 1,
                     branch, new Vector2(1f, 1f), budget);
            }

            float[,] fine = MakeNoise(size, 900 + variant, 220);
            float[,] coarse = MakeNoise(size, 1700 + variant, 14);

            Texture2D albedo = new Texture2D(size, size, TextureFormat.RGBA32, true, true);
            Texture2D height = new Texture2D(size, size, TextureFormat.RGBA32, true, true);
            Color[] aPix = new Color[size * size];
            float[,] hFinal = new float[size, size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float v = h[x, y];
                    // borda irregular: ruído fino corrói o contorno da fratura
                    v *= 0.72f + 0.55f * fine[x, y];
                    v = Mathf.Clamp01(v);
                    // limiar suave -> máscara de alpha
                    float mask = Smoothstep(0.18f, 0.52f, v);
                    hFinal[x, y] = mask;

                    // interior mais escuro no núcleo, esfarelado nas bordas
                    float dark = Mathf.Lerp(0.20f, 0.062f, mask);
                    dark *= 0.8f + 0.4f * coarse[x, y];
                    aPix[y * size + x] = new Color(dark * 1.06f, dark, dark * 0.92f, mask);
                }
            }
            albedo.SetPixels(aPix);
            albedo.Apply();
            SavePng(albedo, TexDir + "/crack_" + variant + "_albedo.png");

            // normal map a partir da altura (rachadura é AFUNDADA -> altura negativa)
            Texture2D normal = new Texture2D(size, size, TextureFormat.RGBA32, true, true);
            Color[] nPix = new Color[size * size];
            float strength = 3.2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int xm = Mathf.Max(0, x - 1);
                    int xp = Mathf.Min(size - 1, x + 1);
                    int ym = Mathf.Max(0, y - 1);
                    int yp = Mathf.Min(size - 1, y + 1);
                    float dx = (-hFinal[xp, y]) - (-hFinal[xm, y]);
                    float dy = (-hFinal[x, yp]) - (-hFinal[x, ym]);
                    Vector3 n = new Vector3(-dx * strength, -dy * strength, 1f).normalized;
                    nPix[y * size + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
                }
            }
            normal.SetPixels(nPix);
            normal.Apply();
            SavePng(normal, TexDir + "/crack_" + variant + "_normal.png");

            // MAOS: R=metallic, G=AO, B=height(unused), A=smoothness
            Texture2D maos = new Texture2D(size, size, TextureFormat.RGBA32, true, true);
            Color[] mPix = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float m = hFinal[x, y];
                    float ao = Mathf.Lerp(1f, 0.25f, m);
                    float smooth = Mathf.Lerp(0.35f, 0.04f, m); // fundo da fenda é fosco
                    mPix[y * size + x] = new Color(0f, ao, 0f, smooth);
                }
            }
            maos.SetPixels(mPix);
            maos.Apply();
            SavePng(maos, TexDir + "/crack_" + variant + "_maos.png");
        }

        AssetDatabase.Refresh();
        Debug.Log("[DetailPass] 3 variantes de rachadura geradas em " + TexDir);
    }

    // Ajusta import settings: normal map marcado como normal, resto linear.
    public static void ConfigureCrackImporters()
    {
        for (int v = 0; v < 3; v++)
        {
            string nPath = TexDir + "/crack_" + v + "_normal.png";
            TextureImporter ni = AssetImporter.GetAtPath(nPath) as TextureImporter;
            if (ni != null)
            {
                ni.textureType = TextureImporterType.NormalMap;
                ni.wrapMode = TextureWrapMode.Clamp;
                ni.SaveAndReimport();
            }

            string aPath = TexDir + "/crack_" + v + "_albedo.png";
            TextureImporter ai = AssetImporter.GetAtPath(aPath) as TextureImporter;
            if (ai != null)
            {
                ai.textureType = TextureImporterType.Default;
                ai.alphaIsTransparency = true;
                ai.sRGBTexture = true;
                ai.wrapMode = TextureWrapMode.Clamp;
                ai.SaveAndReimport();
            }

            string mPath = TexDir + "/crack_" + v + "_maos.png";
            TextureImporter mi = AssetImporter.GetAtPath(mPath) as TextureImporter;
            if (mi != null)
            {
                mi.textureType = TextureImporterType.Default;
                mi.sRGBTexture = false;
                mi.wrapMode = TextureWrapMode.Clamp;
                mi.SaveAndReimport();
            }
        }
        Debug.Log("[DetailPass] importers configurados");
    }
}

