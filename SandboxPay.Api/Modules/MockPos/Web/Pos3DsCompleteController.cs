using Microsoft.AspNetCore.Mvc;
using SandboxPay.Api.Modules.MockPos.Application;
using SandboxPay.Api.Modules.MockPos.Web.Contracts;

namespace SandboxPay.Api.Modules.MockPos.Web;

[ApiController]
[Route("api/v1/pos/3ds")]
public sealed class Pos3DsCompleteController(IComplete3DsService complete3DsService) : ControllerBase
{
    [HttpPost("complete")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(Complete3DsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    public ActionResult<Complete3DsResponse> Complete([FromBody] Complete3DsRequest? request)
    {
        var validationErrors = Validate(request);
        if (validationErrors.Count > 0)
        {
            return BadRequest(ValidationErrorResponse.FromErrors(validationErrors));
        }

        var result = complete3DsService.Complete(ToCommand(request!));

        return Ok(Complete3DsResponse.FromResult(result));
    }

    private static Complete3DsCommand ToCommand(Complete3DsRequest request)
    {
        return new Complete3DsCommand(
            request.MerchantId!.Trim(),
            request.TerminalId!.Trim(),
            request.OrderId!.Trim(),
            request.TransactionId!.Trim(),
            request.ThreeDsSessionId!.Trim());
    }

    private static IReadOnlyList<ValidationError> Validate(Complete3DsRequest? request)
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
        PosValidationHelpers.AddRequired(errors, "threeDsSessionId", request.ThreeDsSessionId);

        return errors;
    }
}
