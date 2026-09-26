using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class PlotProbe
{
    // Inventaria o que ocupa o terreno pretendido para o FECAP.
    public static string Run(string x0s, string x1s, string z0s, string z1s)
    {
        float x0 = P(x0s), x1 = P(x1s), z0 = P(z0s), z1 = P(z1s);
        StringBuilder sb = new StringBuilder();
        GameObject world = GameObject.Find("City 02/World");
        Dictionary<string, int> counts = new Dictionary<string, int>();

        foreach (Transform grp in world.transform)
        {
            int n = 0;
            foreach (MeshRenderer r in grp.GetComponentsInChildren<MeshRenderer>(true))
            {
                Vector3 p = r.transform.position;
                if (p.x >= x0 && p.x <= x1 && p.z >= z0 && p.z <= z1) n++;
            }
            if (n > 0) counts[grp.name] = n;
        }
        sb.AppendLine("terreno X[" + x0 + ".." + x1 + "] Z[" + z0 + ".." + z1 + "]");
        foreach (KeyValuePair<string, int> kv in counts)
            sb.AppendLine("   " + kv.Key + ": " + kv.Value);
        return sb.ToString();
    }
    static float P(string s) { return float.Parse(s, System.Globalization.CultureInfo.InvariantCulture); }
}
