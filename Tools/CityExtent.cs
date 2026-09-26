using System.Text;
using UnityEngine;

public static class CityExtent
{
    public static string Run()
    {
        StringBuilder sb = new StringBuilder();
        GameObject world = GameObject.Find("City 02/World");
        foreach (string g in new string[] { "Ground", "Road", "Buildings", "Tiles (Sidewalks)", "Light Poles", "Trees" })
        {
            Transform t = world.transform.Find(g);
            if (t == null) { sb.AppendLine(g + ": ausente"); continue; }
            bool first = true;
            Bounds b = new Bounds();
            int n = 0;
            foreach (MeshRenderer r in t.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds);
                n++;
            }
            if (first) { sb.AppendLine(g + ": sem renderers"); continue; }
            sb.AppendLine(g + " (" + n + ") centro=" + b.center.ToString("F1")
                + " tamanho=" + b.size.ToString("F1")
                + " X[" + b.min.x.ToString("F0") + ".." + b.max.x.ToString("F0") + "]"
                + " Z[" + b.min.z.ToString("F0") + ".." + b.max.z.ToString("F0") + "]");
        }
        return sb.ToString();
    }
}
