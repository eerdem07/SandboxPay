namespace SandboxPay.Api.Modules.MockPos.Domain;

public enum PosAuthorizeStatus
{
    APPROVED,
    AUTHORIZED,
    PENDING_3DS,
    DECLINED,
    FAILED
}
