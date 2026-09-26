using System.Text;
using UnityEngine;
using UnityEditor;

// Religa materiais nulos deixados pelo remap dos FBX.
// Os objetos de defeito apontavam para os materiais EMBUTIDOS nos modelos;
// ao redirecionar o importer esses sub-assets sumiram e a ref virou null.
// A origem correta e descoberta pelo caminho do mesh de cada renderer.
public static class FixNullMats
{
    const string MatDir = "Assets/Detail/Materials";

    static Material ForMeshPath(string p)
    {
        string name = null;
        if (p.Contains("City Kit (Roads)")) name = "Kenney_Roads";
        else if (p.Contains("City Kit (Commercial)")) name = "Kenney_Commercial";
        else if (p.Contains("City Kit (Industrial)")) name = "Kenney_Industrial";
        else if (p.Contains("City Kit (Suburban)")) name = "Kenney_Suburban";
        else if (p.Contains("City Kit (Cars)")) name = "Kenney_Cars";
        else if (p.Contains("Blocky Characters")) name = "Kenney_Characters";
        if (name == null) return null;
        return AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/" + name + ".mat");
    }

    public static string Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return "ERRO: saia do Play mode primeiro";

        StringBuilder sb = new StringBuilder();
        int fixedCount = 0, unresolved = 0;

        foreach (MeshRenderer r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            Material[] mats = r.sharedMaterials;
            bool anyNull = false;
            foreach (Material m in mats) if (m == null) { anyNull = true; break; }
            if (!anyNull) continue;

            MeshFilter mf = r.GetComponent<MeshFilter>();
            string meshPath = mf == null || mf.sharedMesh == null
                ? "" : AssetDatabase.GetAssetPath(mf.sharedMesh);
            Material repl = ForMeshPath(meshPath);

            if (repl == null)
            {
                unresolved++;
                if (unresolved <= 5)
                    sb.AppendLine("NAO RESOLVIDO: " + r.name + " mesh='" + meshPath + "'");
                continue;
            }

            for (int i = 0; i < mats.Length; i++)
                if (mats[i] == null) mats[i] = repl;
            r.sharedMaterials = mats;
            EditorUtility.SetDirty(r);
            fixedCount++;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        sb.AppendLine("renderers corrigidos: " + fixedCount + ", nao resolvidos: " + unresolved);
        return sb.ToString();
    }
}
