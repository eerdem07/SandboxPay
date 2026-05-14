using Microsoft.AspNetCore.Mvc;
using SandboxPay.Api.Modules.MockPos.Application;
using SandboxPay.Api.Modules.MockPos.Web.Contracts;

namespace SandboxPay.Api.Modules.MockPos.Web;

[ApiController]
[Route("api/v1/pos")]
public sealed class PosRefundController(IRefundPaymentService refundPaymentService) : ControllerBase
{
    [HttpPost("refund")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(RefundPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    public ActionResult<RefundPaymentResponse> Refund([FromBody] RefundPaymentRequest? request)
    {
        var validationErrors = Validate(request);
        if (validationErrors.Count > 0)
        {
            return BadRequest(ValidationErrorResponse.FromErrors(validationErrors));
        }

        var result = refundPaymentService.Refund(ToCommand(request!));

        return Ok(RefundPaymentResponse.FromResult(result));
    }

    private static RefundPaymentCommand ToCommand(RefundPaymentRequest request)
    {
        return new RefundPaymentCommand(
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

    private static IReadOnlyList<ValidationError> Validate(RefundPaymentRequest? request)
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
