using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;

public static class TriggerReach
{
    public static string Run()
    {
        StringBuilder sb = new StringBuilder();
        CharacterController cc = Object.FindFirstObjectByType<CharacterController>();
        Transform p = cc.transform;

        // Geometria REAL da capsula do CharacterController do Unity:
        // height ja inclui as duas semiesferas e e limitada a >= 2*radius.
        float h = Mathf.Max(cc.height, cc.radius * 2f);
        float bottomLocal = cc.center.y - h * 0.5f;
        sb.AppendLine("CC radius=" + cc.radius + " height=" + cc.height + " (efetiva " + h + ")");
        sb.AppendLine("base da capsula, local Y=" + bottomLocal.ToString("F3"));

        GameObject root = GameObject.Find("City 02/World/Infrastructure Defects");
        DefectInfo hole = root.GetComponentsInChildren<DefectInfo>(true)
            .FirstOrDefault(d => d.defectType != null && d.defectType.Contains("Buraco"));
        Vector3 saved = p.position;

        // coloca o rover exatamente sobre o buraco e testa a sobreposicao real
        p.position = new Vector3(hole.transform.position.x, saved.y, hole.transform.position.z);

        float bottomWorld = p.position.y + bottomLocal;
        sb.AppendLine("rover em cima do buraco: base da capsula, mundo Y=" + bottomWorld.ToString("F3"));

        foreach (Collider c in hole.GetComponentsInChildren<Collider>(true))
            sb.AppendLine("   trigger '" + c.name + "' isTrigger=" + c.isTrigger
                + " Y=" + c.bounds.min.y.ToString("F3") + ".." + c.bounds.max.y.ToString("F3"));

        // sobreposicao com a capsula verdadeira
        Vector3 c1 = p.position + cc.center + Vector3.up * (h * 0.5f - cc.radius);
        Vector3 c2 = p.position + cc.center - Vector3.up * (h * 0.5f - cc.radius);
        Collider[] hits = Physics.OverlapCapsule(c1, c2, cc.radius, ~0, QueryTriggerInteraction.Collide);
        int trg = hits.Count(x => x.isTrigger);
        sb.AppendLine("OverlapCapsule real -> " + hits.Length + " colliders, " + trg + " triggers");
        foreach (Collider c in hits.Where(x => x.isTrigger).Take(4))
            sb.AppendLine("      toca trigger: " + c.name);
        sb.AppendLine(trg > 0 ? "RESULTADO: OnTriggerEnter DISPARARIA"
                              : "RESULTADO: NAO DISPARA - trigger fora do alcance da capsula");

        p.position = saved;
        return sb.ToString();
    }
}
