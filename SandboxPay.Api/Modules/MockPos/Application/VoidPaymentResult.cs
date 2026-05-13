using SandboxPay.Api.Modules.MockPos.Domain;

namespace SandboxPay.Api.Modules.MockPos.Application;

public sealed record VoidPaymentResult(
    PosVoidStatus Status,
    PosTransactionType TransactionType,
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
    string VoidedAt);
