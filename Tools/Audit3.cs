using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;

public static class Audit3
{
    // --- 1. nitidez: o que a camera e o URP asset estao fazendo ---
    public static string Sharpness()
    {
        StringBuilder sb = new StringBuilder();
        foreach (Camera c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            UniversalAdditionalCameraData d = c.GetComponent<UniversalAdditionalCameraData>();
            sb.AppendLine(c.name + " postFX=" + (d == null ? "?" : d.renderPostProcessing.ToString())
                + " AA=" + (d == null ? "?" : d.antialiasing.ToString())
                + " AAquality=" + (d == null ? "?" : d.antialiasingQuality.ToString())
                + " allowMSAA=" + c.allowMSAA
                + " pixelRect=" + c.pixelWidth + "x" + c.pixelHeight
                + " targetTex=" + (c.targetTexture == null ? "backbuffer"
                    : c.targetTexture.width + "x" + c.targetTexture.height));
        }
        UniversalRenderPipelineAsset rp = UniversalRenderPipeline.asset;
        if (rp != null)
            sb.AppendLine("URP renderScale=" + rp.renderScale + " msaa=" + rp.msaaSampleCount
                + " hdr=" + rp.supportsHDR + " upscaling=" + rp.upscalingFilter
                + " fsrSharpness=" + rp.fsrSharpness);
        return sb.ToString();
    }

    // --- 2. decal vazando no carrinho: o que a caixa de projecao alcanca ---
    public static string DecalLeak()
    {
        StringBuilder sb = new StringBuilder();
        DecalProjector[] dps = UnityEngine.Object.FindObjectsByType<DecalProjector>(FindObjectsSortMode.None);
        sb.AppendLine("DecalProjectors: " + dps.Length);
        if (dps.Length > 0)
        {
            DecalProjector d = dps[0];
            sb.AppendLine("exemplo: pos=" + d.transform.position.ToString("F2")
                + " size=" + d.size.ToString("F2") + " pivot=" + d.pivot.ToString("F2")
                + " renderingLayerMask=" + d.renderingLayerMask
                + " -> caixa cobre Y de "
                + (d.transform.position.y - d.size.z * 0.5f).ToString("F2") + " a "
                + (d.transform.position.y + d.size.z * 0.5f).ToString("F2"));
        }
        GameObject cart = GameObject.Find("AnalysisCart");
        if (cart == null)
        {
            AnalysisCartController cc = UnityEngine.Object.FindFirstObjectByType<AnalysisCartController>();
            if (cc != null) cart = cc.gameObject;
        }
        if (cart == null) { sb.AppendLine("carrinho NAO encontrado por nome/componente"); return sb.ToString(); }

        sb.AppendLine("carrinho='" + cart.name + "' pos=" + cart.transform.position.ToString("F2"));
        Renderer[] rs = cart.GetComponentsInChildren<Renderer>(true);
        sb.AppendLine("renderers do carrinho: " + rs.Length);
        foreach (Renderer r in rs.Take(6))
            sb.AppendLine("   " + r.name + " renderingLayerMask=" + r.renderingLayerMask
                + " boundsY=" + r.bounds.min.y.ToString("F2") + ".." + r.bounds.max.y.ToString("F2")
                + " mat=" + (r.sharedMaterial == null ? "null" : r.sharedMaterial.name));
        return sb.ToString();
    }

    // --- 3. fisica: defeitos sem collider ---
    public static string Physics()
    {
        GameObject root = GameObject.Find("City 02/World/Infrastructure Defects");
        if (root == null) return "root de defeitos nao encontrado";

        Dictionary<string, int> noCol = new Dictionary<string, int>();
        Dictionary<string, int> withCol = new Dictionary<string, int>();
        StringBuilder sb = new StringBuilder();

        foreach (DefectInfo info in root.GetComponentsInChildren<DefectInfo>(true))
        {
            string t = info.defectType == null ? "?" : info.defectType;
            bool hasCol = info.GetComponentInChildren<Collider>(true) != null;
            Dictionary<string, int> target = hasCol ? withCol : noCol;
            if (!target.ContainsKey(t)) target[t] = 0;
            target[t]++;
        }

        sb.AppendLine("=== defeitos COM collider ===");
        foreach (KeyValuePair<string, int> kv in withCol) sb.AppendLine("  " + kv.Value + "x " + kv.Key);
        sb.AppendLine("=== defeitos SEM collider ===");
        foreach (KeyValuePair<string, int> kv in noCol) sb.AppendLine("  " + kv.Value + "x " + kv.Key);

        // arvores derrubadas e postes danificados sao objetos separados
        int derrubadaNoCol = 0, derrubadaTotal = 0, danifNoCol = 0, danifTotal = 0;
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name.Contains("DERRUBADA"))
            {
                derrubadaTotal++;
                if (t.GetComponent<Collider>() == null) derrubadaNoCol++;
            }
            if (t.name.Contains("DANIFICADO"))
            {
                danifTotal++;
                if (t.GetComponent<Collider>() == null) danifNoCol++;
            }
        }
        sb.AppendLine("arvores DERRUBADA: " + derrubadaTotal + ", sem collider: " + derrubadaNoCol);
        sb.AppendLine("postes DANIFICADO: " + danifTotal + ", sem collider: " + danifNoCol);
        return sb.ToString();
    }
}
