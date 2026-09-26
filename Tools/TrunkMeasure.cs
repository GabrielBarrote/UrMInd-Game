using System.Text;
using UnityEngine;
using UnityEditor;

public static class TrunkMeasure
{
    public static string Run()
    {
        StringBuilder sb = new StringBuilder();
        string[] paths = new string[] {
            "Assets/City 02/Third Party/Kenney/City Kit (Suburban)/tree-large.fbx",
            "Assets/City 02/Third Party/Kenney/City Kit (Suburban)/tree-small.fbx" };

        foreach (string p in paths)
        {
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(p))
            {
                Mesh m = o as Mesh;
                if (m == null) continue;
                Vector3[] v = m.vertices;
                sb.AppendLine("=== " + m.name + " verts=" + v.Length
                    + " boundsY=" + m.bounds.min.y.ToString("F3") + ".." + m.bounds.max.y.ToString("F3") + " ===");

                // varre em faixas de altura e mede o raio maximo em cada faixa:
                // o tronco e a faixa fina de baixo, a copa e a larga de cima
                float top = m.bounds.max.y;
                int bands = 8;
                for (int b = 0; b < bands; b++)
                {
                    float lo = top * b / bands;
                    float hi = top * (b + 1) / bands;
                    float maxR = 0f;
                    int count = 0;
                    foreach (Vector3 vv in v)
                    {
                        if (vv.y < lo || vv.y >= hi) continue;
                        float r = Mathf.Sqrt(vv.x * vv.x + vv.z * vv.z);
                        if (r > maxR) maxR = r;
                        count++;
                    }
                    if (count > 0)
                        sb.AppendLine("  Y " + lo.ToString("F3") + ".." + hi.ToString("F3")
                            + " raioMax=" + maxR.ToString("F4") + " (" + count + " verts)");
                }
                break;
            }
        }
        return sb.ToString();
    }
}
