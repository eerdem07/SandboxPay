namespace SandboxPay.Api.Modules.MockPos.Application;

public sealed record Complete3DsCommand(
    string MerchantId,
    string TerminalId,
    string OrderId,
    string TransactionId,
    string ThreeDsSessionId);
