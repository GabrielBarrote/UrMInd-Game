using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;

// Constroi o predio da FECAP no centro do mapa, a partir das fotos de
// referencia: bloco baixo de 2 pavimentos com faixa branca superior, faixa de
// tijolo aparente, pilastras brancas, vitrine de vidro e trecho de bloco de
// vidro, entrada recuada com marquise, letreiro FECAP em relevo, mastros de
// bandeira e torre branca ao fundo.
//
// IMPLANTACAO: fachada principal em z=+10 voltada para a via em z=0.
//   X de -17 a +17 (34m)   Z de 7 a 24 (17m)   bloco baixo ate y=13
// Limites do quarteirao medidos na cena: vias transversais em x=+-24,
// calcadas em x=+-19.5 e z=4.5/25. Fora disso o predio invade a rua.
public static class FecapBuild
{
    const string MatDir = "Assets/Fecap/Materials";
    const string TexDir = "Assets/Fecap/Textures";
    const string RootName = "FECAP";

    // --- implantacao ---
    const float FrontZ = 7f;       // plano da fachada
    const float BackZ = 24f;
    const float HalfW = 17f;   // quarteirao vai ate calcada em x=+-19.5
    const float PlinthTop = 0.45f;
    const float GlazTop = 4.60f;   // topo da vitrine
    const float BrickTop = 9.60f;  // topo da faixa de tijolo
    const float BandTop = 12.60f;  // topo da faixa branca
    const float CapTop = 13.00f;

    static Transform root;
    static Dictionary<string, Material> M = new Dictionary<string, Material>();

    // ------------------------------------------------------------ helpers

