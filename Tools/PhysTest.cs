using System.Linq;
using System.Text;
using UnityEngine;

public static class PhysTest
{
    // Prova que a capsula do rover seria de fato barrada: faz um CapsuleCast
    // com as dimensoes reais do CharacterController contra uma arvore caida.
    public static string Run()
    {
        StringBuilder sb = new StringBuilder();
        CharacterController cc = UnityEngine.Object.FindFirstObjectByType<CharacterController>();
        if (cc == null) return "rover nao encontrado";

        GameObject root = GameObject.Find("City 02/World/Infrastructure Defects");
        Transform tree = root.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t.name.Contains("DERRUBADA") && t.GetComponent<Collider>() != null);
        if (tree == null) return "nenhuma arvore com collider";

        Collider col = tree.GetComponent<Collider>();
        sb.AppendLine("arvore '" + tree.name + "' collider=" + col.GetType().Name
            + " isTrigger=" + col.isTrigger + " layer=" + tree.gameObject.layer
            + " boundsMundo=" + col.bounds.size.ToString("F2"));

        // capsula do CC nas dimensoes reais
        float r = cc.radius;
        float h = Mathf.Max(cc.height, r * 2f);
        Vector3 target = col.bounds.center;
        target.y = 0f;

        // aproxima de 6m de distancia na direcao do centro da arvore
        Vector3 dir = (target - (target + Vector3.right * 6f)).normalized;
        Vector3 start = target + Vector3.right * 6f;
        Vector3 p1 = start + Vector3.up * (r + 0.05f);
        Vector3 p2 = start + Vector3.up * (h - r + 0.05f);

        RaycastHit hit;
        bool blocked = UnityEngine.Physics.CapsuleCast(p1, p2, r, dir, out hit, 6f);
        sb.AppendLine("CapsuleCast rumo a arvore: " + (blocked
            ? "BLOQUEADO por '" + hit.collider.gameObject.name + "' a " + hit.distance.ToString("F2") + "m"
            : "NAO BLOQUEADO (rover passaria atravessado)"));

        // e os buracos devem ser trigger: detectaveis mas nao bloqueiam
        Transform hole = root.GetComponentsInChildren<DefectInfo>(true)
            .Where(d => d.defectType != null && d.defectType.Contains("Buraco"))
            .Select(d => d.transform).FirstOrDefault();
        if (hole != null)
        {
            Collider[] overl = UnityEngine.Physics.OverlapCapsule(
                hole.position + Vector3.up * (r + 0.05f),
                hole.position + Vector3.up * (h - r + 0.05f), r);
            int trig = overl.Count(c => c.isTrigger);
            int solidC = overl.Count(c => !c.isTrigger);
            sb.AppendLine("sobre um buraco: " + overl.Length + " colliders sobrepostos ("
                + trig + " trigger, " + solidC + " solido) -> "
                + (trig > 0 ? "detectavel por OnTriggerEnter" : "NAO detectavel"));
        }
        return sb.ToString();
    }
}
