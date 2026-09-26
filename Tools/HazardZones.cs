using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;

// Cria uma zona de gatilho dedicada em cada buraco/bueiro.
//
// Os triggers herdados das meshes funcionam, mas por margem de centimetros: a
// base da capsula do rover fica em Y=0.05 e o unico trigger que a alcanca vai
// ate 0.09. Qualquer oscilacao do veiculo faria o tranco falhar. Uma caixa
// dedicada, alta o bastante, torna a deteccao deterministica -- e por ser
// trigger nao bloqueia passagem nem aparece.
public static class HazardZones
{
    public static string Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return "ERRO: saia do Play mode primeiro";

        GameObject root = GameObject.Find("City 02/World/Infrastructure Defects");
        if (root == null) return "ERRO: root de defeitos nao encontrado";

        StringBuilder sb = new StringBuilder();
        int made = 0, reused = 0;

        foreach (DefectInfo info in root.GetComponentsInChildren<DefectInfo>(true))
        {
            string t = info.defectType == null ? "" : info.defectType;
            if (t.IndexOf("Buraco") < 0 && t.IndexOf("Bueiro") < 0) continue;

            Transform existing = null;
            foreach (Transform ch in info.transform)
                if (ch.name == "HazardZone") existing = ch;

            GameObject zone;
            if (existing != null) { zone = existing.gameObject; reused++; }
            else
            {
                zone = new GameObject("HazardZone");
                zone.transform.SetParent(info.transform, false);
                made++;
            }
            zone.transform.localPosition = Vector3.zero;
            zone.transform.localRotation = Quaternion.identity;
            zone.transform.localScale = Vector3.one;

            // footprint real do defeito, a partir dos renderers
            Renderer[] rs = info.GetComponentsInChildren<Renderer>(true);
            float halfX = 0.8f, halfZ = 0.8f;
            if (rs.Length > 0)
            {
                Bounds b = rs[0].bounds;
                for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
                halfX = Mathf.Max(0.6f, b.extents.x);
                halfZ = Mathf.Max(0.6f, b.extents.z);
            }

            BoxCollider bc = zone.GetComponent<BoxCollider>();
            if (bc == null) bc = zone.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            // 0.9m de altura a partir do chao: cobre a capsula com folga ampla
            bc.center = new Vector3(0f, 0.45f, 0f);
            bc.size = new Vector3(halfX * 2f, 0.90f, halfZ * 2f);

            EditorUtility.SetDirty(zone);
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        sb.AppendLine("zonas criadas=" + made + " reaproveitadas=" + reused);
        return sb.ToString();
    }
}
