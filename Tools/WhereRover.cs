using System.Reflection;
using UnityEngine;

public static class WhereRover
{
    public static string Run()
    {
        InspectionGame g = Object.FindFirstObjectByType<InspectionGame>();
        AnalysisCartController c = Object.FindFirstObjectByType<AnalysisCartController>();
        FieldInfo st = typeof(InspectionGame).GetField("state",
            BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo tl = typeof(InspectionGame).GetField("timeLeft",
            BindingFlags.Instance | BindingFlags.NonPublic);
        return "estado=" + st.GetValue(g)
            + " timeLeft=" + tl.GetValue(g)
            + " | spawnConfig=" + g.spawnPosition.ToString("F2")
            + " | roverPos=" + c.transform.position.ToString("F2")
            + " yaw=" + c.transform.eulerAngles.y.ToString("F0")
            + " | camPos=" + (c.cameraRig == null ? "?" : c.cameraRig.position.ToString("F2"));
    }
}
