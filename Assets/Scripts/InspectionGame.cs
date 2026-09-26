using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

// Rodada cronometrada de inspecao.
//
// Fluxo: MENU (digita o nome) -> JOGANDO (60s, registra ocorrencias com E)
//        -> RESULTADO (quantas de cada tipo, pontos, e o ranking).
//
// O registro reaproveita o DefectScanner que ja existia: ele aponta a camera de
// analise no defeito mais proximo, e o jogador confirma com E. Nao ha um segundo
// sistema de deteccao.
public class InspectionGame : MonoBehaviour
{
    enum State { Menu, Playing, Results }

    [Header("Referencias de cena")]
    public DefectScanner scanner;
    public AnalysisCartController cart;

    [Header("Menu")]
    public GameObject menuPanel;
    public InputField nameField;
    public Button startButton;
    public Text menuWarning;

    [Header("HUD")]
    public GameObject hudPanel;
    // Painel da camera de analise. E um Canvas proprio, fora do hudPanel, entao
    // precisa ser desligado a parte ou continua desenhado sobre o resultado.
    public GameObject analysisPanel;
    public Text timerText;
    public Text scoreText;
    public Text countText;
    public Text hintText;
    public Text toastText;

    [Header("Resultado")]
    public GameObject resultsPanel;
    public Text resultHeader;
    public Text breakdownText;
    public Text rankingText;
    public Button againButton;

    [Header("Largada")]
    // Toda rodada comeca na via em frente a FECAP, olhando para o predio.
    public Vector3 spawnPosition = new Vector3(0f, 0.20f, -1.5f);
    public float spawnYaw = 0f;

    [Header("Regras")]
    public float roundSeconds = 120f;
    public float registerRange = 20f;
    // Modo desafio: o jogador classifica a ocorrencia depois de fotografar, e o
    // painel nao entrega tipo e gravidade antes da resposta.
    public bool challengeMode = true;
    // Tempo que o rover precisa ficar quase parado para a foto valer.
    public float stabilitySeconds = 2f;
    public float stabilitySpeed = 0.8f;

    [Header("Central de chamados")]
    // A central manda o jogador a uma regiao, sem marcar o alvo. E o que da
    // funcao a cidade: sem isto, o jogo vira seguir o indicador mais proximo.
    public bool dispatchMode = true;
    public int dispatchBonus = 15;
    public float dispatchMinRange = 50f;
    public float dispatchMaxRange = 260f;

    [Header("Pontuacao")]
    public int pointsEvidence = 40;
    public int pointsCategory = 30;
    public int pointsPriority = 20;
    public int pointsFraming = 10;
    public int penaltyFalseReport = -25;

    // Ordem fixa: e o que o jogador ve numerado na tela e o que o codigo compara.
    public static readonly string[] Types =
    {
        "Buraco no pavimento",
        "Rachadura no asfalto",
        "Bueiro sem tampa",
        "Bueiro danificado",
        "Poste caido com fiacao",
        "Poste inclinado - risco de queda",
        "Arvore derrubada",
        "Entulho na via",
    };

    public static readonly string[] Priorities = { "CRITICA", "ALTA", "MEDIA", "BAIXA" };

    // Um registro confirmado. Contador, resumo e ranking leem daqui e de mais
    // lugar nenhum, entao as tres partes da tela nunca discordam.
    struct Record
    {
        public EntityId id;
        public string type;
        public string severity;
        public int points;
        public bool typeOk;
        public bool priorityOk;
        public DefectScanner.Evidence quality;
    }

    enum Step { Roaming, PickType, PickPriority }

    State state = State.Menu;
    Step step = Step.Roaming;
    float timeLeft;
    string playerName = "";
    readonly List<Record> confirmed = new List<Record>();
    readonly HashSet<EntityId> seen = new HashSet<EntityId>();
    float toastTimer;
    bool roundClosed;

    // ocorrencia em classificacao
    DefectInfo pending;
    DefectScanner.Evidence pendingQuality;
    int pendingTypeChoice = -1;

