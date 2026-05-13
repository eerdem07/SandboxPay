using SandboxPay.Api.Modules.MockPos.Domain;

namespace SandboxPay.Api.Modules.MockPos.Application;

public sealed record RefundPaymentResult(
    PosRefundStatus Status,
    PosTransactionType TransactionType,
    bool Approved,
    string ResponseCode,
    string ResponseMessage,
    string TransactionId,
    string OriginalTransactionId,
    string? PosRefundId,
    string OriginalPosTransactionId,
    string? HostReferenceNumber,
    string Amount,
    string Currency,
    string? RefundedAt);
