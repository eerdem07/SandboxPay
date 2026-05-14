using System.Globalization;
using SandboxPay.Api.Modules.MockPos.Web.Contracts;

namespace SandboxPay.Api.Modules.MockPos.Web;

internal static class PosValidationHelpers
{
    internal static void AddRequired(List<ValidationError> errors, string field, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add(new ValidationError(field, $"{field} must not be blank"));
    }

    internal static void AddAmountValidation(List<ValidationError> errors, string? amount)
    {
        if (string.IsNullOrWhiteSpace(amount)
            || !decimal.TryParse(amount.Trim(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsedAmount)
            || parsedAmount <= 0)
        {
            errors.Add(new ValidationError("amount", "amount must be greater than 0"));
        }
    }
}
