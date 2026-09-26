using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Relatorio das bandeiras da fachada da FECAP: onde esta cada mastro, onde
// estao o finial e o pano, e onde a ponta do mastro realmente cai.
public static class FlagProbe
{
    public static void Report()
    {
        EditorSceneManager.OpenScene("Assets/City - 02 - Day.unity", OpenSceneMode.Single);
        var flags = GameObject.Find("FECAP/Flags");
        if (flags == null) { Debug.Log("[FlagProbe] FECAP/Flags nao encontrado"); return; }

        var sb = new StringBuilder();
        sb.AppendLine("=== FLAGS INICIO ===");
        foreach (Transform pole in flags.transform)
        {
            sb.AppendLine("GRUPO " + pole.name + " localPos=" + V(pole.localPosition));
            foreach (Transform t in pole)
            {
                sb.AppendLine("  " + t.name
                    + " localPos=" + V(t.localPosition)
                    + " euler=" + V(t.localEulerAngles)
                    + " scale=" + V(t.localScale)
                    + " world=" + V(t.position));
                if (t.name == "Pole")
                {
                    Vector3 dir = t.rotation * Vector3.up;
                    sb.AppendLine("    -> eixo=" + V(dir)
                        + " ponta=" + V(t.position + dir * t.localScale.y)
                        + " base=" + V(t.position - dir * t.localScale.y));
                }
            }
            var anim = pole.GetComponentInChildren<Animator>(true);
            var legacy = pole.GetComponentInChildren<Animation>(true);
            sb.AppendLine("  animator=" + (anim != null) + " animation=" + (legacy != null));
        }
        sb.AppendLine("=== FLAGS FIM ===");
        Debug.Log(sb.ToString());
    }

    internal static string V(Vector3 v)
    {
        return string.Format("({0:F3},{1:F3},{2:F3})", v.x, v.y, v.z);
    }
}

// Reancora finial e pano ao mastro a que pertencem e separa os mastros o
// suficiente para os panos nao se cruzarem.
//
// O bug original: finial e pano tinham Z fixo, escrito a mao, que nao
// acompanhava a inclinacao de 14 graus do mastro -- ficavam soltos no ar. E o
// passo entre mastros (1,6 m) era menor que a largura do pano (~2,03 m
// projetada em X), entao panos vizinhos se sobrepunham.
//
// Aqui tudo e derivado do proprio mastro de cada grupo. Nada e apagado.
public static class FlagFix
{
    const float Spacing = 2.4f;      // passo entre mastros; > largura projetada do pano
    const float CenterX = -10f;      // mantem o conjunto onde ja estava
    const float ClearBelowFinial = 0.35f;
    const float PanelPush = 0.08f;   // afasta o pano do cilindro do mastro

    public static void Apply()
    {
        var scene = EditorSceneManager.OpenScene("Assets/City - 02 - Day.unity", OpenSceneMode.Single);
        var flags = GameObject.Find("FECAP/Flags");
        if (flags == null) { Debug.LogError("[FlagFix] FECAP/Flags nao encontrado"); EditorApplication.Exit(1); return; }

        int n = flags.transform.childCount;
        var sb = new StringBuilder();
        sb.AppendLine("=== FLAGFIX INICIO === mastros=" + n);

        for (int i = 0; i < n; i++)
        {
            Transform group = flags.transform.GetChild(i);
            Transform pole = group.Find("Pole");
            Transform finial = group.Find("Finial");
            Transform flag = group.Find("Flag");
            if (pole == null) { sb.AppendLine("SKIP " + group.name + ": sem Pole"); continue; }

            // 1. novo X do mastro, conjunto centrado onde ja estava
            float x = CenterX + (i - (n - 1) * 0.5f) * Spacing;
            Vector3 pp = pole.localPosition;
            pole.localPosition = new Vector3(x, pp.y, pp.z);

            // 2. geometria real do mastro: Cylinder tem altura 2, entao
            //    localScale.y e o meio-comprimento.
            Vector3 axis = pole.localRotation * Vector3.up;
            Vector3 center = pole.localPosition;
            float half = pole.localScale.y;
            Vector3 tip = center + axis * half;

            // 3. finial na ponta exata
            if (finial != null) finial.localPosition = tip;

            // 4. pano presO ao mastro, abaixo do finial
            if (flag != null)
            {
                float rollRad = flag.localEulerAngles.z * Mathf.Deg2Rad;
                float w = flag.localScale.x;
                float h = flag.localScale.y;
                float spanX = Mathf.Abs(w * Mathf.Cos(rollRad)) + Mathf.Abs(h * Mathf.Sin(rollRad));
                float spanY = Mathf.Abs(h * Mathf.Cos(rollRad)) + Mathf.Abs(w * Mathf.Sin(rollRad));

                float targetY = tip.y - ClearBelowFinial - spanY * 0.5f;
                float s = Mathf.Abs(axis.y) > 1e-4f ? (targetY - center.y) / axis.y : 0f;
                Vector3 onPole = center + axis * s;

                flag.localPosition = new Vector3(
                    onPole.x + spanX * 0.5f,
                    onPole.y,
                    onPole.z - PanelPush);

                sb.AppendLine(group.name + " x=" + x.ToString("F3")
                    + " ponta=" + FlagProbe.V(tip)
                    + " pano=" + FlagProbe.V(flag.localPosition)
                    + " larguraProjetada=" + spanX.ToString("F3"));
            }
        }

        sb.AppendLine("passo=" + Spacing + " (sem sobreposicao enquanto passo > larguraProjetada)");
        sb.AppendLine("=== FLAGFIX FIM ===");
        Debug.Log(sb.ToString());

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
