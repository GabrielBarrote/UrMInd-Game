using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class SpawnClear
{
    // Procura um ponto de largada na via em frente a FECAP onde a CAMERA
    // tambem fique livre. A copa das arvores nao tem collider, entao um
    // SphereCast nao a detecta: aqui testamos contra os bounds dos renderers.
    public static string Run()
    {
        StringBuilder sb = new StringBuilder();
        InspectionGame g = Object.FindFirstObjectByType<InspectionGame>();
        AnalysisCartController c = Object.FindFirstObjectByType<AnalysisCartController>();
        Vector3 off = c.cameraOffset;

        List<Renderer> blockers = new List<Renderer>();
        foreach (MeshRenderer r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            if (!r.gameObject.activeInHierarchy) continue;
            Vector3 p = r.bounds.center;
            if (p.x < -40f || p.x > 40f || p.z < -30f || p.z > 12f) continue;
            if (r.transform.IsChildOf(c.transform)) continue;
            blockers.Add(r);
        }
        sb.AppendLine("renderers candidatos na faixa da rua: " + blockers.Count);

        float bestX = 0f; float bestGap = -1f;
        for (float z = -2f; z <= 3.5f; z += 0.5f)
        {
            float x = 0f;
            Vector3 spawn = new Vector3(x, 0.2f, z);
            Vector3 cam = spawn + new Vector3(0f, off.y, off.z);   // yaw 0
            float near = 999f;
            foreach (Renderer r in blockers)
            {
                float d = Vector3.Distance(r.bounds.ClosestPoint(cam), cam);
                if (d < near) near = d;
            }
            sb.AppendLine("z=" + z.ToString("F1") + " camera em " + cam.ToString("F1")
                + " folga=" + near.ToString("F2") + "m");
            if (near > bestGap) { bestGap = near; bestX = z; }
        }
        sb.AppendLine("MELHOR: z=" + bestX.ToString("F1") + " folga=" + bestGap.ToString("F2") + "m");
        return sb.ToString();
    }
}
