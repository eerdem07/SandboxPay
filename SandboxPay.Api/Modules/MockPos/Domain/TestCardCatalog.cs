namespace SandboxPay.Api.Modules.MockPos.Domain;

public static class TestCardCatalog
{
    private static readonly IReadOnlyDictionary<string, string> Cards =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["4111111111111111"] = "00",
            ["4000000000000002"] = "05",
            ["4000000000000012"] = "12",
            ["4000000000000013"] = "13",
            ["4000000000000014"] = "14",
            ["4000000000000030"] = "30",
            ["4000000000000041"] = "41",
            ["4000000000000043"] = "43",
            ["4000000000000051"] = "51",
            ["4000000000000054"] = "54",
            ["4000000000000057"] = "57",
            ["4000000000000058"] = "58",
            ["4000000000000061"] = "61",
            ["4000000000000065"] = "65",
            ["4000000000000091"] = "91",
            ["4000000000000096"] = "96",
            ["4000000000006000"] = "00",
            ["4000000000006003"] = "00",
            ["4000000000009995"] = "TIMEOUT"
        };

    public static bool TryResolveResponseCode(string normalizedPan, out PosResponseCode? responseCode)
    {
        if (Cards.TryGetValue(normalizedPan, out var code))
        {
            responseCode = PosResponseCode.FromCode(code);
            return true;
        }

        responseCode = null;
        return false;
    }
}
