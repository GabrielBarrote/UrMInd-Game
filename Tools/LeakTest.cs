using System.Linq;
using UnityEngine;
using UnityEditor;

public static class LeakTest
{
    static string SavedPath { get { return "Tools/backups/rover_pos.txt"; } }

    // Coloca o rover exatamente sobre uma rachadura, guardando a posicao antiga.
    public static string MoveRoverToCrack(string indexStr)
    {
        int index = 0; int.TryParse(indexStr, out index);
        GameObject root = GameObject.Find("City 02/World/Infrastructure Defects");
        if (root == null) return "root nao encontrado";
        DefectInfo[] cracks = root.GetComponentsInChildren<DefectInfo>(true)
            .Where(d => d.defectType != null && d.defectType.IndexOf("achadura") >= 0).ToArray();
        DefectInfo c = cracks[Mathf.Clamp(index, 0, cracks.Length - 1)];
        Vector3 p = c.transform.position;

        CharacterController cc = UnityEngine.Object.FindFirstObjectByType<CharacterController>();
        if (cc == null) return "rover nao encontrado";

        System.IO.File.WriteAllText(SavedPath,
            cc.transform.position.x.ToString(System.Globalization.CultureInfo.InvariantCulture) + " " +
            cc.transform.position.y.ToString(System.Globalization.CultureInfo.InvariantCulture) + " " +
            cc.transform.position.z.ToString(System.Globalization.CultureInfo.InvariantCulture));

        cc.transform.position = new Vector3(p.x, 0.03f, p.z);
        return "rover em " + c.name + " alvo=" + p.x.ToString("F2") + "," + p.z.ToString("F2");
    }

    public static string RestoreRover()
    {
        if (!System.IO.File.Exists(SavedPath)) return "sem posicao salva";
        string[] parts = System.IO.File.ReadAllText(SavedPath).Split(' ');
        CharacterController cc = UnityEngine.Object.FindFirstObjectByType<CharacterController>();
        if (cc == null) return "rover nao encontrado";
        cc.transform.position = new Vector3(
            float.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture),
            float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture),
            float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture));
        return "rover restaurado";
    }
}
