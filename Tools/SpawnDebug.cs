using System.Reflection;
using UnityEngine;

public static class SpawnDebug
{
    public static string Run()
    {
        InspectionGame g = Object.FindFirstObjectByType<InspectionGame>();
        AnalysisCartController c = Object.FindFirstObjectByType<AnalysisCartController>();
        CharacterController cc = c.GetComponent<CharacterController>();

        string before = c.transform.position.ToString("F2");

        MethodInfo m = typeof(InspectionGame).GetMethod("MoveToSpawn",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (m == null) return "MoveToSpawn nao encontrado";
        m.Invoke(g, null);

        string after = c.transform.position.ToString("F2");
        return "antes=" + before + "  depois=" + after
            + "  ccEnabled=" + cc.enabled + "  cartEnabled=" + c.enabled
            + "  speed=" + c.CurrentSpeed.ToString("F2");
    }
}
