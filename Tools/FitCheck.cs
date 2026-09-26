using System.Text;
using UnityEngine;

public static class FitCheck
{
    public static string Run()
    {
        StringBuilder sb = new StringBuilder();
        GameObject f = GameObject.Find("FECAP");
        if (f == null) return "FECAP ausente";
        Renderer[] rs = f.GetComponentsInChildren<Renderer>(true);
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        sb.AppendLine("FECAP X[" + b.min.x.ToString("F1") + ".." + b.max.x.ToString("F1") + "]"
            + " Z[" + b.min.z.ToString("F1") + ".." + b.max.z.ToString("F1") + "]"
            + " altura=" + b.max.y.ToString("F1"));
        sb.AppendLine("limite do quarteirao: calcadas em x=+-19.5, vias em x=+-24, calcada z=4.5 e z=25");
        bool okX = b.min.x > -19.6f && b.max.x < 19.6f;
        bool okZ = b.min.z > 3.8f && b.max.z < 25.4f;
        sb.AppendLine("dentro em X: " + okX + "   dentro em Z: " + okZ);

        // o spawn esta livre?
        InspectionGame g = Object.FindFirstObjectByType<InspectionGame>();
        if (g != null)
        {
            Vector3 sp = g.spawnPosition;
            Collider[] hits = Physics.OverlapSphere(sp + Vector3.up * 0.6f, 0.6f, ~0,
                QueryTriggerInteraction.Ignore);
            sb.AppendLine("spawn " + sp.ToString("F1") + " -> " + hits.Length
                + " colliders solidos sobrepostos (0 = livre)");
            RaycastHit rh;
            if (Physics.Raycast(sp + Vector3.up * 20f, Vector3.down, out rh, 60f))
                sb.AppendLine("chao sob o spawn: y=" + rh.point.y.ToString("F2") + " '" + rh.collider.name + "'");
            sb.AppendLine("distancia do spawn ate a fachada (z=7): " + (7f - sp.z).ToString("F1") + "m");
        }
        return sb.ToString();
    }
}
