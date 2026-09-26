using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class GridProbe
{
    // Mapeia posicoes de via e calcada perto do centro, para achar o
    // quarteirao livre onde o FECAP cabe sem invadir a rua.
    public static string Run()
    {
        StringBuilder sb = new StringBuilder();
        GameObject world = GameObject.Find("City 02/World");

        SortedSet<int> roadX = new SortedSet<int>();
        SortedSet<int> roadZ = new SortedSet<int>();
        Transform road = world.transform.Find("Road");
        foreach (Transform t in road)
        {
            Vector3 p = t.position;
            if (Mathf.Abs(p.x) > 70f || Mathf.Abs(p.z) > 70f) continue;
            roadX.Add(Mathf.RoundToInt(p.x));
            roadZ.Add(Mathf.RoundToInt(p.z));
        }
        sb.AppendLine("-- vias dentro do quarteirao X[-24..24] Z[2..30] --");
        foreach (Transform t in road)
        {
            Vector3 p2 = t.position;
            if (p2.x < -24f || p2.x > 24f || p2.z < 2f || p2.z > 30f) continue;
            sb.AppendLine("   " + t.name + " " + p2.ToString("F1"));
        }
        sb.AppendLine("-- calcadas idem (amostra) --");
        Transform side = world.transform.Find("Tiles (Sidewalks)");
        int k = 0;
        foreach (Transform t in side)
        {
            Vector3 p3 = t.position;
            if (p3.x < -24f || p3.x > 24f || p3.z < 2f || p3.z > 30f) continue;
            if (k++ < 10) sb.AppendLine("   " + t.name + " " + p3.ToString("F1"));
        }
        sb.AppendLine("   total calcadas no quarteirao: " + k);

        // prédios originais do quarteirao (agora desativados)
        Transform b = world.transform.Find("Buildings");
        sb.AppendLine("-- predios desativados perto do centro --");
        foreach (Transform t in b)
        {
            if (t.gameObject.activeSelf) continue;
            Vector3 p = t.position;
            if (Mathf.Abs(p.x) > 50f || Mathf.Abs(p.z) > 50f) continue;
            Renderer r = t.GetComponentInChildren<Renderer>(true);
            sb.AppendLine("   " + t.name + " pos=" + p.ToString("F1")
                + (r != null ? " tam=" + r.bounds.size.ToString("F1") : ""));
        }
        return sb.ToString();
    }
}
