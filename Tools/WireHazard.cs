using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;

public static class WireHazard
{
    public static string Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return "ERRO: saia do Play mode primeiro";

        CharacterController cc = Object.FindFirstObjectByType<CharacterController>();
        if (cc == null) return "rover nao encontrado";
        StringBuilder sb = new StringBuilder();

        CartHazardResponse hz = cc.GetComponent<CartHazardResponse>();
        if (hz == null) hz = cc.gameObject.AddComponent<CartHazardResponse>();
        EditorUtility.SetDirty(hz);
        sb.AppendLine("CartHazardResponse adicionado. minSpeed=" + hz.minSpeed
            + " reArm=" + hz.reArmSeconds + "s");

        // confere que os triggers estao alcancaveis a partir do DefectInfo
        GameObject root = GameObject.Find("City 02/World/Infrastructure Defects");
        int holes = 0, manholes = 0, reachable = 0;
        foreach (DefectInfo info in root.GetComponentsInChildren<DefectInfo>(true))
        {
            string t = info.defectType == null ? "" : info.defectType;
            bool isHole = t.IndexOf("Buraco") >= 0;
            bool isMan = t.IndexOf("Bueiro") >= 0;
            if (!isHole && !isMan) continue;
            if (isHole) holes++; else manholes++;

            // simula a busca que o script fara: trigger -> GetComponentInParent
            Collider[] cols = info.GetComponentsInChildren<Collider>(true);
            Collider trig = cols.FirstOrDefault(c => c.isTrigger);
            if (trig != null && trig.GetComponentInParent<DefectInfo>() == info) reachable++;
        }
        sb.AppendLine("buracos=" + holes + " bueiros=" + manholes
            + " | com trigger que resolve para o DefectInfo correto=" + reachable);

        // a capsula do rover alcanca o trigger na altura em que ele esta?
        DefectInfo sample = root.GetComponentsInChildren<DefectInfo>(true)
            .FirstOrDefault(d => d.defectType != null && d.defectType.Contains("Buraco"));
        if (sample != null)
        {
            Collider c0 = sample.GetComponentsInChildren<Collider>(true).FirstOrDefault(c => c.isTrigger);
            if (c0 != null)
            {
                Bounds b = c0.bounds;
                float capBottom = cc.transform.position.y + cc.center.y - cc.height * 0.5f - cc.radius;
                sb.AppendLine("trigger exemplo Y=" + b.min.y.ToString("F3") + ".." + b.max.y.ToString("F3")
                    + " | base da capsula do rover Y=" + capBottom.ToString("F3")
                    + " -> " + (b.max.y >= capBottom - 0.05f ? "ALCANCAVEL" : "ALTO DEMAIS/BAIXO DEMAIS, nao dispara"));
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        return sb.ToString();
    }
}