    // confirmacao de registro falso: o primeiro E avisa, o segundo penaliza
    float falseReportArmed;

    // estabilidade da plataforma
    float steadyFor;

    // O texto de ajuda normalmente ocupa apenas uma linha. Durante a
    // classificacao ele cresce e recebe um fundo para manter as opcoes legiveis.
    GameObject classificationBackground;
    Vector2 normalHintSize;
    TextAnchor normalHintAlignment;

    // chamado atual da central
    DefectInfo dispatchTarget;
    // ocorrencias que o jogador chegou a ver mas nao chegou a registrar
    readonly HashSet<EntityId> spotted = new HashSet<EntityId>();

    int Score
    {
        get
        {
            int t = 0;
            for (int i = 0; i < confirmed.Count; i++) t += confirmed[i].points;
            return t;
        }
    }

    // ------------------------------------------------------------- ciclo

    void Start()
    {
        // Garante a nova duracao tambem em builds recuperadas cuja cena ainda
        // tenha armazenado o valor antigo no componente serializado.
        roundSeconds = 120f;
        // Tambem inicializado daqui para manter compatibilidade com builds
        // recuperados que ainda nao possuem a tabela de bootstraps atualizada.
        UrMIndRuntimeSettings.Apply();
        EnsureRuntimeSystems();
        PrepareClassificationPanel();
        FixSaoPauloFlagOrientation();
        if (startButton != null) startButton.onClick.AddListener(TryStart);
        if (againButton != null) againButton.onClick.AddListener(GoToMenu);
        GoToMenu();
    }

    static void EnsureRuntimeSystems()
    {
        if (FindAnyObjectByType<DayNightCycle>() == null)
            new GameObject("Day Night Cycle").AddComponent<DayNightCycle>();
        if (FindAnyObjectByType<CityLightBudget>() == null)
            new GameObject("City Light Budget").AddComponent<CityLightBudget>();
        if (FindAnyObjectByType<RuntimePerformanceMonitor>() == null)
            new GameObject("Local Performance Monitor").AddComponent<RuntimePerformanceMonitor>();
    }

    void PrepareClassificationPanel()
    {
        if (hintText == null) return;

        RectTransform hintRect = hintText.rectTransform;
        normalHintSize = hintRect.sizeDelta;
        normalHintAlignment = hintText.alignment;
        hintText.supportRichText = true;

        var background = new GameObject("Classification Background",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform backgroundRect = (RectTransform)background.transform;
        backgroundRect.SetParent(hintRect.parent, false);
        backgroundRect.anchorMin = hintRect.anchorMin;
        backgroundRect.anchorMax = hintRect.anchorMax;
        backgroundRect.pivot = hintRect.pivot;
        backgroundRect.anchoredPosition = hintRect.anchoredPosition;
        backgroundRect.sizeDelta = new Vector2(680f, 310f);
        backgroundRect.SetSiblingIndex(hintRect.GetSiblingIndex());

        Image image = background.GetComponent<Image>();
        image.color = new Color(0.015f, 0.055f, 0.04f, 0.90f);
        image.raycastTarget = false;
        classificationBackground = background;
        classificationBackground.SetActive(false);
    }

    void SetClassificationPanelVisible(bool visible)
    {
        if (classificationBackground != null)
            classificationBackground.SetActive(visible);
        if (hintText == null) return;

        hintText.rectTransform.sizeDelta = visible
            ? new Vector2(640f, 280f)
            : normalHintSize;
        hintText.alignment = visible ? TextAnchor.MiddleLeft : normalHintAlignment;
        hintText.lineSpacing = visible ? 1.12f : 1f;
    }

    static void FixSaoPauloFlagOrientation()
    {
        Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Include);
        for (int r = 0; r < renderers.Length; r++)
        {
            Material[] sharedMaterials = renderers[r].sharedMaterials;
            bool usesSaoPauloFlag = false;
            for (int s = 0; s < sharedMaterials.Length; s++)
            {
                Material shared = sharedMaterials[s];
                if (shared != null && shared.name.StartsWith("Fec_FlagSP"))
                {
                    usesSaoPauloFlag = true;
                    break;
                }
            }
            if (!usesSaoPauloFlag) continue;

            // Instancia material apenas no renderer da bandeira, sem duplicar
            // centenas de materiais da cidade durante a busca.
            Material[] materials = renderers[r].materials;
            for (int m = 0; m < materials.Length; m++)
            {
                Material material = materials[m];
                if (material == null || !material.name.StartsWith("Fec_FlagSP")) continue;
                if (material.HasProperty("_BaseMap"))
                {
                    material.SetTextureScale("_BaseMap", new Vector2(-1f, 1f));
                    material.SetTextureOffset("_BaseMap", new Vector2(1f, 0f));
                }
                if (material.HasProperty("_MainTex"))
                {
                    material.SetTextureScale("_MainTex", new Vector2(-1f, 1f));
                    material.SetTextureOffset("_MainTex", new Vector2(1f, 0f));
                }
            }
        }
    }

