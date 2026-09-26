using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public static class PlayTest
{
    // Preenche o nome e dispara o botao, para validar a transicao MENU->JOGANDO
    // sem depender de input real.
    public static string StartRound()
    {
        if (!Application.isPlaying) return "precisa estar em Play mode";
        InspectionGame g = Object.FindFirstObjectByType<InspectionGame>();
        if (g == null) return "InspectionGame nao encontrado";
        if (g.nameField == null) return "nameField nulo";

        g.nameField.text = "GABRIEL";
        g.startButton.onClick.Invoke();

        FieldInfo st = typeof(InspectionGame).GetField("state",
            BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo tl = typeof(InspectionGame).GetField("timeLeft",
            BindingFlags.Instance | BindingFlags.NonPublic);
        return "estado=" + (st == null ? "?" : st.GetValue(g).ToString())
            + " timeLeft=" + (tl == null ? "?" : tl.GetValue(g).ToString())
            + " hudAtivo=" + g.hudPanel.activeSelf
            + " menuAtivo=" + g.menuPanel.activeSelf
            + " cartEnabled=" + g.cart.enabled;
    }

    // Registra a ocorrencia mais proxima, como se o jogador tivesse apertado E.
    public static string RegisterNearest()
    {
        InspectionGame g = Object.FindFirstObjectByType<InspectionGame>();
        MethodInfo m = typeof(InspectionGame).GetMethod("HandleRegisterInput",
            BindingFlags.Instance | BindingFlags.NonPublic);
        DefectScanner sc = Object.FindFirstObjectByType<DefectScanner>();
        return "alvo do scanner = " + (sc.Current == null ? "nenhum"
            : sc.Current.defectType + " [" + sc.Current.severity + "] vale "
              + InspectionGame.PointsFor(sc.Current.severity) + " pts");
    }

    // Registra algumas ocorrencias a forca e encerra a rodada, para validar a
    // tela de resultado sem precisar dirigir pela cidade.
    public static string ForceResults()
    {
        if (!Application.isPlaying) return "precisa estar em Play mode";
        InspectionGame g = Object.FindFirstObjectByType<InspectionGame>();
        if (g == null) return "InspectionGame nao encontrado";

        System.Type T = typeof(InspectionGame);
        BindingFlags BF = BindingFlags.Instance | BindingFlags.NonPublic;
        FieldInfo fReg = T.GetField("registered", BF);
        FieldInfo fCount = T.GetField("countByType", BF);
        FieldInfo fPts = T.GetField("pointsByType", BF);
        FieldInfo fScore = T.GetField("score", BF);

        System.Collections.Generic.HashSet<int> reg =
            (System.Collections.Generic.HashSet<int>)fReg.GetValue(g);
        System.Collections.Generic.Dictionary<string, int> cnt =
            (System.Collections.Generic.Dictionary<string, int>)fCount.GetValue(g);
        System.Collections.Generic.Dictionary<string, int> pts =
            (System.Collections.Generic.Dictionary<string, int>)fPts.GetValue(g);

        int total = 0;
        DefectInfo[] all = Object.FindObjectsByType<DefectInfo>(FindObjectsSortMode.None);
        int taken = 0;
        foreach (DefectInfo d in all)
        {
            if (taken >= 11) break;
            if (reg.Contains(d.GetInstanceID())) continue;
            reg.Add(d.GetInstanceID());
            int p = InspectionGame.PointsFor(d.severity);
            total += p;
            if (!cnt.ContainsKey(d.defectType)) { cnt[d.defectType] = 0; pts[d.defectType] = 0; }
            cnt[d.defectType]++;
            pts[d.defectType] += p;
            taken++;
        }
        fScore.SetValue(g, total);

        MethodInfo end = T.GetMethod("EndRound", BF);
        end.Invoke(g, null);
        return "resultado forcado: " + taken + " ocorrencias, " + total + " pts";
    }
}
