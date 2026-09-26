using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;

public static class FixPass2
{
    const string RendererPath = "Assets/Settings/PC_Renderer.asset";

    // Bit 0 = mundo (recebe decal). Bit 1 = o rover (nao recebe decal).
    const uint LayerWorld = 1u << 0;
    const uint LayerVehicle = 1u << 1;

    // ---------------------------------------------- 1. nitidez de volta

    // O SMAA e um blur de borda em pos-processo. Nesta arte de cor chapada e
    // aresta dura ele le como "menos pixels". Deferred nao suporta MSAA, entao
    // a opcao sem custo de nitidez e desligar o AA.
    public static string DisableAA()
    {
        StringBuilder sb = new StringBuilder();
        foreach (Camera c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            UniversalAdditionalCameraData d = c.GetComponent<UniversalAdditionalCameraData>();
            if (d == null) continue;
            d.antialiasing = AntialiasingMode.None;
            EditorUtility.SetDirty(c.gameObject);
            sb.AppendLine("AA desligado em " + c.name + " (postFX segue ligado)");
        }
        MarkDirty();
        return sb.ToString();
    }

    // ------------------------------- 2. decal vazando no carrinho

    public static string FixDecalLeak()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return "ERRO: saia do Play mode primeiro";

        StringBuilder sb = new StringBuilder();

        // (a) liga rendering layers no Decal feature
        UniversalRendererData data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
        foreach (ScriptableRendererFeature f in data.rendererFeatures)
        {
            if (!(f is DecalRendererFeature)) continue;
            SerializedObject so = new SerializedObject(f);
            SerializedProperty p = so.FindProperty("m_Settings.decalLayers");
            if (p != null) { p.boolValue = true; so.ApplyModifiedPropertiesWithoutUndo(); }
            EditorUtility.SetDirty(f);
            sb.AppendLine("decalLayers ligado no DecalRendererFeature");
        }
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();

        // (b) rover vai para o layer de renderizacao 2, que nenhum decal afeta
        CharacterController cc = UnityEngine.Object.FindFirstObjectByType<CharacterController>();
        int moved = 0;
        if (cc != null)
        {
            foreach (Renderer r in cc.GetComponentsInChildren<Renderer>(true))
            {
                r.renderingLayerMask = LayerVehicle;
                EditorUtility.SetDirty(r);
                moved++;
            }
            sb.AppendLine("renderers do rover movidos para rendering layer 2: " + moved);
        }
        else sb.AppendLine("AVISO: rover nao encontrado");

        // (c) decals so afetam o mundo, e a caixa passa a rasar o asfalto
        int fixedDecals = 0;
        foreach (DecalProjector dp in UnityEngine.Object.FindObjectsByType<DecalProjector>(FindObjectsSortMode.None))
        {
            dp.renderingLayerMask = LayerWorld;
            // Os 23 pontos de rachadura estao TODOS em y=0.000 (medido por
            // raycast), entao a caixa pode ser bem rasa: -0.11..0.11. Isso
            // exclui o topo da calcada (~0.15) e a base das rodas (0.15),
            // que antes eram pintados pelo decal.
            Vector3 s = dp.size;
            dp.size = new Vector3(s.x, s.y, 0.22f);
            Vector3 p = dp.transform.position;
            dp.transform.position = new Vector3(p.x, 0f, p.z);
            EditorUtility.SetDirty(dp);
            fixedDecals++;
        }
        sb.AppendLine("decals ajustados: " + fixedDecals);
        MarkDirty();
        return sb.ToString();
    }

    // ------------------------------------------- 3. fisica nos defeitos

    public static string AddDefectColliders()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return "ERRO: saia do Play mode primeiro";

        GameObject root = GameObject.Find("City 02/World/Infrastructure Defects");
        if (root == null) return "ERRO: root de defeitos nao encontrado";

        StringBuilder sb = new StringBuilder();
        int solid = 0, trigger = 0, skipped = 0;

        foreach (DefectInfo info in root.GetComponentsInChildren<DefectInfo>(true))
        {
            string t = info.defectType == null ? "" : info.defectType;

            // Rachadura e pintura plana no chao: nao deve ter fisica.
            if (t.IndexOf("achadura") >= 0) { skipped++; continue; }

            bool asTrigger = t.IndexOf("Buraco") >= 0 || t.IndexOf("Bueiro") >= 0;

            foreach (MeshFilter mf in info.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                if (mf.GetComponent<Collider>() != null) continue;

                BoxCollider bc = mf.gameObject.AddComponent<BoxCollider>();
                // bounds do mesh sao locais, entao rotacao e escala do objeto
                // ja sao respeitadas pelo transform
                bc.center = mf.sharedMesh.bounds.center;
                bc.size = mf.sharedMesh.bounds.size;
                bc.isTrigger = asTrigger;
                EditorUtility.SetDirty(mf.gameObject);
                if (asTrigger) trigger++; else solid++;
            }
        }

        // arvores derrubadas e postes sao objetos irmaos, nao filhos do DefectInfo
        int trees = 0;
        foreach (Transform tr in root.GetComponentsInChildren<Transform>(true))
        {
            if (!tr.name.Contains("DERRUBADA")) continue;
            if (tr.GetComponent<Collider>() != null) continue;
            MeshFilter mf = tr.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;

            BoxCollider bc = tr.gameObject.AddComponent<BoxCollider>();
            bc.center = mf.sharedMesh.bounds.center;
            bc.size = mf.sharedMesh.bounds.size;
            bc.isTrigger = false;   // arvore caida bloqueia o rover
            EditorUtility.SetDirty(tr.gameObject);
            trees++;
        }

        sb.AppendLine("colliders solidos: " + solid);
        sb.AppendLine("colliders trigger (buraco/bueiro): " + trigger);
        sb.AppendLine("arvores derrubadas com collider: " + trees);
        sb.AppendLine("rachaduras ignoradas de proposito: " + skipped);
        MarkDirty();
        return sb.ToString();
    }

    static void MarkDirty()
    {
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
}
