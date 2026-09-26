using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor;

public static class WireRover
{
    static Transform Find(Transform root, string n)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == n) return t;
        return null;
    }

    public static string Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return "ERRO: saia do Play mode primeiro";

        CharacterController cc = Object.FindFirstObjectByType<CharacterController>();
        if (cc == null) return "rover nao encontrado";
        Transform p = cc.transform;
        StringBuilder sb = new StringBuilder();

        CartWheels cw = p.GetComponent<CartWheels>();
        if (cw == null) cw = p.gameObject.AddComponent<CartWheels>();

        List<Transform> ws = new List<Transform>();
        foreach (string n in new string[] { "WheelFL", "WheelFR", "WheelBL", "WheelBR" })
        {
            Transform t = Find(p, n);
            if (t != null) ws.Add(t);
        }
        cw.wheels = ws.ToArray();
        cw.steerLeft = Find(p, "WheelFL_Steer");
        cw.steerRight = Find(p, "WheelFR_Steer");
        cw.wheelRadius = 0.16f;
        EditorUtility.SetDirty(cw);

        sb.AppendLine("CartWheels: rodas=" + cw.wheels.Length
            + " steerL=" + (cw.steerLeft == null ? "NULO" : cw.steerLeft.name)
            + " steerR=" + (cw.steerRight == null ? "NULO" : cw.steerRight.name));

        CartTreeImpact ti = p.GetComponent<CartTreeImpact>();
        if (ti == null) ti = p.gameObject.AddComponent<CartTreeImpact>();
        EditorUtility.SetDirty(ti);
        sb.AppendLine("CartTreeImpact: presente, minImpactSpeed=" + ti.minImpactSpeed);

        AnalysisCartController ctrl = p.GetComponent<AnalysisCartController>();
        sb.AppendLine("cameraRig=" + (ctrl.cameraRig == null ? "NULO" : ctrl.cameraRig.name)
            + " probeRadius=" + ctrl.cameraProbeRadius
            + " minDist=" + ctrl.cameraMinDistance);

        EditorUtility.SetDirty(p.gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        return sb.ToString();
    }
}
