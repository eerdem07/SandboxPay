using Microsoft.AspNetCore.Mvc;
using SandboxPay.Api.Modules.MockPos.Application;
using SandboxPay.Api.Modules.MockPos.Web.Contracts;

namespace SandboxPay.Api.Modules.MockPos.Web;

[ApiController]
[Route("api/v1/pos")]
public sealed class PosVoidController(IVoidPaymentService voidPaymentService) : ControllerBase
{
    [HttpPost("void")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(VoidPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    public ActionResult<VoidPaymentResponse> Void([FromBody] VoidPaymentRequest? request)
    {
        var validationErrors = Validate(request);
        if (validationErrors.Count > 0)
        {
            return BadRequest(ValidationErrorResponse.FromErrors(validationErrors));
        }

        var result = voidPaymentService.Void(ToCommand(request!));

        return Ok(VoidPaymentResponse.FromResult(result));
    }

    private static VoidPaymentCommand ToCommand(VoidPaymentRequest request)
    {
        return new VoidPaymentCommand(
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

    private static IReadOnlyList<ValidationError> Validate(VoidPaymentRequest? request)
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
