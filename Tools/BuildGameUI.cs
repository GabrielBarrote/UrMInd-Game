using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

// Monta a UI da rodada de inspecao.
//
// uGUI legado de proposito: o projeto nao tem os essentials do TextMeshPro
// importados, e a fonte tem que ser LegacyRuntime.ttf (Arial.ttf lanca excecao).
// O EventSystem usa InputSystemUIInputModule porque o projeto esta em
// activeInputHandler=1 (Input System novo) e o StandaloneInputModule quebra.
public static class BuildGameUI
{
    static Font font;

    static Font F()
    {
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return font;
    }

    static readonly Color Ink = new Color(0.88f, 0.93f, 0.97f);
    static readonly Color Dim = new Color(0.62f, 0.70f, 0.78f);
    static readonly Color Green = new Color(0.16f, 0.72f, 0.52f);
    static readonly Color PanelBg = new Color(0.05f, 0.07f, 0.10f, 0.93f);

    static GameObject Node(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static RectTransform Rect(GameObject go, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.offsetMin = oMin; rt.offsetMax = oMax;
        return rt;
    }

    static Text Label(string name, Transform parent, string text, int size,
                      TextAnchor anchor, Color color,
                      Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
    {
        GameObject go = Node(name, parent);
        Rect(go, aMin, aMax, oMin, oMax);
        Text t = go.AddComponent<Text>();
        t.font = F();
        t.fontSize = size;
        t.text = text;
        t.alignment = anchor;
        t.color = color;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.supportRichText = false;
        return t;
    }

    static Image Panel(string name, Transform parent, Color c,
                       Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
    {
        GameObject go = Node(name, parent);
        Rect(go, aMin, aMax, oMin, oMax);
        Image img = go.AddComponent<Image>();
        img.color = c;
        return img;
    }

    static Button MakeButton(string name, Transform parent, string caption,
                             Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
    {
        Image bg = Panel(name, parent, Green, aMin, aMax, oMin, oMax);
        Button b = bg.gameObject.AddComponent<Button>();
        ColorBlock cb = b.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(0.80f, 1f, 0.92f);
        cb.pressedColor = new Color(0.60f, 0.85f, 0.75f);
        b.colors = cb;
        Label(name + "_Label", bg.transform, caption, 22, TextAnchor.MiddleCenter,
              new Color(0.03f, 0.10f, 0.08f),
              Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return b;
    }

    public static string Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return "ERRO: saia do Play mode primeiro";

        // --- EventSystem compativel com o Input System novo ---
        EventSystem es = Object.FindFirstObjectByType<EventSystem>();
        if (es == null)
        {
            GameObject esGo = new GameObject("EventSystem");
            es = esGo.AddComponent<EventSystem>();
        }
        foreach (BaseInputModule old in es.GetComponents<BaseInputModule>())
            if (!(old is InputSystemUIInputModule)) Object.DestroyImmediate(old);
        if (es.GetComponent<InputSystemUIInputModule>() == null)
            es.gameObject.AddComponent<InputSystemUIInputModule>();

        // --- Canvas proprio, acima do HUD de analise ---
        GameObject oldCanvas = GameObject.Find("GameUI");
        if (oldCanvas != null) Object.DestroyImmediate(oldCanvas);

        GameObject canvasGo = new GameObject("GameUI",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler cs = canvasGo.GetComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920f, 1080f);
        cs.matchWidthOrHeight = 0.5f;
        Transform C = canvasGo.transform;

        // ================================================= MENU
        GameObject menu = Node("MenuPanel", C);
        Rect(menu, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Panel("Scrim", menu.transform, new Color(0.02f, 0.04f, 0.06f, 0.88f),
              Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        Image card = Panel("Card", menu.transform, PanelBg,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-370f, -260f), new Vector2(370f, 260f));
        Panel("CardStripe", card.transform, Green,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -6f), new Vector2(0f, 0f));

        Label("Title", card.transform, "INSPECAO URBANA FECAP", 34, TextAnchor.UpperCenter, Ink,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -78f), new Vector2(0f, -30f));
        Label("Sub", card.transform,
            "Voce tem 60 segundos para identificar o maximo de ocorrencias.",
            19, TextAnchor.UpperCenter, Dim,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -112f), new Vector2(0f, -84f));

        Label("Rules", card.transform,
            "W A S D  dirigir        ESPACO  frear        E  registrar ocorrencia\n\n"
            + "PONTOS POR GRAVIDADE\n"
            + "CRITICA 100      ALTA 60      MEDIA 35      BAIXA 15",
            18, TextAnchor.UpperCenter, Dim,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -232f), new Vector2(0f, -132f));

        Label("NameLabel", card.transform, "NOME DE USUARIO", 17, TextAnchor.LowerLeft, Dim,
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(60f, 176f), new Vector2(-60f, 200f));

        Image fieldBg = Panel("NameField", card.transform, new Color(0.10f, 0.13f, 0.17f, 1f),
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(60f, 118f), new Vector2(-60f, 168f));
        InputField field = fieldBg.gameObject.AddComponent<InputField>();
        Text fieldText = Label("Text", fieldBg.transform, "", 22, TextAnchor.MiddleLeft, Ink,
            Vector2.zero, Vector2.one, new Vector2(14f, 2f), new Vector2(-14f, -2f));
        Text placeholder = Label("Placeholder", fieldBg.transform, "digite seu nome...", 22,
            TextAnchor.MiddleLeft, new Color(0.45f, 0.52f, 0.58f),
            Vector2.zero, Vector2.one, new Vector2(14f, 2f), new Vector2(-14f, -2f));
        field.textComponent = fieldText;
        field.placeholder = placeholder;
        field.characterLimit = 16;

        Text warn = Label("Warning", card.transform, "", 17, TextAnchor.UpperCenter,
            new Color(1f, 0.55f, 0.4f),
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 88f), new Vector2(0f, 112f));

        Button start = MakeButton("StartButton", card.transform, "INICIAR INSPECAO",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-170f, 28f), new Vector2(170f, 78f));

