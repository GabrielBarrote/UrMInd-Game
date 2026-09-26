using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Cria o Volume global de gameplay. Os profiles que vieram no asset pack são
// por-câmera, tem DepthOfField e nenhum deles tem Tonemapping — inúteis aqui.
public static class VolumeSetup
{
    const string ProfilePath = "Assets/Detail/Gameplay Volume.asset";

    public static string Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return "ERRO: saia do Play mode primeiro";

        Directory.CreateDirectory("Assets/Detail");
        StringBuilder log = new StringBuilder();

        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);
            log.AppendLine("profile criado: " + ProfilePath);
        }

        // --- Tonemapping: sem isto o HDR estoura os realces (calcada branca) ---
        Tonemapping tm = GetOrAdd<Tonemapping>(profile);
        tm.mode.overrideState = true;
        tm.mode.value = TonemappingMode.ACES;

        // --- Color adjustments: contraste e um leve ganho de saturacao ---
        ColorAdjustments ca = GetOrAdd<ColorAdjustments>(profile);
        ca.postExposure.overrideState = true;
        ca.postExposure.value = 0.45f;
        ca.contrast.overrideState = true;
        ca.contrast.value = 11f;
        ca.saturation.overrideState = true;
        ca.saturation.value = 8f;

        // --- Separa sombra/meio-tom/realce: e o que da leitura de volume ---
        ShadowsMidtonesHighlights smh = GetOrAdd<ShadowsMidtonesHighlights>(profile);
        smh.shadows.overrideState = true;
        smh.shadows.value = new Vector4(0.97f, 0.985f, 1.035f, 0f); // sombra levemente fria
        smh.highlights.overrideState = true;
        smh.highlights.value = new Vector4(1.03f, 1.005f, 0.97f, 0f); // realce levemente quente

        // --- White balance: o ACES puxa frio nesta cena, compensa ---
        WhiteBalance wb = GetOrAdd<WhiteBalance>(profile);
        wb.temperature.overrideState = true;
        wb.temperature.value = 12f;
        wb.tint.overrideState = true;
        wb.tint.value = -4f;

        // --- Bloom discreto; sem isto o sol nao tem presenca ---
        Bloom bloom = GetOrAdd<Bloom>(profile);
        bloom.threshold.overrideState = true;
        bloom.threshold.value = 1.05f;
        bloom.intensity.overrideState = true;
        bloom.intensity.value = 0.35f;
        bloom.scatter.overrideState = true;
        bloom.scatter.value = 0.6f;

        // --- Vinheta leve para focar o centro da tela ---
        Vignette vig = GetOrAdd<Vignette>(profile);
        vig.intensity.overrideState = true;
        vig.intensity.value = 0.22f;
        vig.smoothness.overrideState = true;
        vig.smoothness.value = 0.45f;

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();

        // --- GameObject do Volume na cena ---
        GameObject go = GameObject.Find("--- Gameplay Volume ---");
        if (go == null)
        {
            go = new GameObject("--- Gameplay Volume ---");
            log.AppendLine("GameObject do Volume criado");
        }
        Volume vol = go.GetComponent<Volume>();
        if (vol == null) vol = go.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = 100f;   // acima de qualquer volume do asset pack
        vol.weight = 1f;
        vol.sharedProfile = profile;

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        log.AppendLine("componentes no profile: " + profile.components.Count);
        foreach (VolumeComponent c in profile.components)
            log.AppendLine("   - " + c.GetType().Name);
        return log.ToString();
    }

    static T GetOrAdd<T>(VolumeProfile p) where T : VolumeComponent
    {
        T c;
        if (p.TryGet<T>(out c)) return c;
        return p.Add<T>(true);
    }
}
