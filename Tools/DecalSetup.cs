using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;

// Liga o Decal Renderer Feature, cria os materiais de decal e converte as
// rachaduras de cubos em relevo para DecalProjectors afundados.
public static class DecalSetup
{
    const string RendererPath = "Assets/Settings/PC_Renderer.asset";
    const string TexDir = "Assets/Detail/Textures";
    const string MatDir = "Assets/Detail/Materials";

    // ------------------------------------------------- 1. renderer feature

    public static string AddDecalFeature()
    {
        StringBuilder log = new StringBuilder();
        UniversalRendererData data =
            AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
        if (data == null) return "ERRO: nao carregou " + RendererPath;

        foreach (ScriptableRendererFeature f in data.rendererFeatures)
        {
            if (f is DecalRendererFeature)
                return "Decal feature JA existe: " + f.name;
        }

        DecalRendererFeature feat = ScriptableObject.CreateInstance<DecalRendererFeature>();
        feat.name = "Decal";
        AssetDatabase.AddObjectToAsset(feat, data);

        SerializedObject so = new SerializedObject(data);
        SerializedProperty list = so.FindProperty("m_RendererFeatures");
        list.arraySize++;
        list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = feat;
        so.ApplyModifiedPropertiesWithoutUndo();
        log.AppendLine("feature adicionada, arraySize=" + list.arraySize);

        // m_RendererFeatureMap e recalculado por um metodo interno do URP
        MethodInfo validate = typeof(ScriptableRendererData).GetMethod(
            "ValidateRendererFeatures", BindingFlags.Instance | BindingFlags.NonPublic);
        if (validate != null)
        {
            validate.Invoke(data, null);
            log.AppendLine("ValidateRendererFeatures() invocado");
        }
        else
        {
            log.AppendLine("AVISO: ValidateRendererFeatures nao encontrado por reflexao");
        }

        EditorUtility.SetDirty(feat);
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        log.AppendLine("features agora: " +
            string.Join(", ", data.rendererFeatures.Select(x => x == null ? "null" : x.GetType().Name)));
        return log.ToString();
    }

    // ------------------------------------------------------ 2. materiais

    public static string CreateCrackMaterials()
    {
        Directory.CreateDirectory(MatDir);
        Shader sh = Shader.Find("Shader Graphs/Decal");
        if (sh == null) return "ERRO: shader 'Shader Graphs/Decal' nao encontrado";

        StringBuilder log = new StringBuilder();
        for (int v = 0; v < 3; v++)
        {
            Texture2D baseMap = AssetDatabase.LoadAssetAtPath<Texture2D>(
                TexDir + "/crack_" + v + "_albedo.png");
            Texture2D normalMap = AssetDatabase.LoadAssetAtPath<Texture2D>(
                TexDir + "/crack_" + v + "_normal.png");
            if (baseMap == null || normalMap == null)
            {
                log.AppendLine("variante " + v + ": textura faltando, pulada");
                continue;
            }

            string path = MatDir + "/Decal_Crack_" + v + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(sh);
                AssetDatabase.CreateAsset(m, path);
            }
            m.shader = sh;
            m.SetTexture("Base_Map", baseMap);
            m.SetTexture("Normal_Map", normalMap);
            // normal forte: e o relevo que faz a fenda ler como afundada
            m.SetFloat("Normal_Blend", 1f);
            EditorUtility.SetDirty(m);
            log.AppendLine("material " + path);
        }
        AssetDatabase.SaveAssets();
        return log.ToString();
    }

    // -------------------------------------------- 3. converter rachaduras

    public static string ConvertCracksToDecals()
    {
        // em Play mode as edicoes vao para a copia temporaria e se perdem
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return "ERRO: Editor em Play mode. Saia do Play antes de converter.";

        GameObject root = GameObject.Find("City 02/World/Infrastructure Defects");
        if (root == null) return "ERRO: nao achei 'City 02/World/Infrastructure Defects'";

        Material[] mats = new Material[3];
        for (int v = 0; v < 3; v++)
            mats[v] = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/Decal_Crack_" + v + ".mat");
        if (mats[0] == null) return "ERRO: materiais de decal nao criados ainda";

        StringBuilder log = new StringBuilder();
        int converted = 0;
        int cubesRemoved = 0;

        DefectInfo[] all = root.GetComponentsInChildren<DefectInfo>(true);
        foreach (DefectInfo info in all)
        {
            if (info.defectType == null || info.defectType.IndexOf("achadura") < 0) continue;

            GameObject go = info.gameObject;

            // gravidade -> variante e tamanho
            int variant = 1;
            float width = 3.5f;
            string sev = info.severity == null ? "" : info.severity.ToUpperInvariant();
            if (sev.Contains("BAIXA")) { variant = 0; width = 2.6f; }
            else if (sev.Contains("ALTA")) { variant = 2; width = 4.8f; }

            // remove os cubos em relevo que nunca eram visiveis
            for (int i = go.transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = go.transform.GetChild(i).gameObject;
                UnityEngine.Object.DestroyImmediate(child);
                cubesRemoved++;
            }

            DecalProjector dp = go.GetComponent<DecalProjector>();
            if (dp == null) dp = go.AddComponent<DecalProjector>();

            dp.material = mats[variant];
            // projeta para baixo
            float yaw = (Mathf.Abs(go.transform.position.x * 37.1f
                                 + go.transform.position.z * 91.7f) % 360f);
            go.transform.rotation = Quaternion.Euler(90f, yaw, 0f);
            // caixa de projecao rasa: pega so o asfalto, nao vaza em calcadas altas
            dp.pivot = Vector3.zero;
            // a caixa precisa ENVOLVER o asfalto (y=0), nao tangenciá-lo
            dp.size = new Vector3(width, width, 1.0f);
            dp.fadeFactor = 1f;
            dp.renderingLayerMask = 1;

            Vector3 p = go.transform.position;
            go.transform.position = new Vector3(p.x, 0.35f, p.z);

            converted++;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        log.AppendLine("rachaduras convertidas: " + converted);
        log.AppendLine("cubos em relevo removidos: " + cubesRemoved);
        return log.ToString();
    }
}
