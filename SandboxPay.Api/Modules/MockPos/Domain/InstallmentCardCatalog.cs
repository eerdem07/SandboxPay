namespace SandboxPay.Api.Modules.MockPos.Domain;

public static class InstallmentCardCatalog
{
    // Value is MaxInstallmentCount: 0 = installments not permitted, N = max allowed count
    private static readonly IReadOnlyDictionary<string, int> Cards =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["4000000000006000"] = 0,
            ["4000000000006003"] = 3,
        };

    public static bool TryResolve(string normalizedPan, int installmentCount, out PosResponseCode? responseCode)
    {
        if (!Cards.TryGetValue(normalizedPan, out var maxInstallmentCount))
        {
            responseCode = null;
            return false;
        }

        if (maxInstallmentCount == 0 || installmentCount > maxInstallmentCount)
        {
            responseCode = PosResponseCode.RestrictedCard;
            return true;
        }

        responseCode = null;
        return false;
    }
}
