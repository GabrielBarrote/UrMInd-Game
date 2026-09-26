using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Liga na cena as referencias novas de Inspector, que o codigo sozinho nao
// consegue preencher. Roda uma vez; e idempotente.
public static class WireRefs
{
    public static void Apply()
    {
        var scene = EditorSceneManager.OpenScene("Assets/City - 02 - Day.unity", OpenSceneMode.Single);

        var game = Object.FindAnyObjectByType<InspectionGame>();
        if (game == null) { Debug.LogError("[WireRefs] InspectionGame nao encontrado"); EditorApplication.Exit(1); return; }

        var hud = GameObject.Find("AnalysisHUD");
        if (hud == null) { Debug.LogError("[WireRefs] AnalysisHUD nao encontrado"); EditorApplication.Exit(1); return; }

        game.analysisPanel = hud;

        // roundSeconds ja estava serializado em 60 na cena; o valor salvo ganha
        // do default do codigo, entao o novo tempo de desafio precisa ser
        // escrito aqui.
        game.roundSeconds = 180f;
        game.challengeMode = true;
        EditorUtility.SetDirty(game);

        var sc = Object.FindAnyObjectByType<DefectScanner>();
        if (sc != null)
        {
            sc.revealIdentity = !game.challengeMode;
            EditorUtility.SetDirty(sc);
        }

        Debug.Log("[WireRefs] analysisPanel=" + game.analysisPanel.name
            + " scanner=" + (game.scanner != null ? game.scanner.name : "NULO")
            + " cart=" + (game.cart != null ? game.cart.name : "NULO")
            + " roundSeconds=" + game.roundSeconds
            + " challengeMode=" + game.challengeMode);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
