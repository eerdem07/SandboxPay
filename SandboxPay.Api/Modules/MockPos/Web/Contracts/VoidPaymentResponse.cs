using SandboxPay.Api.Modules.MockPos.Application;

namespace SandboxPay.Api.Modules.MockPos.Web.Contracts;

public sealed record VoidPaymentResponse(
    string Status,
    string TransactionType,
    bool Approved,
    string ResponseCode,
    string ResponseMessage,
    string TransactionId,
    string OriginalTransactionId,
    string? PosVoidId,
    string OriginalPosTransactionId,
    string? HostReferenceNumber,
    string Amount,
    string Currency,
    string VoidedAt)
{
    public static VoidPaymentResponse FromResult(VoidPaymentResult result)
    {
        return new VoidPaymentResponse(
            result.Status.ToString(),
            result.TransactionType.ToString(),
            result.Approved,
            result.ResponseCode,
            result.ResponseMessage,
            result.TransactionId,
            result.OriginalTransactionId,
            result.PosVoidId,
            result.OriginalPosTransactionId,
            result.HostReferenceNumber,
            result.Amount,
            result.Currency,
            result.VoidedAt);
    }
}
