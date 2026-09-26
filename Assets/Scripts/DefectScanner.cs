using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Varre ocorrencias proximas, mira a camera de analise (PiP canto inferior direito)
// no problema mais proximo e preenche o painel de identificacao.
public class DefectScanner : MonoBehaviour
{
    [Header("Referencias")]
    public Camera analysisCamera;
    public Transform scanMarker;
    public Text typeText;
    public Text severityText;
    public Text distanceText;
    public Text statusText;
    public Image severityBar;

    [Header("Config")]
    public float scanRadius = 32f;
    public float camFollowSpeed = 6f;
    public Vector3 idleEuler = new Vector3(6f, 0f, 0f);

    [Header("Validacao da evidencia")]
    // Tudo que pode tapar a ocorrencia. Precisa incluir predios e muros, nao so
    // a camada das ocorrencias, senao uma parede nao bloqueia nada.
    public LayerMask occluders = ~0;
    // Margem em viewport: 0 exige so estar na tela, 0.08 exige folga na borda.
    public float framingMargin = 0.08f;

    // Alvo atual, lido pelo InspectionGame para registrar a ocorrencia.
    public DefectInfo Current { get; private set; }

    // O InspectionGame liga isto no modo treinamento e desliga no desafio.
    public bool revealIdentity = true;

    // Ocorrencias com geometria, ja filtradas. A central sorteia chamados daqui
    // em vez de varrer a cena de novo e arriscar uma lista diferente.
    public IReadOnlyList<DefectInfo> AllDefects
    {
        get { return defects ?? System.Array.Empty<DefectInfo>(); }
    }

    DefectInfo[] defects;
    Renderer[] markerRenderers;
    MaterialPropertyBlock mpb;
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");
    float pulse;

    void Start()
    {
        // Uma ocorrencia sem geometria visivel nao pode ser fotografada, entao
        // nao pode valer ponto: some da varredura em vez de virar um alvo que o
        // jogador nunca consegue enquadrar.
        var all = Object.FindObjectsByType<DefectInfo>(FindObjectsInactive.Exclude);
        var usable = new System.Collections.Generic.List<DefectInfo>(all.Length);
        int dropped = 0;
        foreach (var d in all)
        {
            if (HasVisibleGeometry(d)) usable.Add(d); else dropped++;
        }
        defects = usable.ToArray();
        if (dropped > 0)
            Debug.LogWarning("[DefectScanner] " + dropped + " ocorrencia(s) sem geometria visivel foram ignoradas.");
        markerRenderers = scanMarker != null ? scanMarker.GetComponentsInChildren<Renderer>(true) : new Renderer[0];
        mpb = new MaterialPropertyBlock();
        if (scanMarker != null) scanMarker.gameObject.SetActive(false);
    }

    // Rachadura e DecalProjector, nao Renderer: exigir Renderer aqui apagaria o
    // tipo "Rachadura no asfalto" inteiro da varredura.
    public static bool HasVisibleGeometry(DefectInfo d)
    {
        if (d == null) return false;

        foreach (var r in d.GetComponentsInChildren<Renderer>(false))
            if (r.enabled && r.gameObject.activeInHierarchy) return true;

        foreach (var dp in d.GetComponentsInChildren<UnityEngine.Rendering.Universal.DecalProjector>(false))
            if (dp.enabled && dp.gameObject.activeInHierarchy) return true;

        return false;
    }

    // Ponto que a camera precisa enxergar para a evidencia valer.
    public static Vector3 AimPoint(DefectInfo d)
    {
        return d.transform.position + Vector3.up * d.markerHeight;
    }

    // Quao boa e a foto que a camera de analise consegue tirar agora.
    public enum Evidence { Blocked, Poor, Fine, Excellent }

    // Avalia o enquadramento e a linha de visada da ocorrencia.
    // Fica aqui porque e este script que conhece a camera de analise: duplicar a
    // checagem no InspectionGame criaria duas respostas possiveis para a mesma
    // pergunta.
    public Evidence EvidenceQuality(DefectInfo d, out string reason)
    {
        reason = null;
        if (d == null) { reason = "Nenhuma ocorrencia no alcance."; return Evidence.Blocked; }
        if (analysisCamera == null) return Evidence.Fine;

        Vector3 aim = AimPoint(d);

        Vector3 vp = analysisCamera.WorldToViewportPoint(aim);
        if (vp.z <= 0f
            || vp.x < framingMargin || vp.x > 1f - framingMargin
            || vp.y < framingMargin || vp.y > 1f - framingMargin)
        {
            reason = "Enquadre a ocorrencia na camera de analise.";
            return Evidence.Blocked;
        }

        Vector3 from = analysisCamera.transform.position;
        Vector3 dir = aim - from;
        float dist = dir.magnitude;
        if (dist > 0.01f)
        {
            // RaycastAll porque o primeiro hit costuma ser o proprio rover: a
            // camera de analise vai montada nele. Descartar o rover e a propria
            // ocorrencia e so entao decidir se algo tapa a vista.
            var hits = Physics.RaycastAll(from, dir / dist, dist - 0.15f,
                                          occluders, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                Transform t = hits[i].collider.transform;
                if (t.IsChildOf(transform)) continue;
                if (hits[i].collider.GetComponentInParent<DefectInfo>() == d) continue;
                reason = "Ocorrencia encoberta por " + hits[i].collider.name + ".";
                return Evidence.Blocked;
            }
        }

        // Centralizacao e proximidade decidem entre adequada e excelente. Sao as
        // duas coisas que o jogador controla dirigindo e mirando.
        float offCenter = Mathf.Max(Mathf.Abs(vp.x - 0.5f), Mathf.Abs(vp.y - 0.5f)) * 2f;
        float near = Mathf.Clamp01(dist / scanRadius);

        if (offCenter <= 0.35f && near <= 0.35f) return Evidence.Excellent;
        if (offCenter <= 0.70f && near <= 0.75f) return Evidence.Fine;

        reason = "Enquadramento insuficiente: centralize e aproxime.";
        return Evidence.Poor;
    }

