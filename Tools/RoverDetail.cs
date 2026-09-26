using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor;

// Acrescenta detalhe geometrico ao rover SEM criar material, cor ou textura
// nova: cada peca reusa a instancia de material de uma peca existente.
//
// Todas as medidas derivam das pecas atuais, medidas antes:
//   Base    (0,0.28,0)  escala (1.15,0.18,1.75) -> X +-0.575  Y 0.19..0.37  Z +-0.875
//   Chassis (0,0.45,0)  escala (1.00,0.35,1.60) -> X +-0.500  Y 0.275..0.625 Z +-0.800
//   Rodas   (+-0.58,0.28,+-0.55) raio 0.16, semi-largura 0.12
public static class RoverDetail
{
    const string GroupName = "Detail";
    const uint LayerVehicle = 1u << 1;   // decals nao pintam este layer

    static Transform player;
    static Material matDark, matBody, matGrey, matLens;
    static Transform group;

    // ------------------------------------------------------------- helpers

    static Transform Find(string n)
    {
        foreach (Transform t in player.GetComponentsInChildren<Transform>(true))
            if (t.name == n) return t;
        return null;
    }

    static Material MatOf(string childName)
    {
        Transform t = Find(childName);
        if (t == null) return null;
        Renderer r = t.GetComponent<Renderer>();
        return r == null ? null : r.sharedMaterial;
    }

    // Cria uma primitiva sem collider (CreatePrimitive traz um por padrao, e
    // colliders extras no rover conflitariam com o CharacterController).
    static GameObject Prim(PrimitiveType type, string name, Transform parent,
                           Vector3 pos, Vector3 euler, Vector3 scale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        Collider c = go.GetComponent<Collider>();
        if (c != null) Object.DestroyImmediate(c);

        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localEulerAngles = euler;
        go.transform.localScale = scale;

        Renderer r = go.GetComponent<Renderer>();
        r.sharedMaterial = mat;
        r.renderingLayerMask = LayerVehicle;
        return go;
    }

    // --------------------------------------------------------------- build

    public static string Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return "ERRO: saia do Play mode primeiro";

        CharacterController cc = Object.FindFirstObjectByType<CharacterController>();
        if (cc == null) return "ERRO: rover nao encontrado";
        player = cc.transform;

        StringBuilder sb = new StringBuilder();

        // materiais existentes, reaproveitados por referencia
        matDark = MatOf("Base");
        matBody = MatOf("Chassis");
        matGrey = MatOf("Sensor");
        matLens = MatOf("Dome");
        if (matDark == null || matBody == null || matGrey == null || matLens == null)
            return "ERRO: nao achei os materiais originais (Base/Chassis/Sensor/Dome)";

        // grupo proprio, para a adicao ser facil de remover inteira
        Transform old = Find(GroupName);
        if (old != null) Object.DestroyImmediate(old.gameObject);
        GameObject g = new GameObject(GroupName);
        g.transform.SetParent(player, false);
        g.transform.localPosition = Vector3.zero;
        group = g.transform;

        // ---- estrutura: para-choques e suportes ----
        Prim(PrimitiveType.Cube, "BumperFront", group,
             new Vector3(0f, 0.305f, 0.92f), Vector3.zero, new Vector3(0.98f, 0.10f, 0.07f), matDark);
        Prim(PrimitiveType.Cube, "BumperRear", group,
             new Vector3(0f, 0.305f, -0.92f), Vector3.zero, new Vector3(0.98f, 0.10f, 0.07f), matDark);
        for (int s = -1; s <= 1; s += 2)
        {
            string side = s < 0 ? "L" : "R";
            Prim(PrimitiveType.Cube, "BumperStrutF" + side, group,
                 new Vector3(0.36f * s, 0.305f, 0.865f), Vector3.zero, new Vector3(0.08f, 0.06f, 0.11f), matDark);
            Prim(PrimitiveType.Cube, "BumperStrutR" + side, group,
                 new Vector3(0.36f * s, 0.305f, -0.865f), Vector3.zero, new Vector3(0.08f, 0.06f, 0.11f), matDark);
            // longarina lateral, acompanhando o comprimento da Base
            Prim(PrimitiveType.Cube, "SideRail" + side, group,
                 new Vector3(0.545f * s, 0.225f, 0f), Vector3.zero, new Vector3(0.07f, 0.09f, 1.45f), matDark);
        }

        // ---- protetor de carter sob a base ----
        Prim(PrimitiveType.Cube, "SkidPlate", group,
             new Vector3(0f, 0.188f, 0.42f), Vector3.zero, new Vector3(0.80f, 0.035f, 0.52f), matDark);

        // ---- eixos ligando as rodas (cilindro deitado no X) ----
        Prim(PrimitiveType.Cylinder, "AxleFront", group,
             new Vector3(0f, 0.28f, 0.55f), new Vector3(0f, 0f, 90f), new Vector3(0.045f, 0.60f, 0.045f), matDark);
        Prim(PrimitiveType.Cylinder, "AxleRear", group,
             new Vector3(0f, 0.28f, -0.55f), new Vector3(0f, 0f, 90f), new Vector3(0.045f, 0.60f, 0.045f), matDark);

