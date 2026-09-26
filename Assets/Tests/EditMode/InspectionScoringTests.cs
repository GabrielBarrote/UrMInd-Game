using NUnit.Framework;

public sealed class InspectionScoringTests
{
    [Test]
    public void Calculate_AwardsEveryEarnedBonus()
    {
        Assert.AreEqual(115, InspectionScoring.Calculate(
            true, true, true, true, 40, 30, 20, 10, 15));
    }

    [Test]
    public void Calculate_OnlyEvidenceWhenAnswersAreWrong()
    {
        Assert.AreEqual(40, InspectionScoring.Calculate(
            false, false, false, false, 40, 30, 20, 10, 15));
    }

    [TestCase("  Gabriela  ", "Gabriela")]
    [TestCase("12345678901234567890", "1234567890123456")]
    [TestCase(null, "")]
    public void NormalizePlayerName_TrimsAndLimits(string input, string expected)
    {
        Assert.AreEqual(expected, InspectionScoring.NormalizePlayerName(input));
    }
}
