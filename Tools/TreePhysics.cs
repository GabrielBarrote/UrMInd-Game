using System.Text;
using UnityEngine;
using UnityEditor;

// Colliders nas 1174 arvores em pe.
//
// Medido nos vertices das meshes Kenney: o tronco tem raio local 0.0369 e a
// copa chega a 0.1215. Um BoxCollider do bounds inteiro criaria uma parede
// invisivel de ~2m incluindo a copa, entao usamos CapsuleCollider no tronco.
//
// Nenhum script e adicionado por arvore: 1174 MonoBehaviours seria desperdicio.
// A queda por impacto e resolvida do lado do rover, em CartTreeImpact.
public static class TreePhysics
{
    const float TrunkRadiusLocal = 0.0369f;

    public static string AddStandingTreeColliders()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return "ERRO: saia do Play mode primeiro";

        StringBuilder sb = new StringBuilder();
        int added = 0, already = 0;

        foreach (MeshFilter mf in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
        {
            if (mf.sharedMesh == null) continue;
            string mn = mf.sharedMesh.name;
            if (!mn.StartsWith("tree")) continue;
            if (mf.name.Contains("DERRUBADA")) continue;    // caidas ja tem box

            if (mf.GetComponent<Collider>() != null) { already++; continue; }

            CapsuleCollider cap = mf.gameObject.AddComponent<CapsuleCollider>();
            cap.direction = 1;                              // eixo Y
            cap.radius = TrunkRadiusLocal;

            // cobre a metade de baixo do tronco: o rover tem 1.15m de altura,
            // nao precisa de collider ate a copa
            float top = mf.sharedMesh.bounds.max.y;
            float h = top * 0.5f;
            cap.height = h;
            cap.center = new Vector3(0f, h * 0.5f, 0f);
            cap.isTrigger = false;

            EditorUtility.SetDirty(mf.gameObject);
            added++;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        sb.AppendLine("arvores em pe com capsule adicionada: " + added);
        sb.AppendLine("ja tinham collider: " + already);
        return sb.ToString();
    }
}
