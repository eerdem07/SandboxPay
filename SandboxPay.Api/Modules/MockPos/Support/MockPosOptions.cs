namespace SandboxPay.Api.Modules.MockPos.Support;

public sealed class MockPosOptions
{
    public string AcsBaseUrl { get; init; } = "http://localhost:5102/mock-acs";
}