    void Update()
    {
        if (state == State.Playing)
        {
            timeLeft -= Time.deltaTime;
            TrackStability();
            if (timeLeft <= 0f) { timeLeft = 0f; EndRound(); }
            else
            {
                if (falseReportArmed > 0f) falseReportArmed -= Time.deltaTime;
                if (step == Step.Roaming) HandleRegisterInput();
                else HandleClassifyInput();
                UpdateHud();
            }
        }
        else if (state == State.Menu)
        {
            // Enter tambem inicia, para nao obrigar o clique no botao
            Keyboard kb = Keyboard.current;
            if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
                TryStart();
        }

        if (toastTimer > 0f)
        {
            toastTimer -= Time.deltaTime;
            if (toastText != null)
            {
                Color c = toastText.color;
                c.a = Mathf.Clamp01(toastTimer / 0.6f);
                toastText.color = c;
            }
        }
    }

    // ------------------------------------------------------------ estados

    // Reposiciona o rover no ponto de largada. O CharacterController precisa
    // ser desligado: com ele ativo, setar transform.position e ignorado.
    void MoveToSpawn()
    {
        if (cart == null) return;
        CharacterController cc = cart.GetComponent<CharacterController>();
        bool was = cc != null && cc.enabled;
        if (cc != null) cc.enabled = false;
        cart.transform.position = spawnPosition;
        cart.transform.rotation = Quaternion.Euler(0f, spawnYaw, 0f);
        if (cc != null) cc.enabled = was;
        cart.SnapCamera();
    }

    void GoToMenu()
    {
        state = State.Menu;
        MoveToSpawn();
        if (menuPanel != null) menuPanel.SetActive(true);
        if (hudPanel != null) hudPanel.SetActive(false);
        if (analysisPanel != null) analysisPanel.SetActive(false);
        if (resultsPanel != null) resultsPanel.SetActive(false);
        if (menuWarning != null) menuWarning.text = "";
        if (cart != null) cart.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (nameField != null) nameField.Select();
    }

    void TryStart()
    {
        if (state != State.Menu) return;
        string n = nameField != null ? nameField.text.Trim() : "";
        if (string.IsNullOrEmpty(n))
        {
            if (menuWarning != null) menuWarning.text = "Digite um nome de usuario para comecar.";
            return;
        }
        playerName = InspectionScoring.NormalizePlayerName(n);

        // Limpa so o estado da rodada. O ranking em PlayerPrefs e historico e
        // continua intacto.
        confirmed.Clear();
        seen.Clear();
        spotted.Clear();
        dispatchTarget = null;
        pending = null;
        pendingTypeChoice = -1;
        step = Step.Roaming;
        steadyFor = 0f;
        falseReportArmed = 0f;
        roundClosed = false;
        timeLeft = roundSeconds;
        state = State.Playing;
        MoveToSpawn();

        if (menuPanel != null) menuPanel.SetActive(false);
        if (resultsPanel != null) resultsPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(true);
        if (analysisPanel != null) analysisPanel.SetActive(true);
        if (cart != null) cart.enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (scanner != null) scanner.revealIdentity = !challengeMode;
        NextDispatch();
        Toast(challengeMode
                ? "Desafio: enquadre, estabilize, capture com E e classifique."
                : "Treinamento: aproxime-se e pressione E.",
              new Color(0.6f, 0.9f, 1f));
    }

