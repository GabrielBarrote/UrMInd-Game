using System.Text;
using UnityEngine;
using UnityEditor;

public static class VerifyPass3
{
    public static string Run()
    {
        StringBuilder sb = new StringBuilder();
        CharacterController cc = Object.FindFirstObjectByType<CharacterController>();
        Transform p = cc.transform;

        // --- 1. eixo de giro das rodas ---
        sb.AppendLine("=== EIXO DAS RODAS ===");
        sb.AppendLine("player.right = " + p.right.ToString("F3"));
        foreach (string n in new string[] { "WheelFL", "WheelBR" })
        {
            Transform w = null;
            foreach (Transform t in p.GetComponentsInChildren<Transform>(true)) if (t.name == n) w = t;
            if (w == null) { sb.AppendLine(n + " NAO ENCONTRADA"); continue; }
            float dotRight = Vector3.Dot(w.up, p.right);
            sb.AppendLine(n + " pai=" + w.parent.name
                + " | eixo do cilindro (w.up) = " + w.up.ToString("F3")
                + " | dot com player.right = " + dotRight.ToString("F3")
                + (Mathf.Abs(dotRight) > 0.97f ? "  -> EIXO CORRETO (horizontal, transversal)" : "  -> EIXO ERRADO")
                + " | filhos=" + w.childCount);
            for (int i = 0; i < w.childCount; i++) sb.AppendLine("      filho: " + w.GetChild(i).name);
        }

        // --- 2. cast da camera: existe geometria que bloqueie? ---
        sb.AppendLine("=== CAST DA CAMERA ===");
        AnalysisCartController ctrl = p.GetComponent<AnalysisCartController>();
        Vector3 pivot = p.position + Vector3.up * ctrl.lookHeight;
        Vector3 desired = p.TransformPoint(ctrl.cameraOffset);
        Vector3 toCam = desired - pivot;
        float full = toCam.magnitude;
        Vector3 dir = toCam / full;
        float startOffset = cc.radius + ctrl.cameraProbeRadius + 0.05f;
        RaycastHit hit;
        bool blocked = Physics.SphereCast(pivot + dir * startOffset, ctrl.cameraProbeRadius, dir,
                                          out hit, full - startOffset, ctrl.cameraObstacles,
                                          QueryTriggerInteraction.Ignore);
        sb.AppendLine("distancia cheia=" + full.ToString("F2") + " startOffset=" + startOffset.ToString("F2"));
        sb.AppendLine("bloqueado agora? " + blocked
            + (blocked ? " por '" + hit.collider.name + "' -> camera iria para "
                + (startOffset + hit.distance - ctrl.cameraProbeRadius).ToString("F2") + "m" : " (campo livre)"));
        sb.AppendLine("o cast pega o proprio rover? " + (blocked && hit.collider.transform.IsChildOf(p) ? "SIM (BUG)" : "nao"));

        // --- 3. deteccao de arvore ---
        sb.AppendLine("=== DETECCAO DE ARVORE ===");
        int standing = 0, detectable = 0;
        foreach (MeshFilter mf in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
        {
            if (mf.sharedMesh == null || !mf.sharedMesh.name.StartsWith("tree")) continue;
            if (mf.name.Contains("DERRUBADA")) continue;
            standing++;
            Collider c = mf.GetComponent<Collider>();
            if (c != null && !c.isTrigger && mf.GetComponent<Rigidbody>() == null) detectable++;
        }
        sb.AppendLine("arvores em pe=" + standing + " que o CartTreeImpact reconheceria=" + detectable);
        sb.AppendLine("CartWheels presente=" + (p.GetComponent<CartWheels>() != null)
            + " CartTreeImpact presente=" + (p.GetComponent<CartTreeImpact>() != null));

        // --- 4. colliders extras no rover (nao deve haver) ---
        int extra = 0;
        foreach (Collider c in p.GetComponentsInChildren<Collider>(true))
            if (!(c is CharacterController)) extra++;
        sb.AppendLine("colliders extras no rover (esperado 0): " + extra);
        return sb.ToString();
    }
}
