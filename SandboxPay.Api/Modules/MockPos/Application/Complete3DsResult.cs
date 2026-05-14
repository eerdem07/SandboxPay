using SandboxPay.Api.Modules.MockPos.Domain;

namespace SandboxPay.Api.Modules.MockPos.Application;

public sealed record Complete3DsResult(
    PosAuthorizeStatus Status,
    PosTransactionType TransactionType,
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
    ThreeDsStatus? ThreeDsStatus,
    string? Eci,
    string? MessageVersion);
