using SandboxPay.Api.Modules.MockPos.Domain;

namespace SandboxPay.Api.Modules.MockPos.Application;

public sealed record Pos3DsSession(
    string ThreeDsSessionId,
    string MerchantId,
    string TerminalId,
    string OrderId,
    string TransactionId,
    string Amount,
    string Currency,
    int InstallmentCount,
    bool Capture,
    ThreeDsFlow Flow,
    ThreeDsScenario Scenario,
    DateTimeOffset InitiatedAt,
    DateTimeOffset ExpiresAt,
    bool Completed);
