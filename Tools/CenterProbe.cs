using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public static class CenterProbe
{
    // O que existe perto do centro do mapa, por grupo, num raio dado.
    public static string Run(string radiusStr)
    {
        float R = float.Parse(radiusStr, System.Globalization.CultureInfo.InvariantCulture);
        StringBuilder sb = new StringBuilder();
        GameObject world = GameObject.Find("City 02/World");
        Vector3 c = Vector3.zero;

        foreach (Transform grp in world.transform)
        {
            List<Transform> near = new List<Transform>();
            foreach (MeshRenderer r in grp.GetComponentsInChildren<MeshRenderer>(true))
            {
                Vector3 p = r.transform.position;
                if (Mathf.Abs(p.x - c.x) <= R && Mathf.Abs(p.z - c.z) <= R) near.Add(r.transform);
            }
            if (near.Count == 0) continue;
            sb.AppendLine(grp.name + ": " + near.Count + " objetos dentro de +-" + R + "m do centro");
            foreach (Transform t in near.Take(4))
                sb.AppendLine("     " + t.name + " em " + t.position.ToString("F1"));
        }

        // altura do chao no centro
        RaycastHit hit;
        if (Physics.Raycast(new Vector3(0f, 60f, 0f), Vector3.down, out hit, 200f))
            sb.AppendLine("chao no centro: y=" + hit.point.y.ToString("F2") + " sobre '" + hit.collider.name + "'");
        return sb.ToString();
    }
}