        // ================================================= HUD
        GameObject hud = Node("HudPanel", C);
        Rect(hud, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        Image bar = Panel("TopBar", hud.transform, new Color(0.04f, 0.06f, 0.09f, 0.82f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-300f, -84f), new Vector2(300f, -12f));
        Panel("BarStripe", bar.transform, Green,
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 4f));

        Text timer = Label("Timer", bar.transform, "01:00", 40, TextAnchor.MiddleCenter, Color.white,
            new Vector2(0f, 0f), new Vector2(0.34f, 1f), Vector2.zero, Vector2.zero);
        Text score = Label("Score", bar.transform, "0 PTS", 30, TextAnchor.MiddleCenter, Green,
            new Vector2(0.34f, 0f), new Vector2(0.67f, 1f), Vector2.zero, Vector2.zero);
        Text count = Label("Count", bar.transform, "0 REGISTRADAS", 19, TextAnchor.MiddleCenter, Dim,
            new Vector2(0.67f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

        Text hint = Label("Hint", hud.transform, "", 26, TextAnchor.MiddleCenter, Green,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-320f, 150f), new Vector2(320f, 190f));
        Text toast = Label("Toast", hud.transform, "", 24, TextAnchor.MiddleCenter, Ink,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-420f, 196f), new Vector2(420f, 234f));

        // ================================================= RESULTADO
        GameObject res = Node("ResultsPanel", C);
        Rect(res, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Panel("Scrim", res.transform, new Color(0.02f, 0.04f, 0.06f, 0.90f),
              Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        Image rcard = Panel("Card", res.transform, PanelBg,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-520f, -330f), new Vector2(520f, 330f));
        Panel("CardStripe", rcard.transform, Green,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -6f), new Vector2(0f, 0f));

        Label("Done", rcard.transform, "TEMPO ESGOTADO", 30, TextAnchor.UpperCenter, Dim,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -62f), new Vector2(0f, -24f));
        Text header = Label("Header", rcard.transform, "", 30, TextAnchor.UpperCenter, Green,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -104f), new Vector2(0f, -66f));

        Label("BreakTitle", rcard.transform, "OCORRENCIAS IDENTIFICADAS", 20,
            TextAnchor.UpperLeft, Ink,
            new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(48f, -156f), new Vector2(0f, -124f));
        Text breakdown = Label("Breakdown", rcard.transform, "", 19, TextAnchor.UpperLeft, Dim,
            new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(48f, 96f), new Vector2(-10f, -164f));

        Label("RankTitle", rcard.transform, "RANKING", 20, TextAnchor.UpperLeft, Ink,
            new Vector2(0.5f, 1f), new Vector2(1f, 1f), new Vector2(24f, -156f), new Vector2(-48f, -124f));
        Text ranking = Label("Ranking", rcard.transform, "", 19, TextAnchor.UpperLeft, Dim,
            new Vector2(0.5f, 0f), new Vector2(1f, 1f), new Vector2(24f, 96f), new Vector2(-48f, -164f));

        Button again = MakeButton("AgainButton", rcard.transform, "JOGAR NOVAMENTE",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-170f, 28f), new Vector2(170f, 78f));

        // ================================================= wiring
        CharacterController cc = Object.FindFirstObjectByType<CharacterController>();
        if (cc == null) return "ERRO: rover nao encontrado";

        InspectionGame game = cc.GetComponent<InspectionGame>();
        if (game == null) game = cc.gameObject.AddComponent<InspectionGame>();

        game.scanner = cc.GetComponent<DefectScanner>();
        game.cart = cc.GetComponent<AnalysisCartController>();
        game.menuPanel = menu;
        game.nameField = field;
        game.startButton = start;
        game.menuWarning = warn;
        game.hudPanel = hud;
        game.timerText = timer;
        game.scoreText = score;
        game.countText = count;
        game.hintText = hint;
        game.toastText = toast;
        game.resultsPanel = res;
        game.resultHeader = header;
        game.breakdownText = breakdown;
        game.rankingText = ranking;
        game.againButton = again;

        EditorUtility.SetDirty(game);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        return "UI montada. EventSystem=" + es.GetComponent<InputSystemUIInputModule>().GetType().Name
             + ", InspectionGame ligado em '" + cc.name + "', scanner="
             + (game.scanner != null) + ", cart=" + (game.cart != null);
    }
}
