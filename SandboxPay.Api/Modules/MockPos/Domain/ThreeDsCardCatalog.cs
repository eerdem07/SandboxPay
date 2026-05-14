namespace SandboxPay.Api.Modules.MockPos.Domain;

public static class ThreeDsCardCatalog
{
    private static readonly IReadOnlyDictionary<string, (ThreeDsFlow Flow, ThreeDsScenario Scenario)> Cards =
        new Dictionary<string, (ThreeDsFlow, ThreeDsScenario)>(StringComparer.Ordinal)
        {
            ["4000000000003006"] = (ThreeDsFlow.FRICTIONLESS, ThreeDsScenario.FRICTIONLESS_APPROVED),
            ["4000000000003014"] = (ThreeDsFlow.CHALLENGE,    ThreeDsScenario.CHALLENGE_APPROVED),
            ["4000000000003022"] = (ThreeDsFlow.CHALLENGE,    ThreeDsScenario.CHALLENGE_FAILED_AUTH),
            ["4000000000003030"] = (ThreeDsFlow.ATTEMPTED,    ThreeDsScenario.UNAVAILABLE_ATTEMPTED),
            ["4000000000003048"] = (ThreeDsFlow.TIMEOUT,      ThreeDsScenario.CHALLENGE_TIMEOUT),
            ["4000000000003055"] = (ThreeDsFlow.FRICTIONLESS, ThreeDsScenario.FRICTIONLESS_DECLINED)
        };

    public static bool TryGet(string normalizedPan, out ThreeDsFlow flow, out ThreeDsScenario scenario)
    {
        if (Cards.TryGetValue(normalizedPan, out var entry))
        {
            flow = entry.Flow;
            scenario = entry.Scenario;
            return true;
        }

        flow = default;
        scenario = default;
        return false;
    }
}
