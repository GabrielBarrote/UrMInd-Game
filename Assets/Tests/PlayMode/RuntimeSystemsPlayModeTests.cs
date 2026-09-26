using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class RuntimeSystemsPlayModeTests
{
    [UnityTest]
    public IEnumerator DayNightCycle_ChangesPhaseWithoutReloadingScene()
    {
        yield return null;
        var cycle = DayNightCycle.Instance;
        Assert.IsNotNull(cycle);
        cycle.SetPhase(DayNightCycle.Phase.Night);
        Assert.AreEqual(DayNightCycle.Phase.Night, cycle.CurrentPhase);
        Assert.AreEqual(1f, cycle.NightFactor);
    }

    [UnityTest]
    public IEnumerator LightBudget_DisablesDistantLocalLight()
    {
        var nearObject = new GameObject("Near Light");
        nearObject.AddComponent<Light>().type = LightType.Point;
        var farObject = new GameObject("Far Light");
        Light far = farObject.AddComponent<Light>();
        far.type = LightType.Point;
        farObject.transform.position = Vector3.one * 500f;
        var budgetObject = new GameObject("Budget Test");
        var budget = budgetObject.AddComponent<CityLightBudget>();
        budget.maximumDistance = 100f;
        yield return null;
        budget.RefreshLights();
        budget.ApplyBudgetNow(Vector3.zero, 1f);
        Assert.IsFalse(far.enabled);
        Object.Destroy(nearObject);
        Object.Destroy(farObject);
        Object.Destroy(budgetObject);
    }
}