    // Atalho para quem so quer saber se da para registrar.
    public string BlockedReason(DefectInfo d)
    {
        string reason;
        return EvidenceQuality(d, out reason) == Evidence.Blocked ? (reason ?? "Sem visada.") : null;
    }

    void Update()
    {
        DefectInfo best = null;
        float bestSqr = scanRadius * scanRadius;
        Vector3 me = transform.position;

        if (defects != null)
        {
            for (int i = 0; i < defects.Length; i++)
            {
                DefectInfo d = defects[i];
                if (d == null) continue;
                float s = (d.transform.position - me).sqrMagnitude;
                if (s < bestSqr) { bestSqr = s; best = d; }
            }
        }

        Current = best;
        if (best != null) ShowDetected(best, Mathf.Sqrt(bestSqr));
        else ShowIdle();
    }

    void ShowDetected(DefectInfo d, float dist)
    {
        Color c = d.SeverityColor();
        Vector3 aim = AimPoint(d);

        if (scanMarker != null)
        {
            if (!scanMarker.gameObject.activeSelf) scanMarker.gameObject.SetActive(true);
            scanMarker.position = d.transform.position;
            scanMarker.Rotate(0f, 50f * Time.deltaTime, 0f, Space.World);
            pulse += Time.deltaTime * 3.2f;
            float p = 1f + Mathf.Sin(pulse) * 0.14f;
            scanMarker.localScale = new Vector3(d.markerScale * p, d.markerScale, d.markerScale * p);
            // MaterialPropertyBlock, nao sharedMaterial: escrever em
            // sharedMaterial.color grava no arquivo .mat do projeto e a cor da
            // ultima ocorrencia vista fica salva em disco.
            for (int i = 0; i < markerRenderers.Length; i++)
            {
                if (markerRenderers[i] == null) continue;
                markerRenderers[i].GetPropertyBlock(mpb);
                mpb.SetColor(BaseColorId, c);
                mpb.SetColor(ColorId, c);
                markerRenderers[i].SetPropertyBlock(mpb);
            }
        }

        if (analysisCamera != null)
        {
            Vector3 dir = aim - analysisCamera.transform.position;
            if (dir.sqrMagnitude > 0.01f)
            {
                Quaternion want = Quaternion.LookRotation(dir);
                analysisCamera.transform.rotation = Quaternion.Slerp(
                    analysisCamera.transform.rotation, want,
                    1f - Mathf.Exp(-camFollowSpeed * Time.deltaTime));
            }
        }

        // Com revealIdentity desligado o painel mostra que ha algo, mas nao diz o
        // que e nem quao grave: essa e a resposta que o jogador tem de dar.
        Color shown = revealIdentity ? c : new Color(0.55f, 0.75f, 0.9f);

        if (typeText != null)
        {
            typeText.text = revealIdentity ? d.defectType.ToUpper() : "POSSIVEL PROBLEMA";
            typeText.color = Color.Lerp(shown, Color.white, 0.25f);
        }
        if (severityText != null)
        {
            severityText.text = revealIdentity ? "GRAVIDADE " + d.severity : "GRAVIDADE A CLASSIFICAR";
            severityText.color = Color.Lerp(shown, Color.white, 0.45f);
        }
        if (distanceText != null) { distanceText.text = "ID " + d.code + "   DIST " + dist.ToString("F1") + "m"; distanceText.color = new Color(0.78f, 0.85f, 0.92f); }
        if (statusText != null)
        {
            statusText.text = revealIdentity ? "* PROBLEMA IDENTIFICADO" : "* ANOMALIA DETECTADA";
            statusText.color = shown;
        }
        if (severityBar != null)
        {
            severityBar.color = shown;
            RectTransform rt = severityBar.rectTransform;
            rt.anchorMax = new Vector2(Mathf.Clamp01(1f - dist / scanRadius), rt.anchorMax.y);
        }
    }

    void ShowIdle()
    {
        if (scanMarker != null && scanMarker.gameObject.activeSelf) scanMarker.gameObject.SetActive(false);

        if (analysisCamera != null)
        {
            analysisCamera.transform.localRotation = Quaternion.Slerp(
                analysisCamera.transform.localRotation, Quaternion.Euler(idleEuler),
                1f - Mathf.Exp(-3f * Time.deltaTime));
        }

        Color idle = new Color(0.55f, 0.75f, 0.9f);
        if (typeText != null) { typeText.text = "VIA SEM OCORRENCIAS"; typeText.color = idle; }
        if (severityText != null) { severityText.text = "GRAVIDADE --"; severityText.color = idle; }
        if (distanceText != null) distanceText.text = "VARRENDO SETOR...";
        if (statusText != null) { statusText.text = "o ESCANEANDO"; statusText.color = idle; }
        if (severityBar != null)
        {
            severityBar.color = idle;
            RectTransform rt = severityBar.rectTransform;
            rt.anchorMax = new Vector2(0f, rt.anchorMax.y);
        }
    }
}
