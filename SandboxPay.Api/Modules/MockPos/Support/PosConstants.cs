using System.Globalization;

namespace SandboxPay.Api.Modules.MockPos.Support;

internal static class PosConstants
{
    internal static readonly TimeSpan AuthorizationTtl = TimeSpan.FromDays(7);
    internal const string ThreeDsMessageVersion = "2.2.0";

    internal static string FormatTimestamp(DateTimeOffset timestamp) =>
        timestamp.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

    internal static string? CalculateInstallmentAmount(string amount, int installmentCount)
    {
        if (installmentCount <= 1) return null;
        var parsed = decimal.Parse(amount, CultureInfo.InvariantCulture);
        var perInstallment = Math.Round(parsed / installmentCount, 2, MidpointRounding.AwayFromZero);
        return perInstallment.ToString("F2", CultureInfo.InvariantCulture);
    }
}
