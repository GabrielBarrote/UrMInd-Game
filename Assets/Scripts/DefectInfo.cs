using UnityEngine;

// Metadados de uma ocorrencia de infraestrutura, lidos pelo DefectScanner.
public class DefectInfo : MonoBehaviour
{
    public string defectType = "Ocorrencia";
    public string severity = "MEDIA";     // CRITICA / ALTA / MEDIA / BAIXA
    public string code = "000";
    public float markerScale = 1.5f;
    public float markerHeight = 1.0f;

    public Color SeverityColor()
    {
        switch (severity)
        {
            case "CRITICA": return new Color(1.00f, 0.26f, 0.22f);
            case "ALTA":    return new Color(1.00f, 0.56f, 0.12f);
            case "MEDIA":   return new Color(1.00f, 0.85f, 0.18f);
            default:        return new Color(0.45f, 0.85f, 1.00f);
        }
    }
}