    static Material Mat(string name, Color color, float smooth, float metal,
                        string albedo, string normal, Vector2 tiling, bool transparent)
    {
        Directory.CreateDirectory(MatDir);
        string path = MatDir + "/" + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
        m.shader = sh;
        m.SetColor("_BaseColor", color);
        m.SetFloat("_Smoothness", smooth);
        m.SetFloat("_Metallic", metal);
        m.enableInstancing = true;

        if (albedo != null)
        {
            Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "/" + albedo);
            if (t != null) m.SetTexture("_BaseMap", t);
        }
        if (normal != null)
        {
            Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "/" + normal);
            if (t != null)
            {
                m.SetTexture("_BumpMap", t);
                m.EnableKeyword("_NORMALMAP");
            }
        }
        m.SetTextureScale("_BaseMap", tiling);

        if (transparent)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.renderQueue = 3000;
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_ALPHATEST_ON");
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    static void BuildMaterials()
    {
        M["white"] = Mat("Fec_White", new Color(0.93f, 0.92f, 0.89f), 0.15f, 0f, null, null, Vector2.one, false);
        M["brick"] = Mat("Fec_Brick", Color.white, 0.10f, 0f, "brick_albedo.png", "brick_normal.png", Vector2.one, false);
        M["glassblock"] = Mat("Fec_GlassBlock", Color.white, 0.55f, 0f, "glassblock_albedo.png", "glassblock_normal.png", Vector2.one, false);
        M["glass"] = Mat("Fec_Glass", new Color(0.32f, 0.45f, 0.55f, 0.62f), 0.92f, 0.1f, null, null, Vector2.one, true);
        M["dark"] = Mat("Fec_DarkFrame", new Color(0.13f, 0.14f, 0.16f), 0.45f, 0.35f, null, null, Vector2.one, false);
        M["green"] = Mat("Fec_Green", new Color(0.02f, 0.36f, 0.24f), 0.30f, 0f, null, null, Vector2.one, false);
        M["plinth"] = Mat("Fec_Plinth", new Color(0.30f, 0.30f, 0.31f), 0.12f, 0f, null, null, Vector2.one, false);
        M["hedge"] = Mat("Fec_Hedge", new Color(0.10f, 0.32f, 0.12f), 0.08f, 0f, null, null, Vector2.one, false);
        M["yellow"] = Mat("Fec_Yellow", new Color(0.93f, 0.74f, 0.10f), 0.35f, 0f, null, null, Vector2.one, false);
        M["metal"] = Mat("Fec_Metal", new Color(0.62f, 0.64f, 0.66f), 0.60f, 0.8f, null, null, Vector2.one, false);
        M["concrete"] = Mat("Fec_Concrete", new Color(0.82f, 0.81f, 0.78f), 0.10f, 0f, null, null, Vector2.one, false);
        M["logo"] = Mat("Fec_Logo", Color.white, 0.20f, 0f, "fecap_logo.jpg", null, Vector2.one, false);
        M["flagBR"] = Mat("Fec_FlagBR", Color.white, 0.25f, 0f, "flag_br.png", null, Vector2.one, false);
        M["flagSP"] = Mat("Fec_FlagSP", Color.white, 0.25f, 0f, "flag_sp.png", null, Vector2.one, false);
        M["flagFECAP"] = Mat("Fec_FlagFECAP", Color.white, 0.25f, 0f, "flag_fecap.png", null, Vector2.one, false);
        AssetDatabase.SaveAssets();
    }

    static GameObject Prim(PrimitiveType t, string name, Transform parent,
                           Vector3 pos, Vector3 euler, Vector3 scale,
                           Material mat, bool collide)
    {
        GameObject go = GameObject.CreatePrimitive(t);
        go.name = name;
        Collider c = go.GetComponent<Collider>();
        if (c != null && !collide) Object.DestroyImmediate(c);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localEulerAngles = euler;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    // Caixa definida por cantos no mundo, que e como o predio foi projetado.
    static GameObject Box(string name, Transform parent, float x0, float x1,
                          float y0, float y1, float z0, float z1,
                          Material mat, bool collide)
    {
        return Prim(PrimitiveType.Cube, name, parent,
            new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, (z0 + z1) * 0.5f),
            Vector3.zero,
            new Vector3(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0), Mathf.Abs(z1 - z0)),
            mat, collide);
    }

    // Tiling POR OBJETO. O tiling do material vale para todos que o usam, e a
    // textura de 1m esticaria pelos 56m da fachada. MaterialPropertyBlock
    // resolve sem precisar de um material por tamanho de pano.
    static void Tile(GameObject go, float metersX, float metersY)
    {
        Renderer r = go.GetComponent<Renderer>();
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        r.GetPropertyBlock(mpb);
        mpb.SetVector("_BaseMap_ST", new Vector4(metersX, metersY, 0f, 0f));
        r.SetPropertyBlock(mpb);
    }

    static Transform Group(string name, Transform parent)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent, false);
        return g.transform;
    }

    // ------------------------------------------------------------- build

    public static string Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return "ERRO: saia do Play mode primeiro";

        StringBuilder sb = new StringBuilder();
        BuildMaterials();

        GameObject oldRoot = GameObject.Find(RootName);
        if (oldRoot != null) Object.DestroyImmediate(oldRoot);
        GameObject r = new GameObject(RootName);
        root = r.transform;
        root.position = Vector3.zero;

        int cleared = ClearPlot();
        sb.AppendLine("objetos desativados no terreno: " + cleared);

        Structure();
        Pilasters();
        GroundFloor();
        Signage();
        Flags();
        Tower();
        SideFacades();
        Sidewalk();
        RoofGear();

        int renderers = root.GetComponentsInChildren<Renderer>(true).Length;
        sb.AppendLine("FECAP construido: " + renderers + " renderers, "
            + root.GetComponentsInChildren<Collider>(true).Length + " colliders");

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        return sb.ToString();
    }

    // Desativa (nao apaga) o que ocupa o terreno, para ser reversivel.
    static int ClearPlot()
    {
        GameObject world = GameObject.Find("City 02/World");
        int n = 0;
        string[] groups = new string[] { "Buildings", "Vehicles", "Cones", "Boxes", "Trees" };
        foreach (string gn in groups)
        {
            Transform g = world.transform.Find(gn);
            if (g == null) continue;
            List<Transform> hit = new List<Transform>();
            foreach (Transform child in g)
            {
                Vector3 p = child.position;
                if (p.x > -30f && p.x < 30f && p.z > 8.5f && p.z < 36f) hit.Add(child);
            }
            foreach (Transform t in hit) { t.gameObject.SetActive(false); n++; }
        }
        return n;
    }

    // --------------------------------------------------- volume principal

    static void Structure()
    {
        Transform g = Group("Structure", root);

        // embasamento escuro
        Box("Plinth", g, -HalfW - 0.3f, HalfW + 0.3f, 0f, PlinthTop, FrontZ - 0.35f, BackZ, M["plinth"], true);

        // Faixa de tijolo dividida por vao entre pilastras: cada pano recebe
        // tiling de 1 textura por metro, senao o tijolo estica pela fachada.
        float bh = BrickTop - GlazTop;
        for (float bx = -HalfW; bx < HalfW - 0.01f; bx += 6.8f)
        {
            GameObject panel = Box("BrickBay_" + bx.ToString("F0"), g, bx, bx + 6.8f,
                GlazTop, BrickTop, FrontZ - 0.15f, FrontZ, M["brick"], true);
            Tile(panel, 6.8f, bh);
        }
        // faixa branca superior
        Box("WhiteBand_Front", g, -HalfW, HalfW, BrickTop, BandTop, FrontZ - 0.30f, FrontZ, M["white"], true);
        // coroamento
        Box("Cap_Front", g, -HalfW - 0.25f, HalfW + 0.25f, BandTop, CapTop, FrontZ - 0.45f, FrontZ + 0.15f, M["white"], false);

        // laterais e fundo em massa branca
        Box("Side_L", g, -HalfW - 0.15f, -HalfW, PlinthTop, BandTop, FrontZ - 0.15f, BackZ, M["white"], true);
        Box("Side_R", g, HalfW, HalfW + 0.15f, PlinthTop, BandTop, FrontZ - 0.15f, BackZ, M["white"], true);
        Box("Back", g, -HalfW, HalfW, PlinthTop, BandTop, BackZ - 0.2f, BackZ, M["white"], true);
        // massa interna, para o predio nao ser oco visto de cima
        Box("Bulk", g, -HalfW, HalfW, PlinthTop, BandTop - 0.05f, FrontZ + 0.2f, BackZ - 0.2f, M["concrete"], true);
        // laje de cobertura
        Box("Roof", g, -HalfW - 0.25f, HalfW + 0.25f, BandTop, BandTop + 0.25f, FrontZ - 0.2f, BackZ, M["concrete"], false);
    }

    // Pilastras brancas a cada 8m, como nas fotos.
    static void Pilasters()
    {
        Transform g = Group("Pilasters", root);
        for (float x = -HalfW; x <= HalfW + 0.01f; x += 6.8f)
        {
            Box("Pilaster_" + x.ToString("F0"), g, x - 0.6f, x + 0.6f,
                0f, BandTop + 0.05f, FrontZ - 0.62f, FrontZ, M["white"], true);
            // sombra/recorte na base da pilastra
            Box("PilasterBase_" + x.ToString("F0"), g, x - 0.72f, x + 0.72f,
                0f, 0.35f, FrontZ - 0.72f, FrontZ, M["plinth"], false);
        }
    }

    // ----------------------------------------- terreo: vitrine e entrada

    static void GroundFloor()
    {
        Transform g = Group("GroundFloor", root);
        float z = FrontZ - 0.35f;          // plano do envidracamento, recuado

        // Trecho esquerdo: vitrine de vidro com montantes (foto 1)
        Glazing(g, -HalfW + 0.6f, -3.4f, z, 1.7f);
        // Trecho direito: bloco de vidro (foto 2)
        GameObject gb = Box("GlassBlockWall", g, 3.8f, HalfW - 0.6f, PlinthTop, GlazTop - 0.35f, z - 0.06f, z, M["glassblock"], true);
        Tile(gb, (HalfW - 0.6f) - 3.8f, (GlazTop - 0.35f) - PlinthTop);
        Box("GlassBlockSill", g, 3.6f, HalfW - 0.4f, GlazTop - 0.35f, GlazTop - 0.2f, z - 0.2f, z + 0.05f, M["white"], false);
        Box("GlassBlockBase", g, 3.6f, HalfW - 0.4f, PlinthTop, PlinthTop + 0.5f, z - 0.2f, z + 0.05f, M["white"], false);

        Entrance(g, z);
        Hedge(g);
    }

    // Vitrine: vidro continuo + montantes verticais escuros + travessas
    static void Glazing(Transform g, float x0, float x1, float z, float step)
    {
        Box("Glass_Panel", g, x0, x1, PlinthTop + 0.1f, GlazTop - 0.2f, z - 0.05f, z, M["glass"], false);
        Box("Glass_Head", g, x0 - 0.1f, x1 + 0.1f, GlazTop - 0.2f, GlazTop, z - 0.15f, z + 0.05f, M["dark"], false);
        Box("Glass_Sill", g, x0 - 0.1f, x1 + 0.1f, PlinthTop, PlinthTop + 0.1f, z - 0.15f, z + 0.05f, M["dark"], false);
        for (float x = x0; x <= x1 + 0.01f; x += step)
            Box("Mullion", g, x - 0.05f, x + 0.05f, PlinthTop, GlazTop, z - 0.12f, z + 0.02f, M["dark"], false);
        // travessa horizontal a meia altura
        Box("Transom", g, x0, x1, 2.4f, 2.52f, z - 0.12f, z + 0.02f, M["dark"], false);
    }

    // Entrada recuada com marquise, portas de vidro e placa
    static void Entrance(Transform g, float z)
    {
        Transform e = Group("Entrance", g);
        float zi = z - 1.2f;   // recuo da entrada

        Box("Reveal_L", e, -3.8f, -3.4f, PlinthTop, GlazTop, zi, z, M["white"], true);
        Box("Reveal_R", e, 3.4f, 3.8f, PlinthTop, GlazTop, zi, z, M["white"], true);
        Box("Reveal_Top", e, -3.8f, 3.8f, GlazTop - 0.4f, GlazTop, zi, z, M["dark"], false);

        // parede recuada de vidro com portas
        Box("Entry_Glass", e, -3.4f, 3.4f, PlinthTop, GlazTop - 0.4f, zi - 0.05f, zi, M["glass"], false);
        for (float x = -3.4f; x <= 3.4f; x += 1.7f)
            Box("Entry_Mullion", e, x - 0.06f, x + 0.06f, PlinthTop, GlazTop - 0.4f, zi - 0.1f, zi + 0.02f, M["dark"], false);

        // portas duplas no centro do vao
        Box("Door_L", e, -1.30f, -0.05f, PlinthTop, PlinthTop + 2.6f, zi - 0.12f, zi - 0.02f, M["dark"], false);
        Box("Door_R", e, 0.05f, 1.30f, PlinthTop, PlinthTop + 2.6f, zi - 0.12f, zi - 0.02f, M["dark"], false);
        Prim(PrimitiveType.Cylinder, "Handle_L", e, new Vector3(-0.22f, 1.55f, zi - 0.18f),
             new Vector3(90f, 0f, 0f), new Vector3(0.05f, 0.45f, 0.05f), M["metal"], false);
        Prim(PrimitiveType.Cylinder, "Handle_R", e, new Vector3(0.22f, 1.55f, zi - 0.18f),
             new Vector3(90f, 0f, 0f), new Vector3(0.05f, 0.45f, 0.05f), M["metal"], false);

        // marquise sobre a entrada
        Box("Canopy", e, -4.4f, 4.4f, GlazTop, GlazTop + 0.3f, zi - 1.05f, z, M["dark"], false);
        for (float x = -3.2f; x <= 3.2f; x += 3.2f)
            Prim(PrimitiveType.Cylinder, "CanopyRod", e,
                 new Vector3(x, GlazTop + 0.9f, zi - 0.75f), new Vector3(28f, 0f, 0f),
                 new Vector3(0.06f, 0.95f, 0.06f), M["metal"], false);

        // placa com o brasao ao lado da porta
        // Placa foi para a fachada ao lado da entrada: o vao ficou estreito
        // demais para caber placa e portas no mesmo pano.
        Box("PlaquePanel", e, -6.6f, -4.5f, 1.2f, 3.3f, FrontZ - 0.40f, FrontZ - 0.36f, M["white"], false);
        Prim(PrimitiveType.Cube, "PlaqueLogo", e, new Vector3(-5.55f, 2.25f, FrontZ - 0.44f),
             Vector3.zero, new Vector3(1.7f, 1.7f, 0.04f), M["logo"], false);
    }

    // Jardim vertical / cerca viva, presente na foto 1
    static void Hedge(Transform g)
    {
        Transform h = Group("Hedge", g);
        Box("HedgeBed", h, 3.9f, 16.4f, 0f, 0.55f, FrontZ - 2.2f, FrontZ - 1.5f, M["white"], true);
        for (float x = 4.0f; x < 16.3f; x += 1.1f)
        {
            float rnd = Mathf.PerlinNoise(x * 0.7f, 3.1f);
            Box("HedgeBush", h, x, x + 1.05f, 0.5f, 1.35f + rnd * 0.25f,
                FrontZ - 2.25f, FrontZ - 1.45f, M["hedge"], false);
        }
    }

    // --------------------------------------------- letreiro FECAP em relevo

    static void Signage()
    {
        Transform g = Group("Signage", root);
        float y = (BrickTop + BandTop) * 0.5f;     // centro da faixa branca
        float z = FrontZ - 0.30f;                  // face da faixa branca
        // O letreiro precisa caber ENTRE as pilastras de x=-3.4 e x=+3.4: elas
        // avancam 0.62m e cobririam o F e o P se o letreiro fosse mais largo.
        float H = 1.55f, T = 0.30f, D = 0.30f, W = 0.94f, gap = 0.22f;

        string word = "FECAP";
        float total = word.Length * W + (word.Length - 1) * gap;
        float x = -total * 0.5f;

        for (int i = 0; i < word.Length; i++)
        {
            Letter(g, word[i], x, y - H * 0.5f, z, W, H, T, D);
            x += W + gap;
        }
    }

    // Letras em blocos, na proporcao do letreiro das fotos.
    static void Letter(Transform g, char ch, float x, float y, float z,
                       float W, float H, float T, float D)
    {
        Transform L = Group("L_" + ch, g);
        Material m = M["green"];
        float zf = z - D, zb = z;

        switch (ch)
        {
            case 'F':
                Box("stem", L, x, x + T, y, y + H, zf, zb, m, false);
                Box("top", L, x, x + W, y + H - T, y + H, zf, zb, m, false);
                Box("mid", L, x, x + W * 0.82f, y + H * 0.46f, y + H * 0.46f + T, zf, zb, m, false);
                break;
            case 'E':
                Box("stem", L, x, x + T, y, y + H, zf, zb, m, false);
                Box("top", L, x, x + W, y + H - T, y + H, zf, zb, m, false);
                Box("mid", L, x, x + W * 0.82f, y + H * 0.46f, y + H * 0.46f + T, zf, zb, m, false);
                Box("bot", L, x, x + W, y, y + T, zf, zb, m, false);
                break;
            case 'C':
                Box("stem", L, x, x + T, y, y + H, zf, zb, m, false);
                Box("top", L, x, x + W, y + H - T, y + H, zf, zb, m, false);
                Box("bot", L, x, x + W, y, y + T, zf, zb, m, false);
                break;
            case 'A':
                Box("stem", L, x, x + T, y, y + H, zf, zb, m, false);
                Box("stemR", L, x + W - T, x + W, y, y + H, zf, zb, m, false);
                Box("top", L, x, x + W, y + H - T, y + H, zf, zb, m, false);
                Box("mid", L, x, x + W, y + H * 0.42f, y + H * 0.42f + T, zf, zb, m, false);
                break;
            case 'P':
                Box("stem", L, x, x + T, y, y + H, zf, zb, m, false);
                Box("top", L, x, x + W, y + H - T, y + H, zf, zb, m, false);
                Box("stemR", L, x + W - T, x + W, y + H * 0.42f, y + H, zf, zb, m, false);
                Box("mid", L, x, x + W, y + H * 0.42f, y + H * 0.42f + T, zf, zb, m, false);
                break;
        }
    }

    // ------------------------------------------------------- mastros

    static void Flags()
    {
        Transform g = Group("Flags", root);
        float[] xs = new float[] { -11.6f, -10.0f, -8.4f };
        string[] mats = new string[] { "flagSP", "flagBR", "flagFECAP" };
        for (int i = 0; i < 3; i++)
        {
            float x = xs[i];
            Transform p = Group("Pole_" + i, g);
            Prim(PrimitiveType.Cylinder, "Pole", p,
                 new Vector3(x, 4.2f, FrontZ - 1.0f), new Vector3(-14f, 0f, 0f),
                 new Vector3(0.11f, 4.2f, 0.11f), M["metal"], false);
            Prim(PrimitiveType.Sphere, "Finial", p,
                 new Vector3(x, 8.3f, FrontZ - 3.05f), Vector3.zero,
                 new Vector3(0.22f, 0.22f, 0.22f), M["yellow"], false);

            Material fm = M.ContainsKey(mats[i]) ? M[mats[i]] : M["green"];
            // placa fina em vez de Quad: Quad tem face unica e ficava de costas
            Prim(PrimitiveType.Cube, "Flag", p,
                 new Vector3(x + 0.95f, 7.0f, FrontZ - 2.55f), new Vector3(0f, 0f, -6f),
                 new Vector3(1.9f, 1.3f, 0.04f), fm, false);
        }
    }

    // --------------------------------------------------- torre ao fundo

    static void Tower()
    {
        Transform g = Group("Tower", root);
        float x0 = -7f, x1 = 7f, z0 = 15f, z1 = 23.6f;
        float top = 24f;

        Box("TowerBody", g, x0, x1, 0f, top, z0, z1, M["concrete"], true);
        Box("TowerCap", g, x0 - 0.4f, x1 + 0.4f, top, top + 0.5f, z0 - 0.4f, z1 + 0.4f, M["white"], false);

        // faixas de janela por pavimento, nas 3 faces visiveis
        for (int f = 1; f <= 7; f++)
        {
            float y = 1.2f + f * 3.4f;
            if (y + 1.5f > top) break;
            // fachada sul (para a rua)
            for (float x = x0 + 1.6f; x < x1 - 1.2f; x += 3.1f)
            {
                Box("WF_S", g, x - 0.12f, x + 2.02f, y - 0.12f, y + 1.62f, z0 - 0.10f, z0, M["dark"], false);
                Box("W_S", g, x, x + 1.9f, y, y + 1.5f, z0 - 0.16f, z0 - 0.10f, M["glass"], false);
            }
            // fachada norte (fundo)
            for (float x = x0 + 1.6f; x < x1 - 1.2f; x += 3.1f)
            {
                Box("WF_N", g, x - 0.12f, x + 2.02f, y - 0.12f, y + 1.62f, z1, z1 + 0.10f, M["dark"], false);
                Box("W_N", g, x, x + 1.9f, y, y + 1.5f, z1 + 0.10f, z1 + 0.16f, M["glass"], false);
            }
            // fachada oeste
            for (float z = z0 + 1.8f; z < z1 - 1.2f; z += 3.1f)
            {
                Box("WF_W", g, x0 - 0.10f, x0, y - 0.12f, y + 1.62f, z - 0.12f, z + 2.02f, M["dark"], false);
                Box("W_W", g, x0 - 0.16f, x0 - 0.10f, y, y + 1.5f, z, z + 1.9f, M["glass"], false);
            }
            // fachada leste
            for (float z = z0 + 1.8f; z < z1 - 1.2f; z += 3.1f)
            {
                Box("WF_E", g, x1, x1 + 0.10f, y - 0.12f, y + 1.62f, z - 0.12f, z + 2.02f, M["dark"], false);
                Box("W_E", g, x1 + 0.10f, x1 + 0.16f, y, y + 1.5f, z, z + 1.9f, M["glass"], false);
            }
        }

        // casa de maquinas / caixa d'agua no topo
        Box("Penthouse", g, x0 + 3f, x0 + 9f, top + 0.5f, top + 3.0f, z0 + 2f, z0 + 6f, M["concrete"], false);
        Box("PenthouseTop", g, x0 + 2.7f, x0 + 9.3f, top + 3.0f, top + 3.3f, z0 + 1.7f, z0 + 6.3f, M["white"], false);
        for (int i = 0; i < 3; i++)
            Prim(PrimitiveType.Cylinder, "Mast", g,
                 new Vector3(x0 + 4f + i * 2f, top + 4.4f, z0 + 4f), Vector3.zero,
                 new Vector3(0.08f, 1.3f, 0.08f), M["metal"], false);
    }


    // ---------------------------------------------- laterais e fundo
    // As fotos so mostram a frente. As outras faces seguem a mesma linguagem
    // (embasamento, tijolo, faixa branca, pilastras) para o predio fechar por
    // todos os lados, com programa de servico no fundo.
    static void SideFacades()
    {
        Transform g = Group("SideFacades", root);
        float depth = BackZ - FrontZ;
        float bays = 3f;
        float step = depth / bays;
        float bh = BrickTop - GlazTop;

        foreach (int sx in new int[] { -1, 1 })
        {
            float face = (HalfW + 0.15f) * sx;          // face externa da empena
            float outer = face + 0.14f * sx;
            string side = sx < 0 ? "W" : "E";

            for (int i = 0; i < bays; i++)
            {
                float z0 = FrontZ + i * step;
                float z1 = z0 + step;
                GameObject panel = Box("BrickBay_" + side + i, g,
                    Mathf.Min(face, outer), Mathf.Max(face, outer),
                    GlazTop, BrickTop, z0, z1, M["brick"], false);
                Tile(panel, step, bh);

                // Janela do terreo. A moldura avanca 0.10 e o vidro ocupa a
                // faixa de 0.10 a 0.16: sem isso as duas caixas dividem a mesma
                // profundidade e brigam por z (z-fighting).
                float wz0 = z0 + step * 0.22f, wz1 = z1 - step * 0.22f;
                float fA = face, fB = face + 0.10f * sx;
                float gA = face + 0.10f * sx, gB = face + 0.16f * sx;
                Box("WinFrame_" + side + i, g,
                    Mathf.Min(fA, fB), Mathf.Max(fA, fB),
                    1.30f, 3.70f, wz0 - 0.14f, wz1 + 0.14f, M["dark"], false);
                Box("Win_" + side + i, g,
                    Mathf.Min(gA, gB), Mathf.Max(gA, gB),
                    1.45f, 3.55f, wz0, wz1, M["glass"], false);
            }

            Box("WhiteBand_" + side, g,
                Mathf.Min(face, outer), Mathf.Max(face, outer),
                BrickTop, BandTop, FrontZ, BackZ, M["white"], false);
            Box("Cap_" + side, g,
                Mathf.Min(face, outer + 0.22f * sx), Mathf.Max(face, outer + 0.22f * sx),
                BandTop, CapTop, FrontZ - 0.45f, BackZ, M["white"], false);
            Box("Plinth_" + side, g,
                Mathf.Min(face, outer + 0.10f * sx), Mathf.Max(face, outer + 0.10f * sx),
                0f, PlinthTop, FrontZ, BackZ, M["plinth"], false);

            for (int i = 0; i <= bays; i++)
            {
                float z = FrontZ + i * step;
                Box("Pilaster_" + side + i, g,
                    Mathf.Min(face, outer + 0.42f * sx), Mathf.Max(face, outer + 0.42f * sx),
                    0f, BandTop + 0.05f, z - 0.55f, z + 0.55f, M["white"], false);
            }
        }

        BackFacade(g, bh);
    }

    // Fundo: area de servico, com doca de carga e equipamentos.
    static void BackFacade(Transform g, float bh)
    {
        float face = BackZ;
        float outer = BackZ + 0.14f;

        for (float bx = -HalfW; bx < HalfW - 0.01f; bx += 6.8f)
        {
            GameObject panel = Box("BrickBay_N_" + bx.ToString("F0"), g,
                bx, bx + 6.8f, GlazTop, BrickTop, face, outer, M["brick"], false);
            Tile(panel, 6.8f, bh);
        }
        Box("WhiteBand_N", g, -HalfW, HalfW, BrickTop, BandTop, face, outer, M["white"], false);
        Box("Cap_N", g, -HalfW - 0.25f, HalfW + 0.25f, BandTop, CapTop, face, outer + 0.22f, M["white"], false);
        Box("Plinth_N", g, -HalfW, HalfW, 0f, PlinthTop, face, outer + 0.10f, M["plinth"], false);

        // doca de carga recuada
        Box("DockRecess", g, -5.5f, 5.5f, 0f, 4.2f, face - 0.1f, outer + 0.05f, M["dark"], false);
        Box("DockFloor", g, -5.5f, 5.5f, 1.05f, 1.25f, outer, outer + 0.72f, M["concrete"], false);
        for (float x = -4.2f; x <= 4.2f; x += 2.8f)
            Box("DockDoor", g, x - 1.1f, x + 1.1f, 1.25f, 3.9f, outer + 0.02f, outer + 0.08f, M["metal"], false);

        // grupo gerador e caixas tecnicas no patio
        Box("Genset", g, 8.5f, 13.5f, 0f, 2.3f, outer + 0.05f, outer + 0.72f, M["metal"], false);
        Box("GensetVent", g, 9.0f, 13.0f, 2.3f, 2.45f, outer + 0.10f, outer + 0.67f, M["dark"], false);
        Box("Gas", g, -13.5f, -9.0f, 0f, 1.8f, outer + 0.05f, outer + 0.65f, M["yellow"], false);

        for (int i = 0; i <= 3; i++)
        {
            float x = -HalfW + i * (HalfW * 2f / 3f);
            Box("Pilaster_N" + i, g, x - 0.55f, x + 0.55f, 0f, BandTop + 0.05f,
                face, outer + 0.42f, M["white"], false);
        }
    }

    // ---------------------------------------------- calcada e mobiliario

    static void Sidewalk()
    {
        Transform g = Group("Sidewalk", root);
        // faixa de calcada em frente ao predio
        Box("Walk", g, -HalfW - 2f, HalfW + 2f, 0.02f, 0.14f, 4.2f, FrontZ - 0.35f, M["concrete"], false);
        Box("Curb", g, -HalfW - 2f, HalfW + 2f, 0f, 0.18f, 4.0f, 4.3f, M["plinth"], false);

        // balizadores amarelos, como na foto 2
        for (float x = -15f; x <= 15f; x += 4.3f)
        {
            Transform b = Group("Bollard", g);
            Prim(PrimitiveType.Cylinder, "L", b, new Vector3(x - 0.35f, 0.45f, 5.6f),
                 Vector3.zero, new Vector3(0.09f, 0.45f, 0.09f), M["yellow"], false);
            Prim(PrimitiveType.Cylinder, "R", b, new Vector3(x + 0.35f, 0.45f, 5.6f),
                 Vector3.zero, new Vector3(0.09f, 0.45f, 0.09f), M["yellow"], false);
            Prim(PrimitiveType.Cylinder, "Arc", b, new Vector3(x, 0.88f, 5.6f),
                 new Vector3(0f, 0f, 90f), new Vector3(0.09f, 0.38f, 0.09f), M["yellow"], false);
        }

        // gradil metalico junto ao meio-fio (foto 1)
        for (float x = -HalfW - 2f; x <= HalfW + 2f; x += 2.2f)
        {
            Prim(PrimitiveType.Cube, "RailPost", g, new Vector3(x, 0.62f, 4.15f),
                 Vector3.zero, new Vector3(0.07f, 1.1f, 0.07f), M["metal"], false);
        }
        Box("RailTop", g, -HalfW - 2f, HalfW + 2f, 1.1f, 1.18f, 4.11f, 4.19f, M["metal"], false);
        Box("RailMid", g, -HalfW - 2f, HalfW + 2f, 0.55f, 0.61f, 4.12f, 4.18f, M["metal"], false);
    }

    // Equipamentos de cobertura do bloco baixo
    static void RoofGear()
    {
        Transform g = Group("RoofGear", root);
        for (int i = 0; i < 5; i++)
        {
            float x = -13f + i * 6f;
            Box("AC_" + i, g, x, x + 2.0f, BandTop + 0.25f, BandTop + 1.05f, 11f, 13f, M["metal"], false);
            Box("ACfan_" + i, g, x + 0.3f, x + 1.7f, BandTop + 1.05f, BandTop + 1.15f, 11.3f, 12.7f, M["dark"], false);
        }
        Box("Duct", g, -14f, 14f, BandTop + 0.25f, BandTop + 0.70f, 14f, 14.7f, M["metal"], false);
        // luminarias de fachada apontando para a faixa branca
        for (float x = -15f; x <= 15f; x += 6.8f)
            Prim(PrimitiveType.Cube, "Wallwasher", g, new Vector3(x, BandTop + 0.35f, FrontZ - 0.75f),
                 new Vector3(35f, 0f, 0f), new Vector3(0.45f, 0.18f, 0.35f), M["dark"], false);
    }
}
