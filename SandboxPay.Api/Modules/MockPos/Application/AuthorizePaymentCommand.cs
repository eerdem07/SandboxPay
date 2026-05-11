namespace SandboxPay.Api.Modules.MockPos.Application;

public sealed record AuthorizePaymentCommand(
    string MerchantId,
    string TerminalId,
    string OrderId,
    string TransactionId,
    string Amount,
    string Currency,
    int InstallmentCount,
    bool Capture,
    string CardPan);
