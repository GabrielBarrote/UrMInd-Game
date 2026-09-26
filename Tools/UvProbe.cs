using System.Text;
using UnityEngine;
using UnityEditor;

public static class UvProbe
{
    public static string Report()
    {
        string[] paths = new string[] {
            "Assets/City 02/Third Party/Kenney/City Kit (Roads)/road-straight.fbx",
            "Assets/City 02/Third Party/Kenney/City Kit (Commercial)/building-f.fbx",
            "Assets/City 02/Third Party/Kenney/City Kit (Cars)/Models/FBX format/sedan.fbx"
        };
        StringBuilder sb = new StringBuilder();
        foreach (string p in paths)
        {
            Object[] objs = AssetDatabase.LoadAllAssetsAtPath(p);
            foreach (Object o in objs)
            {
                Mesh m = o as Mesh;
                if (m == null) continue;
                Vector2[] uv = m.uv;
                if (uv == null || uv.Length == 0) { sb.AppendLine(p + " :: " + m.name + " SEM UV0"); continue; }
                float minx = 9e9f, maxx = -9e9f, miny = 9e9f, maxy = -9e9f;
                foreach (Vector2 u in uv)
                {
                    if (u.x < minx) minx = u.x; if (u.x > maxx) maxx = u.x;
                    if (u.y < miny) miny = u.y; if (u.y > maxy) maxy = u.y;
                }
                sb.AppendLine(m.name + " verts=" + m.vertexCount + " tris=" + (m.triangles.Length / 3)
                    + " uv0 X[" + minx.ToString("F4") + ".." + maxx.ToString("F4") + "]"
                    + " Y[" + miny.ToString("F4") + ".." + maxy.ToString("F4") + "]"
                    + " uv2=" + (m.uv2 != null && m.uv2.Length > 0)
                    + " normals=" + (m.normals.Length > 0) + " tangents=" + (m.tangents.Length > 0)
                    + " readable=" + m.isReadable);
                break;
            }
        }
        return sb.ToString();
    }
}
