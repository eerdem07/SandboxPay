using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SandboxPay.Api.Modules.MockPos.Web.Contracts;

public sealed record ValidationErrorResponse(
    string Status,
    bool Approved,
    string ResponseCode,
    string ResponseMessage,
    IReadOnlyCollection<ValidationError> Errors)
{
    public static ValidationErrorResponse FromErrors(IReadOnlyCollection<ValidationError> errors)
    {
        return new ValidationErrorResponse("FAILED", false, "30", "Format error", errors);
    }

    public static ValidationErrorResponse FromModelState(ModelStateDictionary modelState)
    {
        var errors = modelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .SelectMany(
                entry => entry.Value!.Errors.Select(error =>
                    new ValidationError(ToCamelCasePath(entry.Key), ResolveMessage(error))))
            .ToArray();

        return FromErrors(errors.Length > 0
            ? errors
            : [new ValidationError("body", "request body is invalid")]);
    }

    private static string ResolveMessage(ModelError error)
    {
        return string.IsNullOrWhiteSpace(error.ErrorMessage)
            ? "request body is invalid"
            : error.ErrorMessage;
    }

    private static string ToCamelCasePath(string key)
    {
        var field = key.Trim();
        if (string.IsNullOrEmpty(field) || field == "$")
        {
            return "body";
        }

        if (field.StartsWith("$.", StringComparison.Ordinal))
        {
            field = field[2..];
        }

        if (field.StartsWith("request.", StringComparison.OrdinalIgnoreCase))
        {
            field = field["request.".Length..];
        }

        return string.Join('.', field
            .Split('.', StringSplitOptions.RemoveEmptyEntries)
            .Select(ToCamelCase));
    }

    private static string ToCamelCase(string value)
    {
        if (string.IsNullOrEmpty(value) || char.IsLower(value[0]))
        {
            return value;
        }

        return char.ToLowerInvariant(value[0]) + value[1..];
    }
}
