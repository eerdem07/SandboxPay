namespace SandboxPay.Api.Modules.MockPos.Web.Contracts;

public sealed class Complete3DsRequest
{
    public string? MerchantId { get; init; }

    public string? TerminalId { get; init; }

    public string? OrderId { get; init; }

    public string? TransactionId { get; init; }

    public string? ThreeDsSessionId { get; init; }
}
