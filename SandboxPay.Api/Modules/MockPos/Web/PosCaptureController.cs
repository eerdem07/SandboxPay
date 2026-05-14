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

        PosValidationHelpers.AddRequired(errors, "merchantId", request.MerchantId);
        PosValidationHelpers.AddRequired(errors, "terminalId", request.TerminalId);
        PosValidationHelpers.AddRequired(errors, "orderId", request.OrderId);
        PosValidationHelpers.AddRequired(errors, "transactionId", request.TransactionId);
        PosValidationHelpers.AddRequired(errors, "originalTransactionId", request.OriginalTransactionId);
        PosValidationHelpers.AddRequired(errors, "originalPosTransactionId", request.OriginalPosTransactionId);
        PosValidationHelpers.AddRequired(errors, "authCode", request.AuthCode);
        PosValidationHelpers.AddRequired(errors, "hostReferenceNumber", request.HostReferenceNumber);
        PosValidationHelpers.AddAmountValidation(errors, request.Amount);
        PosValidationHelpers.AddRequired(errors, "currency", request.Currency);

        return errors;
    }
}
