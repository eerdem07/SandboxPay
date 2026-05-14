using System.Globalization;
using SandboxPay.Api.Modules.MockPos.Application;
using SandboxPay.Api.Modules.MockPos.Web.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace SandboxPay.Api.Modules.MockPos.Web;

[ApiController]
[Route("api/v1/pos")]
public sealed class PosAuthorizeController(IAuthorizePaymentService authorizePaymentService) : ControllerBase
{
    [HttpPost("authorize")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(AuthorizePaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    public ActionResult<AuthorizePaymentResponse> Authorize([FromBody] AuthorizePaymentRequest? request)
    {
        var validationErrors = Validate(request);
        if (validationErrors.Count > 0)
        {
            return BadRequest(ValidationErrorResponse.FromErrors(validationErrors));
        }

        var result = authorizePaymentService.Authorize(ToCommand(request!));

        return Ok(AuthorizePaymentResponse.FromResult(result));
    }

    private static AuthorizePaymentCommand ToCommand(AuthorizePaymentRequest request)
    {
        return new AuthorizePaymentCommand(
            request.MerchantId!.Trim(),
            request.TerminalId!.Trim(),
            request.OrderId!.Trim(),
            request.TransactionId!.Trim(),
            request.Amount!.Trim(),
            request.Currency!.Trim(),
            request.InstallmentCount!.Value,
            request.Capture!.Value,
            request.Card!.Pan!.Trim());
    }

    private static IReadOnlyList<ValidationError> Validate(AuthorizePaymentRequest? request)
    {
        var errors = new List<ValidationError>();
        if (request is null)
        {
            errors.Add(new ValidationError("body", "request body is required"));
            return errors;
        }

        PosValidationHelpers.AddRequired(errors, "merchantId", request.MerchantId);
        PosValidationHelpers.AddRequired(errors, "terminalId", request.TerminalId);
        PosValidationHelpers.AddRequired(errors, "orderId", request.OrderId);
        PosValidationHelpers.AddRequired(errors, "transactionId", request.TransactionId);
        PosValidationHelpers.AddAmountValidation(errors, request.Amount);
        PosValidationHelpers.AddRequired(errors, "currency", request.Currency);

        if (request.InstallmentCount is null || request.InstallmentCount.Value < 1 || request.InstallmentCount.Value > 12)
        {
            errors.Add(new ValidationError("installmentCount", "installmentCount must be between 1 and 12"));
        }

        if (request.Capture is null)
        {
            errors.Add(new ValidationError("capture", "capture is required"));
        }

        if (request.Card is null)
        {
            errors.Add(new ValidationError("card", "card is required"));
            return errors;
        }

        PosValidationHelpers.AddRequired(errors, "card.holderName", request.Card.HolderName);
        PosValidationHelpers.AddRequired(errors, "card.pan", request.Card.Pan);
        AddExpiryMonthValidation(errors, request.Card.ExpiryMonth);
        AddExpiryYearValidation(errors, request.Card.ExpiryYear);
        AddCvvValidation(errors, request.Card.Cvv);

        return errors;
    }

    private static void AddExpiryMonthValidation(List<ValidationError> errors, string? expiryMonth)
    {
        var normalizedMonth = expiryMonth?.Trim();
        if (normalizedMonth is null
            || normalizedMonth.Length != 2
            || !int.TryParse(normalizedMonth, NumberStyles.None, CultureInfo.InvariantCulture, out var month)
            || month is < 1 or > 12)
        {
            errors.Add(new ValidationError("card.expiryMonth", "card.expiryMonth must be between 01 and 12"));
        }
    }

    private static void AddExpiryYearValidation(List<ValidationError> errors, string? expiryYear)
    {
        var normalized = expiryYear?.Trim();
        if (normalized is null
            || normalized.Length != 4
            || !int.TryParse(normalized, NumberStyles.None, CultureInfo.InvariantCulture, out _))
        {
            errors.Add(new ValidationError("card.expiryYear", "card.expiryYear must be a 4-digit year"));
        }
    }

    private static void AddCvvValidation(List<ValidationError> errors, string? cvv)
    {
        var normalizedCvv = cvv?.Trim();
        if (normalizedCvv is null
            || normalizedCvv.Length is not (3 or 4)
            || normalizedCvv.Any(character => !char.IsDigit(character)))
        {
            errors.Add(new ValidationError("card.cvv", "card.cvv must be 3 or 4 digits"));
        }
    }
}
