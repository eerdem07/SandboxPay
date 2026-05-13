namespace SandboxPay.Api.Modules.MockPos.Application;

public sealed record PosAuthorization(
    string MerchantId,
    string TerminalId,
    string OrderId,
    string TransactionId,
    string PosTransactionId,
    string AuthCode,
    string HostReferenceNumber,
    string Amount,
    string Currency,
    DateTimeOffset AuthorizedAt,
    DateTimeOffset AuthorizationExpiresAt,
    bool Captured,
    bool Voided,
    bool Refunded);
