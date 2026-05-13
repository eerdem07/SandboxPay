namespace SandboxPay.Api.Modules.MockPos.Domain;

public enum PosTransactionType
{
    SALE,
    AUTHORIZATION_ONLY,
    CAPTURE,
    VOID,
    REFUND
}
