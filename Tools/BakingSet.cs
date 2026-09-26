using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// Cria e configura o ProbeVolumeBakingSet desta cena.
//
// O unico set que existia no projeto estava DENTRO do pacote URP (template de
// cena), nao servia. Normalmente o set e criado pela janela de Lighting; aqui
// replicamos o mesmo caminho por script. 'AddScene' e 'singleSceneMode' sao
// internos do pacote, dai a reflexao.
public static class BakingSet
{
    const string SetPath = "Assets/Detail/CityBakingSet.asset";

    public static string Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return "ERRO: saia do Play mode primeiro";

        StringBuilder sb = new StringBuilder();
        Scene scene = SceneManager.GetActiveScene();

        ProbeVolumeBakingSet set = AssetDatabase.LoadAssetAtPath<ProbeVolumeBakingSet>(SetPath);
        if (set == null)
        {
            set = ScriptableObject.CreateInstance<ProbeVolumeBakingSet>();
            set.name = "CityBakingSet";
            MethodInfo defaults = typeof(ProbeVolumeBakingSet).GetMethod("SetDefaults",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (defaults != null) defaults.Invoke(set, null);
            AssetDatabase.CreateAsset(set, SetPath);
            sb.AppendLine("baking set criado: " + SetPath);
        }
        else sb.AppendLine("baking set reaproveitado: " + SetPath);

        FieldInfo single = typeof(ProbeVolumeBakingSet).GetField("singleSceneMode",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (single != null) single.SetValue(set, true);

        // --- densidade: cidade aberta de 575x275m ---
        // minDistanceBetweenProbes e o espacamento no nivel mais subdividido.
        // 0.5 (padrao do template indoor) daria centenas de milhares de probes
        // nesta area; 3m e adequado para exterior.
        set.minDistanceBetweenProbes = 3f;
        set.simplificationLevels = 3;
        sb.AppendLine("minDistanceBetweenProbes=" + set.minDistanceBetweenProbes
            + " simplificationLevels=" + set.simplificationLevels
            + " -> maxSubdivision=" + set.maxSubdivision);

        // --- vincula a cena ao set ---
        // TryAddScene e publica e devolve false se a cena ja pertence a algum
        // set; AddScene tem um segundo parametro opcional, dai a reflexao
        // anterior falhar por contagem de argumentos.
        string guid = AssetDatabase.AssetPathToGUID(scene.path);
        bool added = set.TryAddScene(guid);
        sb.AppendLine(added ? "cena adicionada ao set (guid=" + guid + ")"
                            : "cena ja pertencia a um set");

        EditorUtility.SetDirty(set);
        AssetDatabase.SaveAssets();

        ProbeVolumeBakingSet check = SetForScene(guid);
        sb.AppendLine("verificacao: set da cena = " + (check == null ? "NULO" : check.name));
        sb.AppendLine("cenas no set = " + set.sceneGUIDs.Count);
        return sb.ToString();
    }

    // GetBakingSetForScene tambem e interno ao pacote.
    static ProbeVolumeBakingSet SetForScene(string guid)
    {
        MethodInfo mi = typeof(ProbeVolumeBakingSet).GetMethod("GetBakingSetForScene",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            null, new System.Type[] { typeof(string) }, null);
        if (mi == null) return null;
        return mi.Invoke(null, new object[] { guid }) as ProbeVolumeBakingSet;
    }

    // Redimensiona o volume para a area construida medida (nao para o Ground).
    public static string SizeVolume()
    {
        ProbeVolume pv = UnityEngine.Object.FindFirstObjectByType<ProbeVolume>();
        if (pv == null) return "ERRO: nenhum ProbeVolume";
        // medido: X[-283..263] Z[-87..163], predios ate 43.8m
        pv.transform.position = new Vector3(-10f, 20f, 38f);
        pv.size = new Vector3(575f, 52f, 275f);
        EditorUtility.SetDirty(pv);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            SceneManager.GetActiveScene());
        return "ProbeVolume pos=" + pv.transform.position.ToString("F0")
             + " size=" + pv.size.ToString("F0");
    }
}
