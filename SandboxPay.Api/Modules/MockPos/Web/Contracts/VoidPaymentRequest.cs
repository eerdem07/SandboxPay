namespace SandboxPay.Api.Modules.MockPos.Web.Contracts;

public sealed class VoidPaymentRequest
{
    public string? MerchantId { get; init; }

    public string? TerminalId { get; init; }

    public string? OrderId { get; init; }

    public string? TransactionId { get; init; }

    public string? OriginalTransactionId { get; init; }

    public string? OriginalPosTransactionId { get; init; }

    public string? AuthCode { get; init; }

    public string? HostReferenceNumber { get; init; }

    public string? Amount { get; init; }

    public string? Currency { get; init; }
}
