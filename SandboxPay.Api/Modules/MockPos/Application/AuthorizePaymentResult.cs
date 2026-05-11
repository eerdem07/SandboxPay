using SandboxPay.Api.Modules.MockPos.Domain;

namespace SandboxPay.Api.Modules.MockPos.Application;

public sealed record AuthorizePaymentResult(
    PosAuthorizeStatus Status,
    PosTransactionType TransactionType,
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
    string AuthorizedAt);
