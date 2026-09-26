using System.IO;
using UnityEngine;
using UnityEditor;

// Gera a textura de detalhe empacotada usada pelo shader triplanar.
// RGBA num unico sample:
//   R,G = normal do grao (XY codificado 0..1)
//   B   = variacao macro de baixa frequencia (quebra a cor chapada)
//   A   = manchas finas (variacao de smoothness)
public static class GrainPass
{
    const string TexDir = "Assets/Detail/Textures";
    const string OutPath = TexDir + "/detail_pack.png";

    // Ruido de valor TILEAVEL: os indices da grade dao a volta.
    static float[,] Tileable(int size, int seed, int cells)
    {
        System.Random rng = new System.Random(seed);
        float[,] g = new float[cells, cells];
        for (int j = 0; j < cells; j++)
            for (int i = 0; i < cells; i++)
                g[i, j] = (float)rng.NextDouble();

        float[,] o = new float[size, size];
        float scale = (float)cells / size;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float fx = x * scale, fy = y * scale;
                int ix = Mathf.FloorToInt(fx), iy = Mathf.FloorToInt(fy);
                float tx = fx - ix, ty = fy - iy;
                tx = tx * tx * (3f - 2f * tx);
                ty = ty * ty * (3f - 2f * ty);
                int ix0 = ((ix % cells) + cells) % cells;
                int iy0 = ((iy % cells) + cells) % cells;
                int ix1 = (ix0 + 1) % cells;
                int iy1 = (iy0 + 1) % cells;
                float a = Mathf.Lerp(g[ix0, iy0], g[ix1, iy0], tx);
                float b = Mathf.Lerp(g[ix0, iy1], g[ix1, iy1], tx);
                o[x, y] = Mathf.Lerp(a, b, ty);
            }
        }
        return o;
    }

    static float[,] Fbm(int size, int seed, int baseCells, int octaves)
    {
        float[,] sum = new float[size, size];
        float amp = 1f, norm = 0f;
        int cells = baseCells;
        for (int o = 0; o < octaves; o++)
        {
            float[,] layer = Tileable(size, seed + o * 131, cells);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    sum[x, y] += layer[x, y] * amp;
            norm += amp;
            amp *= 0.5f;
            cells *= 2;
        }
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                sum[x, y] /= norm;
        return sum;
    }

    public static string Generate()
    {
        Directory.CreateDirectory(TexDir);
        int size = 512;

        // altura do grao: varias oitavas, dominada pela alta frequencia
        float[,] h = Fbm(size, 4242, 16, 5);
        // variacao macro: baixa frequencia, para manchar a cor chapada
        float[,] macro = Fbm(size, 9090, 3, 3);
        // manchas finas para smoothness
        float[,] fine = Fbm(size, 7373, 32, 2);

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true, true);
        Color[] px = new Color[size * size];
        float strength = 6.0f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // sobel com wrap para a normal continuar tileavel
                int xm = (x - 1 + size) % size, xp = (x + 1) % size;
                int ym = (y - 1 + size) % size, yp = (y + 1) % size;
                float dx = (h[xp, y] - h[xm, y]) * strength;
                float dy = (h[x, yp] - h[x, ym]) * strength;
                Vector3 n = new Vector3(-dx, -dy, 1f).normalized;

                px[y * size + x] = new Color(
                    n.x * 0.5f + 0.5f,
                    n.y * 0.5f + 0.5f,
                    macro[x, y],
                    fine[x, y]);
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        File.WriteAllBytes(OutPath, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);

        AssetDatabase.Refresh();

        TextureImporter ti = AssetImporter.GetAtPath(OutPath) as TextureImporter;
        if (ti != null)
        {
            ti.textureType = TextureImporterType.Default;
            ti.sRGBTexture = false;          // dados, nao cor
            ti.wrapMode = TextureWrapMode.Repeat;
            ti.filterMode = FilterMode.Bilinear;
            ti.mipmapEnabled = true;
            ti.alphaSource = TextureImporterAlphaSource.FromInput;
            ti.alphaIsTransparency = false;
            ti.SaveAndReimport();
        }
        return "gerado " + OutPath;
    }
}
