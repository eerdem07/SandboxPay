using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using SandboxPay.Api.Modules.MockPos.Application;
using SandboxPay.Api.Modules.MockPos.Web.Contracts;

namespace SandboxPay.Api.Modules.MockPos.Web;

[ApiController]
[Route("api/v1/pos")]
public sealed class PosCaptureController(ICapturePaymentService capturePaymentService) : ControllerBase
{
    [HttpPost("capture")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(CapturePaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    public ActionResult<CapturePaymentResponse> Capture([FromBody] CapturePaymentRequest? request)
    {
        var validationErrors = Validate(request);
        if (validationErrors.Count > 0)
        {
            return BadRequest(ValidationErrorResponse.FromErrors(validationErrors));
        }

        var result = capturePaymentService.Capture(ToCommand(request!));

        return Ok(CapturePaymentResponse.FromResult(result));
    }

    private static CapturePaymentCommand ToCommand(CapturePaymentRequest request)
    {
        return new CapturePaymentCommand(
            request.MerchantId!.Trim(),
            request.TerminalId!.Trim(),
            request.OrderId!.Trim(),
            request.TransactionId!.Trim(),
            request.OriginalTransactionId!.Trim(),
            request.OriginalPosTransactionId!.Trim(),
            request.AuthCode!.Trim(),
            request.HostReferenceNumber!.Trim(),
            request.Amount!.Trim(),
            request.Currency!.Trim());
    }

    private static IReadOnlyList<ValidationError> Validate(CapturePaymentRequest? request)
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
        AddRequired(errors, "originalTransactionId", request.OriginalTransactionId);
        AddRequired(errors, "originalPosTransactionId", request.OriginalPosTransactionId);
        AddRequired(errors, "authCode", request.AuthCode);
        AddRequired(errors, "hostReferenceNumber", request.HostReferenceNumber);
        AddAmountValidation(errors, request.Amount);
        AddRequired(errors, "currency", request.Currency);

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
}
