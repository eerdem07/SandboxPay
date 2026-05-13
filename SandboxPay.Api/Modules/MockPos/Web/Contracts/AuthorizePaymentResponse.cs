using SandboxPay.Api.Modules.MockPos.Application;

namespace SandboxPay.Api.Modules.MockPos.Web.Contracts;

public sealed record AuthorizePaymentResponse(
    string Status,
    string TransactionType,
    bool Approved,
    string ResponseCode,
    string ResponseMessage,
    string TransactionId,
    string? PosTransactionId,
    string? AuthCode,
    string? HostReferenceNumber,
    string Amount,
    string Currency,
    int InstallmentCount,
    string AuthorizedAt)
{
    public static AuthorizePaymentResponse FromResult(AuthorizePaymentResult result)
    {
        return new AuthorizePaymentResponse(
            result.Status.ToString(),
            result.TransactionType.ToString(),
            result.Approved,
            result.ResponseCode,
            result.ResponseMessage,
            result.TransactionId,
            result.PosTransactionId,
            result.AuthCode,
            result.HostReferenceNumber,
            result.Amount,
            result.Currency,
            result.InstallmentCount,
            result.AuthorizedAt);
    }
}
