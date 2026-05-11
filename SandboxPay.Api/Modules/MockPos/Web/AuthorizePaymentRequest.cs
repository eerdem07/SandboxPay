namespace SandboxPay.Api.Modules.MockPos.Web;

public sealed class AuthorizePaymentRequest
{
    public string? MerchantId { get; init; }

    public string? TerminalId { get; init; }

    public string? OrderId { get; init; }

    public string? TransactionId { get; init; }

    public string? Amount { get; init; }

    public string? Currency { get; init; }

    public int? InstallmentCount { get; init; }

    public bool? Capture { get; init; }

    public CardRequest? Card { get; init; }
}
