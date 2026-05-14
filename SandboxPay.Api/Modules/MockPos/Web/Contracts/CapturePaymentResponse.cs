using SandboxPay.Api.Modules.MockPos.Application;

namespace SandboxPay.Api.Modules.MockPos.Web.Contracts;

public sealed record CapturePaymentResponse(
    string Status,
    string TransactionType,
    bool Approved,
    string ResponseCode,
    string ResponseMessage,
    string TransactionId,
    string OriginalTransactionId,
    string? PosCaptureId,
    string OriginalPosTransactionId,
    string? HostReferenceNumber,
    string Amount,
    string Currency,
    string? CapturedAt)
{
    public static CapturePaymentResponse FromResult(CapturePaymentResult result)
    {
        return new CapturePaymentResponse(
            result.Status.ToString(),
            result.TransactionType.ToString(),
            result.Approved,
            result.ResponseCode,
            result.ResponseMessage,
            result.TransactionId,
            result.OriginalTransactionId,
            result.PosCaptureId,
            result.OriginalPosTransactionId,
            result.HostReferenceNumber,
            result.Amount,
            result.Currency,
            result.CapturedAt);
    }
}
