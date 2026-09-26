using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Prepara o bake de iluminacao usando Adaptive Probe Volumes.
//
// Lightmaps classicos foram descartados de proposito: NENHUMA mesh Kenney tem
// UV2, entao seria preciso gerar UV de lightmap em 77 FBX e atlasar 4989
// objetos -- incluindo 1174 arvores de 44 vertices. APV nao usa UV2, ilumina
// objetos dinamicos (rover, veiculos, arvores que caem) e assa muito mais
// rapido nesta escala.
public static class BakeSetup
{
    // Subarvores que nunca se movem -> contribuem para a GI.
    static readonly string[] StaticGroups = new string[] {
        "Ground", "Road", "Buildings", "Tiles (Sidewalks)", "Light Poles", "Boxes", "Tanks", "Chimneys"
    };

    public static string Prepare()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return "ERRO: saia do Play mode primeiro";

        StringBuilder sb = new StringBuilder();
        GameObject world = GameObject.Find("City 02/World");
        if (world == null) return "ERRO: 'City 02/World' nao encontrado";

        // --- 1. flags de estatico apenas onde e verdade ---
        // Arvores ficam de FORA: elas recebem Rigidbody ao serem derrubadas, e
        // marcar como estatico um objeto que vai se mover quebra a GI.
        int marked = 0;
        foreach (string gname in StaticGroups)
        {
            Transform g = world.transform.Find(gname);
            if (g == null) { sb.AppendLine("grupo ausente: " + gname); continue; }
            foreach (MeshRenderer r in g.GetComponentsInChildren<MeshRenderer>(true))
            {
                GameObjectUtility.SetStaticEditorFlags(r.gameObject,
                    StaticEditorFlags.ContributeGI | StaticEditorFlags.OccluderStatic
                    | StaticEditorFlags.BatchingStatic);
                r.receiveGI = ReceiveGI.LightProbes;   // APV: nada de lightmap
                EditorUtility.SetDirty(r.gameObject);
                marked++;
            }
        }
        sb.AppendLine("renderers marcados como estaticos/ContributeGI: " + marked);

        // --- 2. sol em Mixed: direto continua realtime, indireto vai para o bake ---
        Light sun = Object.FindObjectsByType<Light>(FindObjectsSortMode.None)
            .FirstOrDefault(l => l.type == LightType.Directional);
        if (sun != null)
        {
            sun.lightmapBakeType = LightmapBakeType.Mixed;
            EditorUtility.SetDirty(sun);
            sb.AppendLine("sol '" + sun.name + "' -> Mixed (sombra direta preservada)");
        }

        // --- 3. URP passa a usar Probe Volumes ---
        UniversalRenderPipelineAsset rp = UniversalRenderPipeline.asset;
        SerializedObject rpso = new SerializedObject(rp);
        SerializedProperty lps = rpso.FindProperty("m_LightProbeSystem");
        if (lps != null) { lps.enumValueIndex = 1; rpso.ApplyModifiedPropertiesWithoutUndo(); }
        EditorUtility.SetDirty(rp);
        sb.AppendLine("URP LightProbeSystem -> ProbeVolumes");

        // --- 4. LightingSettings proprio ---
        string lsPath = "Assets/Detail/CityLighting.lighting";
        LightingSettings ls = AssetDatabase.LoadAssetAtPath<LightingSettings>(lsPath);
        if (ls == null)
        {
            ls = new LightingSettings();
            ls.name = "CityLighting";
            AssetDatabase.CreateAsset(ls, lsPath);
        }
        ls.bakedGI = true;
        ls.realtimeGI = false;
        ls.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
        ls.directSampleCount = 32;
        ls.indirectSampleCount = 256;
        ls.maxBounces = 2;
        ls.ao = false;                 // o SSAO em tela ja cobre oclusao de contato
        EditorUtility.SetDirty(ls);
        Lightmapping.lightingSettings = ls;
        sb.AppendLine("LightingSettings: " + lsPath + " (GPU, 2 bounces)");

        // --- 5. Probe Volume cobrindo a cidade ---
        Bounds b = new Bounds();
        bool first = true;
        foreach (string gname in StaticGroups)
        {
            Transform g = world.transform.Find(gname);
            if (g == null) continue;
            foreach (MeshRenderer r in g.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (first) { b = r.bounds; first = false; }
                else b.Encapsulate(r.bounds);
            }
        }
        if (first) return sb.ToString() + "ERRO: sem renderers para medir a cidade";

        GameObject pvGo = GameObject.Find("--- Probe Volume ---");
        if (pvGo == null) pvGo = new GameObject("--- Probe Volume ---");
        ProbeVolume pv = pvGo.GetComponent<ProbeVolume>();
        if (pv == null) pv = pvGo.AddComponent<ProbeVolume>();

        pv.mode = ProbeVolume.Mode.Local;
        pvGo.transform.position = new Vector3(b.center.x, b.min.y + 12f, b.center.z);
        pv.size = new Vector3(b.size.x + 10f, 30f, b.size.z + 10f);
        pv.overrideRendererFilters = false;
        EditorUtility.SetDirty(pvGo);

        sb.AppendLine("cidade: centro=" + b.center.ToString("F1") + " tamanho=" + b.size.ToString("F1"));
        sb.AppendLine("ProbeVolume: pos=" + pvGo.transform.position.ToString("F1")
                      + " size=" + pv.size.ToString("F1"));

        AssetDatabase.SaveAssets();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        return sb.ToString();
    }
}
