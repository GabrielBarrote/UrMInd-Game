using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

public static class ProjectUpgrade
{
    public static void ApplySettings()
    {
        PlayerSettings.companyName = "UrMInd";
        PlayerSettings.productName = "UrMInd Game";
        PlayerSettings.bundleVersion = "1.0.0";
        PlayerSettings.defaultScreenWidth = 1980;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = false;

        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Branding/UrMIndIcon.png");
        if (icon == null) throw new System.InvalidOperationException("Icone UrMInd nao foi importado.");
        PlayerSettings.SetIcons(NamedBuildTarget.Standalone, new[] { icon }, IconKind.Application);

        AssetDatabase.SaveAssets();
        Debug.Log("[ProjectUpgrade] UrMInd, versao 1.0.0, 1980x1080 e icone configurados.");
    }
}
