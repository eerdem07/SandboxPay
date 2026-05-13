namespace SandboxPay.Api.Modules.MockPos.Web.Contracts;

public sealed class CardRequest
{
    public string? HolderName { get; init; }

    public string? Pan { get; init; }

    public string? ExpiryMonth { get; init; }

    public string? ExpiryYear { get; init; }

    public string? Cvv { get; init; }
}