    // O rover precisa estar quase parado para a foto valer: dirigir e fotografar
    // ao mesmo tempo deixaria a captura trivial.
    void TrackStability()
    {
        float v = cart != null ? Mathf.Abs(cart.CurrentSpeed) : 0f;
        if (v <= stabilitySpeed) steadyFor += Time.deltaTime;
        else steadyFor = 0f;
    }

    bool IsSteady { get { return steadyFor >= stabilitySeconds; } }

    // Motivo por que a ocorrencia nao pode ser registrada agora, ou null se pode.
    // Uma unica funcao decide: o HUD e a tecla E leem a mesma resposta, entao a
    // dica nunca promete um registro que a confirmacao vai recusar.
    string RegisterBlockedReason(DefectInfo d)
    {
        if (state != State.Playing) return "Rodada encerrada.";
        if (d == null) return "Nenhuma ocorrencia no alcance.";
        if (seen.Contains(d.GetEntityId())) return "Ja registrada.";

        float dist = Vector3.Distance(transform.position, d.transform.position);
        if (dist > registerRange) return "Aproxime-se mais (" + dist.ToString("F0") + "m).";

        if (scanner != null)
        {
            string reason;
            var q = scanner.EvidenceQuality(d, out reason);
            if (q == DefectScanner.Evidence.Blocked || q == DefectScanner.Evidence.Poor)
                return reason ?? "Sem visada.";
        }

        if (!IsSteady) return "Estabilize a camera (" + (stabilitySeconds - steadyFor).ToString("F1") + "s).";
        return null;
    }

