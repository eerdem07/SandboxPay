namespace SandboxPay.Api.Modules.MockPos.Domain;

public enum ThreeDsFlow
{
    FRICTIONLESS,
    CHALLENGE,
    ATTEMPTED,
    TIMEOUT
}
