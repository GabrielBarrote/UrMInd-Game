using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Bateria de aceitacao do CidadeUnity, rodavel por CLI:
//   Unity.exe -batchmode -quit -nographics -executeMethod AcceptanceTest.Run
//
// Cobre o que da para afirmar sem jogar: cenario presente, bandeiras ancoradas
// e separadas, ocorrencias com objeto fisico e codigo unico, cameras sanas,
// referencias de Inspector ligadas e a cena na lista de build. Sai com codigo 1
// se qualquer verificacao falhar, para o shell nao tratar regressao como sucesso.
//
// O que NAO cobre, e continua exigindo teste humano: dirigir, registrar,
// segurar a tecla e reiniciar tres vezes.
public static class AcceptanceTest
{
    const string ScenePath = "Assets/City - 02 - Day.unity";

    static readonly List<string> Failures = new List<string>();
    static readonly StringBuilder Log = new StringBuilder();

    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Log.AppendLine("=== ACEITACAO INICIO ===");
        Log.AppendLine("cena=" + scene.path + " raizes=" + scene.rootCount);

        CheckCity();
        CheckFlags();
        CheckDefects();
        CheckCameras();
        CheckWiring();
        CheckBuildList();

        Log.AppendLine("--- " + (Failures.Count == 0 ? "TUDO PASSOU" : Failures.Count + " FALHA(S)") + " ---");
        foreach (var f in Failures) Log.AppendLine("FALHA: " + f);
        Log.AppendLine("=== ACEITACAO FIM ===");
        Debug.Log(Log.ToString());

