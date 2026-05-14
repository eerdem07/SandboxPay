using Microsoft.Extensions.Options;
using SandboxPay.Api.Modules.MockPos.Domain;
using SandboxPay.Api.Modules.MockPos.Support;

namespace SandboxPay.Api.Modules.MockPos.Application;

public interface IAuthorizePaymentService
{
    AuthorizePaymentResult Authorize(AuthorizePaymentCommand command);
}

public sealed class AuthorizePaymentService(
    IPosIdGenerator posIdGenerator,
    IPosAuthorizationStore authorizationStore,
    IPos3DsSessionStore pos3DsSessionStore,
    IOptions<MockPosOptions> options) : IAuthorizePaymentService
{
    private readonly string acsBaseUrl = options.Value.AcsBaseUrl;

    public AuthorizePaymentResult Authorize(AuthorizePaymentCommand command)
    {
        var normalizedPan = CardNumber.Normalize(command.CardPan);

        if (ThreeDsCardCatalog.TryGet(normalizedPan, out var threeDsFlow, out var threeDsScenario))
        {
            return Initiate3Ds(command, threeDsFlow, threeDsScenario);
        }

        PosResponseCode responseCode;
        if (command.InstallmentCount >= 2
            && InstallmentCardCatalog.TryResolve(normalizedPan, command.InstallmentCount, out var installmentDeclined))
        {
            responseCode = installmentDeclined!;
        }
        else if (TestCardCatalog.TryResolveResponseCode(normalizedPan, out var catalogCode))
        {
            responseCode = catalogCode!;
        }
        else
        {
            responseCode = LuhnValidator.IsValid(normalizedPan) ? PosResponseCode.Approved : PosResponseCode.InvalidCardNumber;
        }

        var transactionType = command.Capture
            ? PosTransactionType.SALE
            : PosTransactionType.AUTHORIZATION_ONLY;
        var status = responseCode.ResolveStatus(command.Capture);
        var approved = responseCode.IsApproval;
        var authorizedAt = DateTimeOffset.UtcNow;
        var hasHostIdentifiers = status != PosAuthorizeStatus.FAILED;
        var installmentAmount = approved
            ? PosConstants.CalculateInstallmentAmount(command.Amount, command.InstallmentCount)
            : null;

        var result = new AuthorizePaymentResult(
            status,
            transactionType,
            approved,
            responseCode.Code,
            responseCode.Message,
            command.TransactionId,
            hasHostIdentifiers ? posIdGenerator.GeneratePosTransactionId() : null,
            approved ? posIdGenerator.GenerateAuthCode() : null,
            hasHostIdentifiers ? posIdGenerator.GenerateHostReferenceNumber(authorizedAt) : null,
            command.Amount,
            command.Currency,
            command.InstallmentCount,
            installmentAmount,
            PosConstants.FormatTimestamp(authorizedAt));

        if (ShouldStoreAuthorization(result))
        {
            authorizationStore.Save(new PosAuthorization(
                command.MerchantId,
                command.TerminalId,
                command.OrderId,
                command.TransactionId,
                result.PosTransactionId!,
                result.AuthCode!,
                result.HostReferenceNumber!,
                command.Amount,
                command.Currency,
                command.InstallmentCount,
                authorizedAt,
                authorizedAt.Add(PosConstants.AuthorizationTtl),
                Captured: result.TransactionType == PosTransactionType.SALE,
                Voided: false,
                Refunded: false));
        }

        return result;
    }

    private AuthorizePaymentResult Initiate3Ds(
        AuthorizePaymentCommand command,
        ThreeDsFlow flow,
        ThreeDsScenario scenario)
    {
        var initiatedAt = DateTimeOffset.UtcNow;
        var expiresAt = initiatedAt.AddMinutes(15);
        var sessionId = posIdGenerator.Generate3DsSessionId();
        var transactionType = command.Capture ? PosTransactionType.SALE : PosTransactionType.AUTHORIZATION_ONLY;

        pos3DsSessionStore.Save(new Pos3DsSession(
            sessionId,
            command.MerchantId,
            command.TerminalId,
            command.OrderId,
            command.TransactionId,
            command.Amount,
            command.Currency,
            command.InstallmentCount,
            command.Capture,
            flow,
            scenario,
            initiatedAt,
            expiresAt,
            Completed: false));

        return new AuthorizePaymentResult(
            PosAuthorizeStatus.PENDING_3DS,
            transactionType,
            Approved: false,
            PosResponseCode.Pending.Code,
            PosResponseCode.Pending.Message,
            command.TransactionId,
            PosTransactionId: null,
            AuthCode: null,
            HostReferenceNumber: null,
            command.Amount,
            command.Currency,
            command.InstallmentCount,
            InstallmentAmount: null,
            AuthorizedAt: null,
            ThreeDsSessionId: sessionId,
            AcsUrl: $"{acsBaseUrl}?sessionId={sessionId}",
            ThreeDsFlow: flow,
            MessageVersion: PosConstants.ThreeDsMessageVersion,
            ExpiresAt: PosConstants.FormatTimestamp(expiresAt));
    }

    private static bool ShouldStoreAuthorization(AuthorizePaymentResult result)
    {
        return result.Approved
            && result.TransactionType is PosTransactionType.AUTHORIZATION_ONLY or PosTransactionType.SALE
            && result.PosTransactionId is not null
            && result.AuthCode is not null
            && result.HostReferenceNumber is not null;
    }
}
