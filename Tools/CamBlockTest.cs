using System.Text;
using UnityEngine;
using UnityEditor;

public static class CamBlockTest
{
    // Varre orientacoes do rover procurando uma em que a camera ficaria dentro
    // de geometria, e mostra para onde ela recuaria.
    public static string Run()
    {
        StringBuilder sb = new StringBuilder();
        CharacterController cc = Object.FindFirstObjectByType<CharacterController>();
        Transform p = cc.transform;
        AnalysisCartController ctrl = p.GetComponent<AnalysisCartController>();

        Quaternion savedRot = p.rotation;
        Vector3 savedPos = p.position;

        int blockedCount = 0;
        for (int deg = 0; deg < 360; deg += 30)
        {
            p.rotation = Quaternion.Euler(0f, deg, 0f);

            Vector3 pivot = p.position + Vector3.up * ctrl.lookHeight;
            Vector3 desired = p.TransformPoint(ctrl.cameraOffset);
            Vector3 toCam = desired - pivot;
            float full = toCam.magnitude;
            Vector3 dir = toCam / full;
            float startOffset = cc.radius + ctrl.cameraProbeRadius + 0.05f;

            RaycastHit hit;
            bool blocked = Physics.SphereCast(pivot + dir * startOffset, ctrl.cameraProbeRadius,
                dir, out hit, full - startOffset, ctrl.cameraObstacles, QueryTriggerInteraction.Ignore);

            if (blocked)
            {
                blockedCount++;
                float d = Mathf.Clamp(startOffset + hit.distance - ctrl.cameraProbeRadius,
                                      ctrl.cameraMinDistance, full);
                sb.AppendLine("yaw " + deg + ": BLOQUEADO por '" + hit.collider.name
                    + "' -> recua de " + full.ToString("F2") + "m para " + d.ToString("F2") + "m"
                    + (hit.collider.transform.IsChildOf(p) ? "  <-- BUG: proprio rover" : ""));
            }
        }
        sb.AppendLine("orientacoes testadas=12, bloqueadas=" + blockedCount);

        p.rotation = savedRot;
        p.position = savedPos;
        return sb.ToString();
    }
}
