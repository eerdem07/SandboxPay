namespace SandboxPay.Api.Modules.MockPos.Application;

public sealed record VoidPaymentCommand(
    string MerchantId,
    string TerminalId,
    string OrderId,
    string TransactionId,
    string OriginalTransactionId,
    string OriginalPosTransactionId,
    string AuthCode,
    string HostReferenceNumber,
    string Amount,
    string Currency);
