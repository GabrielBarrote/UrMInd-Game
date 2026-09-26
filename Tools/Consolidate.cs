using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;

// Consolida os 90 materiais 'colormap' embutidos nos FBX em um material
// compartilhado por kit, usando o shader triplanar.
//
// Faz isso por REMAP do importer de cada FBX, nao editando a cena nem o prefab:
// assim nao gera milhares de overrides de prefab e e reversivel.
public static class Consolidate
{
    const string MatDir = "Assets/Detail/Materials";
    const string KenneyRoot = "Assets/City 02/Third Party/Kenney";

    // pasta do kit -> (nome do material compartilhado, caminho da colormap)
    static readonly string[][] Kits = new string[][] {
        new string[] { "City Kit (Roads)",      "Kenney_Roads",      KenneyRoot + "/City Kit (Roads)/Textures/colormap.png" },
        new string[] { "City Kit (Commercial)", "Kenney_Commercial", KenneyRoot + "/City Kit (Commercial)/Textures/colormap.png" },
        new string[] { "City Kit (Industrial)", "Kenney_Industrial", KenneyRoot + "/City Kit (Industrial)/Textures/colormap.png" },
        new string[] { "City Kit (Suburban)",   "Kenney_Suburban",   KenneyRoot + "/City Kit (Suburban)/Textures/colormap.png" },
        new string[] { "City Kit (Cars)",       "Kenney_Cars",       KenneyRoot + "/City Kit (Cars)/Models/FBX format/Textures/colormap.png" },
        new string[] { "Blocky Characters",     "Kenney_Characters", KenneyRoot + "/Blocky Characters/Textures/texture-d.png" },
    };

    // Ajuste de detalhe por kit: asfalto e mais grosseiro que carroceria.
    static void TuneForKit(Material m, string kit)
    {
        if (kit.Contains("Roads"))
        {
            m.SetFloat("_DetailScale", 2.2f);      // grao de asfalto, fino
            m.SetFloat("_DetailStrength", 1.35f);
            m.SetFloat("_MacroScale", 0.07f);
            m.SetFloat("_MacroVariation", 0.20f);  // remendos e desgaste
            m.SetFloat("_SmoothnessVariation", 0.16f);
            m.SetFloat("_Smoothness", 0.20f);
        }
        else if (kit.Contains("Cars"))
        {
            m.SetFloat("_DetailScale", 4.0f);      // chapa pintada: grao minimo
            m.SetFloat("_DetailStrength", 0.30f);
            m.SetFloat("_MacroScale", 0.30f);
            m.SetFloat("_MacroVariation", 0.05f);
            m.SetFloat("_SmoothnessVariation", 0.05f);
            m.SetFloat("_Smoothness", 0.62f);
        }
        else if (kit.Contains("Characters"))
        {
            m.SetFloat("_DetailScale", 5.0f);
            m.SetFloat("_DetailStrength", 0.25f);
            m.SetFloat("_MacroVariation", 0.05f);
            m.SetFloat("_SmoothnessVariation", 0.05f);
            m.SetFloat("_Smoothness", 0.30f);
        }
        else
        {
            // predios: reboco/concreto
            m.SetFloat("_DetailScale", 1.4f);
            m.SetFloat("_DetailStrength", 0.85f);
            m.SetFloat("_MacroScale", 0.10f);
            m.SetFloat("_MacroVariation", 0.14f);
            m.SetFloat("_SmoothnessVariation", 0.10f);
            m.SetFloat("_Smoothness", 0.18f);
        }
    }

    public static string CreateSharedMaterials()
    {
        Directory.CreateDirectory(MatDir);
        Shader sh = Shader.Find("City/Kenney Detail");
        if (sh == null) return "ERRO: shader 'City/Kenney Detail' nao encontrado";

        Texture2D pack = AssetDatabase.LoadAssetAtPath<Texture2D>(
            "Assets/Detail/Textures/detail_pack.png");
        if (pack == null) return "ERRO: detail_pack.png nao encontrado";

        StringBuilder sb = new StringBuilder();
        foreach (string[] kit in Kits)
        {
            Texture2D colormap = AssetDatabase.LoadAssetAtPath<Texture2D>(kit[2]);
            if (colormap == null) { sb.AppendLine("SEM TEXTURA: " + kit[2]); continue; }

            string path = MatDir + "/" + kit[1] + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(sh);
                AssetDatabase.CreateAsset(m, path);
            }
            m.shader = sh;
            m.SetTexture("_BaseMap", colormap);
            m.SetTexture("_DetailPack", pack);
            m.SetFloat("_Metallic", 0f);
            m.enableInstancing = true;
            TuneForKit(m, kit[0]);
            EditorUtility.SetDirty(m);
            sb.AppendLine("material " + path + "  base=" + colormap.name);
        }
        AssetDatabase.SaveAssets();
        return sb.ToString();
    }

    public static string RemapFbxMaterials()
    {
        StringBuilder sb = new StringBuilder();
        int fbxTouched = 0, remaps = 0;

        foreach (string[] kit in Kits)
        {
            string folder = KenneyRoot + "/" + kit[0];
            if (!Directory.Exists(folder)) { sb.AppendLine("pasta ausente: " + folder); continue; }

            Material shared = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/" + kit[1] + ".mat");
            if (shared == null) { sb.AppendLine("material ausente: " + kit[1]); continue; }

            string[] guids = AssetDatabase.FindAssets("t:Model", new string[] { folder });
            foreach (string g in guids)
            {
                string fbx = AssetDatabase.GUIDToAssetPath(g);
                ModelImporter mi = AssetImporter.GetAtPath(fbx) as ModelImporter;
                if (mi == null) continue;

                bool changed = false;
                foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(fbx))
                {
                    Material embedded = o as Material;
                    if (embedded == null) continue;
                    mi.AddRemap(new AssetImporter.SourceAssetIdentifier(
                        typeof(Material), embedded.name), shared);
                    changed = true;
                    remaps++;
                }
                if (changed)
                {
                    mi.SaveAndReimport();
                    fbxTouched++;
                }
            }
            sb.AppendLine(kit[0] + " -> " + kit[1]);
        }

        AssetDatabase.Refresh();
        sb.AppendLine("FBX remapeados: " + fbxTouched + ", materiais redirecionados: " + remaps);
        return sb.ToString();
    }
}
