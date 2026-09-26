using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;

// Posiciona a Scene View para inspecionar defeitos específicos.
public static class Shots
{
    // Enquadra a n-ésima rachadura, olhando de cima em ângulo, a uma distância
    // parecida com a que o rover a veria em jogo.
    public static string FrameCrack(string indexStr)
    {
        int index = 0;
        int.TryParse(indexStr, out index);

        GameObject root = GameObject.Find("City 02/World/Infrastructure Defects");
        if (root == null) return "ERRO: root de defeitos nao encontrado";

        DefectInfo[] cracks = root.GetComponentsInChildren<DefectInfo>(true)
            .Where(d => d.defectType != null && d.defectType.IndexOf("achadura") >= 0)
            .ToArray();
        if (cracks.Length == 0) return "ERRO: nenhuma rachadura";

        DefectInfo c = cracks[Mathf.Clamp(index, 0, cracks.Length - 1)];
        Vector3 p = c.transform.position;

        SceneView sv = SceneView.lastActiveSceneView;
        if (sv == null) return "ERRO: sem Scene View ativa";

        sv.orthographic = false;
        sv.pivot = new Vector3(p.x, 0f, p.z);
        sv.rotation = Quaternion.Euler(72f, 20f, 0f);
        sv.size = 4.0f;
        sv.Repaint();

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("rachadura " + index + "/" + cracks.Length + " = " + c.name);
        sb.AppendLine("gravidade=" + c.severity + " codigo=" + c.code);
        sb.AppendLine("pos=" + p.ToString("F2"));
        UnityEngine.Rendering.Universal.DecalProjector dp =
            c.GetComponent<UnityEngine.Rendering.Universal.DecalProjector>();
        sb.AppendLine("decal=" + (dp == null ? "AUSENTE" : dp.material.name + " size=" + dp.size.ToString("F2")));
        sb.AppendLine("filhos restantes=" + c.transform.childCount);
        return sb.ToString();
    }
}