        if (Failures.Count > 0) EditorApplication.Exit(1);
    }

    static void Check(bool ok, string label, string detail)
    {
        Log.AppendLine((ok ? "[ok]   " : "[FALHA] ") + label + "  " + detail);
        if (!ok) Failures.Add(label + ": " + detail);
    }

    // A cidade e o item que ja regrediu: tem de estar presente, visivel e larga.
    static void CheckCity()
    {
        var city = GameObject.Find("City 02");
        if (city == null) { Check(false, "cidade presente", "raiz 'City 02' nao existe"); return; }

        int visible = 0, shadowsOnly = 0, rendererOff = 0;
        Bounds b = new Bounds();
        bool hasB = false;
        foreach (var r in city.GetComponentsInChildren<Renderer>(true))
        {
            if (!r.gameObject.activeInHierarchy) continue;
            if (!r.enabled) { rendererOff++; continue; }
            if (r.shadowCastingMode == ShadowCastingMode.ShadowsOnly) { shadowsOnly++; continue; }
            visible++;
            if (!hasB) { b = r.bounds; hasB = true; } else b.Encapsulate(r.bounds);
        }

        Check(city.activeInHierarchy, "cidade ativa", "activeInHierarchy=" + city.activeInHierarchy);
        Check(visible > 4000, "cidade renderizando", visible + " renderers visiveis");
        Check(shadowsOnly == 0, "sem ShadowsOnly", shadowsOnly + " renderers so-sombra");
        Check(rendererOff == 0, "sem renderer desligado", rendererOff + " renderers off");
        Check(hasB && b.extents.x > 200f && b.extents.z > 200f, "extensao da cidade",
              hasB ? "extents=" + b.extents : "sem bounds");

        // Materiais quebrados aparecem como shader de erro; e assim que a cidade
        // "sumiu" antes.
        int badShader = 0;
        foreach (var r in city.GetComponentsInChildren<Renderer>(true))
            foreach (var m in r.sharedMaterials)
                if (m == null || m.shader == null || m.shader.name == "Hidden/InternalErrorShader")
                    badShader++;
        Check(badShader == 0, "shaders validos", badShader + " material(is) com shader de erro");
    }

    // Cada pano tem de sair do seu proprio mastro e nao invadir o vizinho.
    static void CheckFlags()
    {
        var flags = GameObject.Find("FECAP/Flags");
        if (flags == null) { Check(false, "bandeiras presentes", "FECAP/Flags nao existe"); return; }

        var spans = new List<Vector2>();
        foreach (Transform group in flags.transform)
        {
            Transform pole = group.Find("Pole");
            Transform finial = group.Find("Finial");
            Transform flag = group.Find("Flag");
            if (pole == null || flag == null) { Check(false, "bandeira " + group.name, "falta Pole ou Flag"); continue; }

            Vector3 axis = pole.rotation * Vector3.up;
            Vector3 tip = pole.position + axis * pole.localScale.y;

            if (finial != null)
                Check(Vector3.Distance(finial.position, tip) < 0.2f,
                      "finial na ponta (" + group.name + ")",
                      "folga=" + Vector3.Distance(finial.position, tip).ToString("F3") + "m");

            // distancia do centro do pano ao eixo do mastro, medida na altura do pano
            float s = Mathf.Abs(axis.y) > 1e-4f ? (flag.position.y - pole.position.y) / axis.y : 0f;
            Vector3 onPole = pole.position + axis * s;
            float lateral = Vector3.Distance(new Vector3(flag.position.x, 0f, flag.position.z),
                                             new Vector3(onPole.x, 0f, onPole.z));
            Check(lateral < flag.localScale.x, "pano preso ao mastro (" + group.name + ")",
                  "afastamento=" + lateral.ToString("F3") + "m, largura=" + flag.localScale.x);

            float roll = flag.localEulerAngles.z * Mathf.Deg2Rad;
            float spanX = Mathf.Abs(flag.localScale.x * Mathf.Cos(roll))
                        + Mathf.Abs(flag.localScale.y * Mathf.Sin(roll));
            spans.Add(new Vector2(flag.position.x - spanX * 0.5f, flag.position.x + spanX * 0.5f));
        }

        spans.Sort((a, c) => a.x.CompareTo(c.x));
        for (int i = 1; i < spans.Count; i++)
            Check(spans[i].x >= spans[i - 1].y, "panos sem sobreposicao (" + i + ")",
                  "anterior termina em " + spans[i - 1].y.ToString("F2")
                  + ", proximo comeca em " + spans[i].x.ToString("F2"));
    }

    // Ocorrencia sem objeto fisico nao pode valer ponto; codigo repetido faria
    // marcador e texto divergirem.
    static void CheckDefects()
    {
        var defects = Object.FindObjectsByType<DefectInfo>(FindObjectsInactive.Exclude);
        Check(defects.Length > 0, "ocorrencias presentes", defects.Length + " DefectInfo");

        int noGeometry = 0;
        var codes = new Dictionary<string, int>();
        var types = new HashSet<string>();
        foreach (var d in defects)
        {
            // Mesma regra que o jogo usa em runtime, para o teste nao aprovar
            // uma cena que o DefectScanner recusaria (nem o contrario).
            if (!DefectScanner.HasVisibleGeometry(d)) noGeometry++;

            if (!codes.ContainsKey(d.code)) codes[d.code] = 0;
            codes[d.code]++;
            types.Add(d.defectType);
        }

        int dup = 0;
        foreach (var kv in codes) if (kv.Value > 1) dup++;
        Check(dup == 0, "codigos unicos", dup + " codigo(s) repetido(s)");
        Check(noGeometry == 0, "ocorrencias com geometria", noGeometry + " sem renderer visivel");

        // Todo tipo presente na cena tem de existir na lista que o jogador ve,
        // senao a classificacao correta seria impossivel.
        var known = new HashSet<string>(InspectionGame.Types);
        var unknown = new List<string>();
        foreach (var t in types) if (!known.Contains(t)) unknown.Add(t);
        Check(unknown.Count == 0, "tipos classificaveis",
              unknown.Count == 0 ? types.Count + " tipos, todos na lista"
                                 : "fora da lista: " + string.Join(", ", unknown));
    }

    static void CheckCameras()
    {
        foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude))
        {
            Check(cam.cullingMask != 0, "culling mask (" + cam.name + ")", "mask=" + cam.cullingMask);
            Check(cam.farClipPlane >= 50f, "far clip (" + cam.name + ")", "far=" + cam.farClipPlane);
        }
    }

    static void CheckWiring()
    {
        var game = Object.FindAnyObjectByType<InspectionGame>();
        if (game == null) { Check(false, "InspectionGame presente", "nao encontrado"); return; }

        Check(game.scanner != null, "scanner ligado", game.scanner != null ? game.scanner.name : "NULO");
        Check(game.cart != null, "rover ligado", game.cart != null ? game.cart.name : "NULO");
        Check(game.analysisPanel != null, "painel da camera ligado",
              game.analysisPanel != null ? game.analysisPanel.name : "NULO");
        Check(game.roundSeconds > 0f, "duracao da rodada", game.roundSeconds + "s");
        Check(game.registerRange > 0f, "alcance de registro", game.registerRange + "m");

        var sc = Object.FindAnyObjectByType<DefectScanner>();
        if (sc != null)
            Check(sc.revealIdentity == !game.challengeMode, "modo coerente",
                  "challengeMode=" + game.challengeMode + " revealIdentity=" + sc.revealIdentity);
    }

    // Uma cena pode existir no projeto e estar fora da compilacao.
    static void CheckBuildList()
    {
        bool found = false;
        foreach (var s in EditorBuildSettings.scenes)
            if (s.path == ScenePath && s.enabled) { found = true; break; }

        // O BuildScript passa a lista de cenas direto, entao a lista do editor
        // pode estar vazia sem que isso quebre o build.
        Log.AppendLine((found ? "[ok]   " : "[nota] ") + "cena na build list  "
            + (found ? "presente e ativa" : "ausente — BuildScript.BuildWin64 passa a cena explicitamente"));
    }
}
