using System.Linq;
using System.Text;
using UnityEngine;

public static class StandTreeTest
{
    // Confirma que a capsula do rover e barrada por arvore EM PE.
    public static string Run()
    {
        StringBuilder sb = new StringBuilder();
        CharacterController cc = Object.FindFirstObjectByType<CharacterController>();

        MeshFilter tree = Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)
            .FirstOrDefault(m => m.sharedMesh != null
                && m.sharedMesh.name.StartsWith("tree")
                && !m.name.Contains("DERRUBADA")
                && m.GetComponent<CapsuleCollider>() != null
                && m.gameObject.activeInHierarchy);
        if (tree == null) return "nenhuma arvore em pe com capsula";

        CapsuleCollider cap = tree.GetComponent<CapsuleCollider>();
        sb.AppendLine("arvore '" + tree.name + "' em " + tree.transform.position.ToString("F1"));
        sb.AppendLine("   collider=" + cap.GetType().Name + " isTrigger=" + cap.isTrigger
            + " raioMundo=" + (cap.radius * tree.transform.lossyScale.x).ToString("F2") + "m"
            + " alturaMundo=" + (cap.height * tree.transform.lossyScale.y).ToString("F2") + "m");
        sb.AppendLine("   Rigidbody=" + (tree.GetComponent<Rigidbody>() == null ? "nenhum (nao cai)" : "PRESENTE"));

        float r = cc.radius;
        float h = Mathf.Max(cc.height, r * 2f);
        Vector3 target = cap.bounds.center; target.y = 0f;
        Vector3 start = target + Vector3.right * 7f;
        Vector3 dir = (target - start).normalized;
        Vector3 p1 = start + Vector3.up * (r + 0.05f);
        Vector3 p2 = start + Vector3.up * (h - r + 0.05f);

        RaycastHit hit;
        bool blocked = Physics.CapsuleCast(p1, p2, r, dir, out hit, 7f, ~0,
            QueryTriggerInteraction.Ignore);
        sb.AppendLine("CapsuleCast do rover: " + (blocked
            ? "BLOQUEADO por '" + hit.collider.gameObject.name + "' a " + hit.distance.ToString("F2") + "m"
            : "NAO BLOQUEADO"));

        int comFall = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .Count(m => m != null && m.GetType().Name == "CartTreeImpact");
        sb.AppendLine("componentes CartTreeImpact na cena: " + comFall + " (0 = arvore nao quebra mais)");
        return sb.ToString();
    }
}
