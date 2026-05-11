using System.Globalization;
using SandboxPay.Api.Modules.MockPos.Application;
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

        AddRequired(errors, "merchantId", request.MerchantId);
        AddRequired(errors, "terminalId", request.TerminalId);
        AddRequired(errors, "orderId", request.OrderId);
        AddRequired(errors, "transactionId", request.TransactionId);
        AddAmountValidation(errors, request.Amount);
        AddRequired(errors, "currency", request.Currency);

        if (request.InstallmentCount is null || request.InstallmentCount.Value < 1)
        {
            errors.Add(new ValidationError("installmentCount", "installmentCount must be greater than or equal to 1"));
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

        AddRequired(errors, "card.holderName", request.Card.HolderName);
        AddRequired(errors, "card.pan", request.Card.Pan);
        AddExpiryMonthValidation(errors, request.Card.ExpiryMonth);
        AddRequired(errors, "card.expiryYear", request.Card.ExpiryYear);
        AddCvvValidation(errors, request.Card.Cvv);

        return errors;
    }

    private static void AddRequired(List<ValidationError> errors, string field, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new ValidationError(field, $"{field} must not be blank"));
        }
    }

    private static void AddAmountValidation(List<ValidationError> errors, string? amount)
    {
        if (string.IsNullOrWhiteSpace(amount)
            || !decimal.TryParse(amount.Trim(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsedAmount)
            || parsedAmount <= 0)
        {
            errors.Add(new ValidationError("amount", "amount must be greater than 0"));
        }
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