        // ---- grade frontal: 3 lamelas na face dianteira do Chassis (z=0.80) ----
        float[] slatY = new float[] { 0.37f, 0.45f, 0.53f };
        for (int i = 0; i < slatY.Length; i++)
            Prim(PrimitiveType.Cube, "GrilleSlat" + i, group,
                 new Vector3(0f, slatY[i], 0.815f), Vector3.zero,
                 new Vector3(0.44f, 0.035f, 0.025f), matDark);

        // ---- arcos de roda, na cor da carroceria ----
        // Placa VERTICAL rente a lateral do Chassis (X=+-0.5), nao bandeja
        // horizontal: uma aba deitada sobre a roda lia como prateleira.
        foreach (int sx in new int[] { -1, 1 })
            foreach (int sz in new int[] { -1, 1 })
            {
                string n = "WheelArch" + (sz > 0 ? "F" : "R") + (sx < 0 ? "L" : "R");
                Prim(PrimitiveType.Cube, n, group,
                     new Vector3(0.515f * sx, 0.50f, 0.55f * sz), Vector3.zero,
                     new Vector3(0.055f, 0.18f, 0.46f), matBody);
            }

        // ---- friso do capo, seguindo o topo do Chassis (y=0.625) ----
        Prim(PrimitiveType.Cube, "HoodLip", group,
             new Vector3(0f, 0.615f, 0.60f), Vector3.zero, new Vector3(0.86f, 0.05f, 0.34f), matBody);

        // ---- farois: usam o material do Dome, sem introduzir cor nova ----
        foreach (int sx in new int[] { -1, 1 })
            Prim(PrimitiveType.Cube, "Headlight" + (sx < 0 ? "L" : "R"), group,
                 new Vector3(0.32f * sx, 0.50f, 0.812f), Vector3.zero,
                 new Vector3(0.17f, 0.09f, 0.02f), matLens);

        // ---- aros e cubos, filhos das rodas para girarem junto ----
        // a roda tem escala (0.32,0.12,0.32): raio 0.16, semi-largura 0.12.
        // escala do filho e relativa, entao converte-se o alvo por esses fatores.
        string[] wheels = new string[] { "WheelFL", "WheelFR", "WheelBL", "WheelBR" };
        int rims = 0;
        foreach (string wn in wheels)
        {
            Transform w = Find(wn);
            if (w == null) { sb.AppendLine("AVISO: " + wn + " nao encontrada"); continue; }

            // limpa aro/cubo de execucoes anteriores: eles sao filhos da roda,
            // nao do grupo Detail, entao nao caem na limpeza do grupo
            for (int i = w.childCount - 1; i >= 0; i--)
            {
                string cn = w.GetChild(i).name;
                if (cn.EndsWith("_Rim") || cn.EndsWith("_Hub"))
                    Object.DestroyImmediate(w.GetChild(i).gameObject);
            }

            // aro: raio 0.105, semi-largura 0.125 (sobressai levemente do pneu)
            Prim(PrimitiveType.Cylinder, wn + "_Rim", w,
                 Vector3.zero, Vector3.zero,
                 new Vector3(0.105f / 0.16f, 0.125f / 0.12f, 0.105f / 0.16f), matGrey);
            // cubo central: raio 0.05, semi-largura 0.135
            Prim(PrimitiveType.Cylinder, wn + "_Hub", w,
                 Vector3.zero, Vector3.zero,
                 new Vector3(0.05f / 0.16f, 0.135f / 0.12f, 0.05f / 0.16f), matGrey);
            rims++;
        }

        // ---- pivos de esterco nas rodas dianteiras ----
        // o giro do pneu usa o Y LOCAL da roda (o cilindro ja vem com rotZ=90),
        // entao o esterco precisa de um pai proprio girando no Y do veiculo
        int pivots = 0;
        foreach (string wn in new string[] { "WheelFL", "WheelFR" })
        {
            Transform w = Find(wn);
            if (w == null) continue;
            if (w.parent != null && w.parent.name == wn + "_Steer") { pivots++; continue; }

            Vector3 lp = w.localPosition;
            GameObject pivot = new GameObject(wn + "_Steer");
            pivot.transform.SetParent(player, false);
            pivot.transform.localPosition = lp;
            pivot.transform.localRotation = Quaternion.identity;
            pivot.transform.localScale = Vector3.one;

            w.SetParent(pivot.transform, false);   // mantem os valores locais
            w.localPosition = Vector3.zero;        // agora a posicao e do pivo
            pivots++;
        }

        // garante que nenhuma peca antiga do rover receba decal
        int layered = 0;
        foreach (Renderer r in player.GetComponentsInChildren<Renderer>(true))
        {
            r.renderingLayerMask = LayerVehicle;
            EditorUtility.SetDirty(r);
            layered++;
        }

        EditorUtility.SetDirty(player.gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        sb.AppendLine("grupo '" + GroupName + "' criado com " + group.childCount + " pecas");
        sb.AppendLine("rodas com aro+cubo: " + rims);
        sb.AppendLine("pivos de esterco dianteiros: " + pivots);
        sb.AppendLine("renderers do rover no rendering layer 2: " + layered);
        sb.AppendLine("materiais novos criados: 0 (todos reaproveitados)");
        return sb.ToString();
    }
}
