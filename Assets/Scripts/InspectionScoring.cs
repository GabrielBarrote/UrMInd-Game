public static class InspectionScoring
{
    public static int Calculate(
        bool typeCorrect,
        bool priorityCorrect,
        bool excellentFraming,
        bool answeredDispatch,
        int evidencePoints,
        int categoryPoints,
        int priorityPoints,
        int framingPoints,
        int dispatchBonus)
    {
        int total = evidencePoints;
        if (typeCorrect) total += categoryPoints;
        if (priorityCorrect) total += priorityPoints;
        if (excellentFraming) total += framingPoints;
        if (answeredDispatch) total += dispatchBonus;
        return total;
    }

    public static string NormalizePlayerName(string value, int maxLength = 16)
    {
        string trimmed = (value ?? string.Empty).Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed.Substring(0, maxLength);
    }
}
