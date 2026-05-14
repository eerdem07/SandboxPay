using SandboxPay.Api.Modules.MockPos.Application;

namespace SandboxPay.Api.Modules.MockPos.Web.Contracts;

public sealed record Complete3DsResponse(
    string Status,
    string TransactionType,
    bool Approved,
    string ResponseCode,
    string ResponseMessage,
    string TransactionId,
    string? OriginalTransactionId,
    string? PosTransactionId,
    string? AuthCode,
    string? HostReferenceNumber,
    string? Amount,
    string? Currency,
    int? InstallmentCount,
    string? InstallmentAmount,
    string AuthorizedAt,
    string ThreeDsSessionId,
    string? ThreeDsStatus,
    string? Eci,
    string? MessageVersion)
{
    public static Complete3DsResponse FromResult(Complete3DsResult result)
    {
        return new Complete3DsResponse(
            result.Status.ToString(),
            result.TransactionType.ToString(),
            result.Approved,
            result.ResponseCode,
            result.ResponseMessage,
            result.TransactionId,
            result.OriginalTransactionId,
            result.PosTransactionId,
            result.AuthCode,
            result.HostReferenceNumber,
            result.Amount,
            result.Currency,
            result.InstallmentCount,
            result.InstallmentAmount,
            result.AuthorizedAt,
            result.ThreeDsSessionId,
            result.ThreeDsStatus?.ToString(),
            result.Eci,
            result.MessageVersion);
    }
}
