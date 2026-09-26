using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;

public static class Probe
{
    // Raycast de cima para baixo sobre cada rachadura, para descobrir a altura
    // real da superficie que o decal precisa atingir.
    public static string RoadHeights()
    {
        GameObject root = GameObject.Find("City 02/World/Infrastructure Defects");
        if (root == null) return "ERRO: root nao encontrado";

        DefectInfo[] cracks = root.GetComponentsInChildren<DefectInfo>(true)
            .Where(d => d.defectType != null && d.defectType.IndexOf("achadura") >= 0)
            .ToArray();

        StringBuilder sb = new StringBuilder();
        int hits = 0;
        for (int i = 0; i < cracks.Length; i++)
        {
            Vector3 p = cracks[i].transform.position;
            Vector3 from = new Vector3(p.x, 40f, p.z);
            RaycastHit hit;
            if (Physics.Raycast(from, Vector3.down, out hit, 200f))
            {
                hits++;
                if (i < 8)
                    sb.AppendLine(cracks[i].name + " y=" + hit.point.y.ToString("F3")
                                  + " sobre '" + hit.collider.gameObject.name + "'");
            }
            else if (i < 8) sb.AppendLine(cracks[i].name + " SEM HIT");
        }
        sb.AppendLine("total com hit: " + hits + "/" + cracks.Length);
        return sb.ToString();
    }
}