    void HandleRegisterInput()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null || !kb.eKey.wasPressedThisFrame) return;

        DefectInfo d = scanner != null ? scanner.Current : null;
        string blocked = RegisterBlockedReason(d);
        if (blocked != null)
        {
            // Sem ocorrencia por perto, insistir no E e um registro falso. Exige
            // duas confirmacoes para nao punir quem so apertou explorando.
            if (d == null)
            {
                if (falseReportArmed > 0f)
                {
                    falseReportArmed = 0f;
                    confirmed.Add(new Record
                    {
                        id = default,
                        type = "Registro falso",
                        severity = "BAIXA",
                        points = penaltyFalseReport,
                        quality = DefectScanner.Evidence.Blocked,
                    });
                    Toast(penaltyFalseReport + "  REGISTRO FALSO", new Color(1f, 0.35f, 0.3f));
                    return;
                }
                falseReportArmed = 2f;
                Toast("Nada aqui. [E] de novo confirma registro falso (" + penaltyFalseReport + ").",
                      new Color(1f, 0.75f, 0.3f));
                return;
            }
            Toast(blocked, new Color(1f, 0.75f, 0.3f));
            return;
        }

        string why;
        pendingQuality = scanner.EvidenceQuality(d, out why);
        pending = d;
        seen.Add(d.GetEntityId());

        if (!challengeMode)
        {
            Commit(d, true, true);
            return;
        }

        step = Step.PickType;
        pendingTypeChoice = -1;
        Toast("Evidencia capturada. Classifique o tipo.", new Color(0.6f, 0.9f, 1f));
    }

    void HandleClassifyInput()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        // A ocorrencia pode sumir entre a captura e a resposta; sem isto o
        // registro ficaria preso esperando uma classificacao impossivel.
        if (pending == null) { step = Step.Roaming; return; }

        int pick = ReadNumberKey(kb);
        if (pick < 0) return;

        if (step == Step.PickType)
        {
            if (pick >= Types.Length) return;
            pendingTypeChoice = pick;
            step = Step.PickPriority;
            Toast("Tipo anotado. Agora a prioridade.", new Color(0.6f, 0.9f, 1f));
            return;
        }

        if (pick >= Priorities.Length) return;
        bool typeOk = Types[pendingTypeChoice] == pending.defectType;
        bool prioOk = Priorities[pick] == pending.severity;
        Commit(pending, typeOk, prioOk);
    }

    static int ReadNumberKey(Keyboard kb)
    {
        if (kb.digit1Key.wasPressedThisFrame) return 0;
        if (kb.digit2Key.wasPressedThisFrame) return 1;
        if (kb.digit3Key.wasPressedThisFrame) return 2;
        if (kb.digit4Key.wasPressedThisFrame) return 3;
        if (kb.digit5Key.wasPressedThisFrame) return 4;
        if (kb.digit6Key.wasPressedThisFrame) return 5;
        if (kb.digit7Key.wasPressedThisFrame) return 6;
        if (kb.digit8Key.wasPressedThisFrame) return 7;
        return -1;
    }

    void Commit(DefectInfo d, bool typeOk, bool priorityOk)
    {
        bool answeredCall = dispatchTarget == d;
        int pts = InspectionScoring.Calculate(
            typeOk,
            priorityOk,
            pendingQuality == DefectScanner.Evidence.Excellent,
            answeredCall,
            pointsEvidence,
            pointsCategory,
            pointsPriority,
            pointsFraming,
            dispatchBonus);

        confirmed.Add(new Record
        {
            id = d.GetEntityId(),
            type = d.defectType,
            severity = d.severity,
            points = pts,
            typeOk = typeOk,
            priorityOk = priorityOk,
            quality = pendingQuality,
        });

        Toast("+" + pts + "  " + d.defectType.ToUpper() + "  [" + d.severity + "]"
              + (answeredCall ? "  CHAMADO ATENDIDO" : "")
              + (typeOk ? "" : "  tipo errado")
              + (priorityOk ? "" : "  prioridade errada"),
              d.SeverityColor());

        pending = null;
        pendingTypeChoice = -1;
        step = Step.Roaming;
        steadyFor = 0f;
        spotted.Remove(d.GetEntityId());

        if (answeredCall || dispatchTarget == null) NextDispatch();
    }

    void EndRound()
    {
        // O resultado e gravado uma vez so. Sem esta guarda, qualquer caminho
        // que chame EndRound de novo duplicaria a entrada no ranking.
        if (roundClosed) return;
        roundClosed = true;

        state = State.Results;
        step = Step.Roaming;
        pending = null;
        if (hudPanel != null) hudPanel.SetActive(false);
        if (analysisPanel != null) analysisPanel.SetActive(false);
        if (resultsPanel != null) resultsPanel.SetActive(true);
        if (cart != null) cart.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        int total = Score;
        SaveScore(playerName, total, confirmed.Count);

        if (resultHeader != null)
            resultHeader.text = playerName.ToUpper() + "  -  " + total + " PONTOS  -  "
                              + confirmed.Count + " REGISTROS";

        if (breakdownText != null) breakdownText.text = BuildBreakdown();
        if (rankingText != null) rankingText.text = BuildRanking();
    }

    // Resumo que explica o placar em vez de so mostrar o numero: diz onde o
    // jogador perdeu ponto.
    string BuildBreakdown()
    {
        if (confirmed.Count == 0)
            return "Nenhum registro concluido.\nAproxime-se, enquadre na camera de analise e pressione E.";

        var count = new Dictionary<string, int>();
        var pts = new Dictionary<string, int>();
        int typeMiss = 0, prioMiss = 0, excellent = 0, falses = 0;

        foreach (var r in confirmed)
        {
            if (!count.ContainsKey(r.type)) { count[r.type] = 0; pts[r.type] = 0; }
            count[r.type]++;
            pts[r.type] += r.points;
            if (r.points == penaltyFalseReport) { falses++; continue; }
            if (!r.typeOk) typeMiss++;
            if (!r.priorityOk) prioMiss++;
            if (r.quality == DefectScanner.Evidence.Excellent) excellent++;
        }

        var sb = new System.Text.StringBuilder();
        var keys = new List<string>(count.Keys);
        keys.Sort(delegate (string a, string b) { return pts[b].CompareTo(pts[a]); });
        foreach (string k in keys)
            sb.AppendLine(count[k].ToString().PadLeft(2) + "x  " + k + "   ....   " + pts[k] + " pts");

        sb.AppendLine();
        if (typeMiss > 0) sb.AppendLine(typeMiss + " classificacao(oes) de tipo erradas  (-" + pointsCategory + " cada)");
        if (prioMiss > 0) sb.AppendLine(prioMiss + " prioridade(s) erradas  (-" + pointsPriority + " cada)");
        if (excellent > 0) sb.AppendLine(excellent + " enquadramento(s) excelentes  (+" + pointsFraming + " cada)");
        if (falses > 0) sb.AppendLine(falses + " registro(s) falso(s)");
        if (spotted.Count > 0)
            sb.AppendLine("Voce viu " + spotted.Count
                          + " possivel(is) problema(s) e nao concluiu a captura da evidencia.");
        return sb.ToString();
    }

    void UpdateHud()
    {
        if (timerText != null)
        {
            int s = Mathf.CeilToInt(timeLeft);
            timerText.text = string.Format("{0:00}:{1:00}", s / 60, s % 60);
            timerText.color = timeLeft <= 10f
                ? Color.Lerp(new Color(1f, 0.3f, 0.25f), Color.white, Mathf.PingPong(Time.time * 3f, 1f))
                : Color.white;
        }
        if (scoreText != null) scoreText.text = Score + " PTS";
        if (countText != null) countText.text = confirmed.Count + " REGISTRADAS";
        if (hintText == null) return;

        bool classifying = step == Step.PickType || step == Step.PickPriority;
        SetClassificationPanelVisible(classifying);
        if (step == Step.PickType) { hintText.text = Menu("TIPO", Types); return; }
        if (step == Step.PickPriority) { hintText.text = SeverityMenu(); return; }

        DefectInfo cur = scanner != null ? scanner.Current : null;
        if (cur == null)
        {
            hintText.text = dispatchTarget != null ? "CENTRAL: reclamacao " + DispatchHint() : "";
            return;
        }

        // So conta como "viu e nao registrou" se chegou perto o bastante para
        // fotografar. Senao o relatorio acusaria como perdida toda ocorrencia
        // que passou no raio de varredura enquanto o jogador dirigia.
        if (Vector3.Distance(transform.position, cur.transform.position) <= registerRange)
            spotted.Add(cur.GetEntityId());

        string reason = RegisterBlockedReason(cur);
        if (reason != null) { hintText.text = reason + QualityTag(cur); return; }

        // No desafio a recompensa ainda nao esta decidida: depende da
        // classificacao que o jogador ainda vai dar.
        hintText.text = (challengeMode
            ? "[E] CAPTURAR EVIDENCIA"
            : "[E] REGISTRAR  (+" + pointsEvidence + ")") + QualityTag(cur);
    }

    // Indicador de qualidade da evidencia, sempre visivel: e a informacao que
    // deixa o jogador corrigir a posicao em vez de adivinhar por que falhou.
    string QualityTag(DefectInfo d)
    {
        if (scanner == null) return "";
        string why;
        switch (scanner.EvidenceQuality(d, out why))
        {
            case DefectScanner.Evidence.Blocked: return "   [ENCOBERTA]";
            case DefectScanner.Evidence.Poor: return "   [INSUFICIENTE]";
            case DefectScanner.Evidence.Excellent: return "   [EXCELENTE +" + pointsFraming + "]";
            default: return "   [ADEQUADA]";
        }
    }

    // ------------------------------------------------------------ central

    // Sorteia uma ocorrencia ainda nao registrada dentro de uma faixa de
    // distancia. A faixa importa: perto demais entrega a resposta, longe demais
    // gasta a rodada inteira em deslocamento.
    void NextDispatch()
    {
        dispatchTarget = null;
        if (!dispatchMode || scanner == null) return;

        var pool = new List<DefectInfo>();
        var wide = new List<DefectInfo>();
        Vector3 me = transform.position;

        foreach (var d in scanner.AllDefects)
        {
            if (d == null || seen.Contains(d.GetEntityId())) continue;
            float dist = Vector3.Distance(me, d.transform.position);
            if (dist >= dispatchMinRange && dist <= dispatchMaxRange) pool.Add(d);
            else wide.Add(d);
        }

        var from = pool.Count > 0 ? pool : wide;
        if (from.Count == 0) return;
        dispatchTarget = from[Random.Range(0, from.Count)];

        Toast("CENTRAL: reclamacao " + DispatchHint(), new Color(0.6f, 0.9f, 1f));
    }

    string DispatchHint()
    {
        if (dispatchTarget == null) return "";
        Vector3 delta = dispatchTarget.transform.position - transform.position;
        float dist = delta.magnitude;
        // Arredonda para dezena: a central passa uma regiao, nao coordenada.
        int approx = Mathf.RoundToInt(dist / 10f) * 10;
        return "a ~" + approx + " m para " + Compass(delta);
    }

    static string Compass(Vector3 delta)
    {
        float ang = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
        if (ang < 0f) ang += 360f;
        string[] names = { "NORTE", "NORDESTE", "LESTE", "SUDESTE", "SUL", "SUDOESTE", "OESTE", "NOROESTE" };
        return names[Mathf.RoundToInt(ang / 45f) % 8];
    }

    static string Menu(string title, string[] options)
    {
        var sb = new System.Text.StringBuilder(title);
        for (int i = 0; i < options.Length; i++)
        {
            sb.Append('\n').Append('[').Append(i + 1).Append("] ").Append(options[i]);
        }
        return sb.ToString();
    }

    static string SeverityMenu()
    {
        // Cores semanticas: quanto maior o risco, mais quente a cor.
        string[] colors = { "#FF4D4D", "#FF9F32", "#FFE04D", "#43A9FF" };
        var sb = new System.Text.StringBuilder("GRAVIDADE");
        for (int i = 0; i < Priorities.Length; i++)
        {
            sb.Append("\n<color=").Append(colors[i]).Append(">");
            sb.Append('[').Append(i + 1).Append("] ").Append(Priorities[i]);
            sb.Append("</color>");
        }
        return sb.ToString();
    }

    void Toast(string msg, Color c)
    {
        if (toastText == null) return;
        toastText.text = msg;
        toastText.color = c;
        toastTimer = 2.0f;
    }

    // ------------------------------------------------------------ ranking

    [System.Serializable]
    class Entry { public string name; public int score; public int found; }

    [System.Serializable]
    class Board { public List<Entry> entries = new List<Entry>(); }

    const string PrefKey = "fecap_inspecao_ranking";

    static Board LoadBoard()
    {
        string json = PlayerPrefs.GetString(PrefKey, "");
        if (string.IsNullOrEmpty(json)) return new Board();
        Board b = JsonUtility.FromJson<Board>(json);
        return b ?? new Board();
    }

    static void SaveScore(string name, int score, int found)
    {
        Board b = LoadBoard();
        b.entries.Add(new Entry { name = name, score = score, found = found });
        b.entries.Sort(delegate (Entry a, Entry c) { return c.score.CompareTo(a.score); });
        if (b.entries.Count > 20) b.entries.RemoveRange(20, b.entries.Count - 20);
        PlayerPrefs.SetString(PrefKey, JsonUtility.ToJson(b));
        PlayerPrefs.Save();
    }

    string BuildRanking()
    {
        Board b = LoadBoard();
        if (b.entries.Count == 0) return "Sem partidas registradas.";
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        int n = Mathf.Min(10, b.entries.Count);
        for (int i = 0; i < n; i++)
        {
            Entry e = b.entries[i];
            sb.AppendLine((i + 1).ToString().PadLeft(2) + ".  "
                + e.name.PadRight(17).Substring(0, 17)
                + e.score.ToString().PadLeft(5) + " pts   "
                + e.found + " oc.");
        }
        return sb.ToString();
    }
}
