using System.Globalization;
using SandboxPay.Api.Modules.MockPos.Domain;
using SandboxPay.Api.Modules.MockPos.Support;

namespace SandboxPay.Api.Modules.MockPos.Application;

public interface IAuthorizePaymentService
{
    AuthorizePaymentResult Authorize(AuthorizePaymentCommand command);
}

public sealed class AuthorizePaymentService(
    IPosIdGenerator posIdGenerator,
    IPosAuthorizationStore authorizationStore) : IAuthorizePaymentService
{
    private static readonly TimeSpan AuthorizationTtl = TimeSpan.FromDays(7);

    public AuthorizePaymentResult Authorize(AuthorizePaymentCommand command)
    {
        var normalizedPan = CardNumber.Normalize(command.CardPan);

        if (!TestCardCatalog.TryResolveResponseCode(normalizedPan, out var responseCode))
        {
            responseCode = LuhnValidator.IsValid(normalizedPan)
                ? PosResponseCode.Approved
                : PosResponseCode.InvalidCardNumber;
        }

        var transactionType = command.Capture
            ? PosTransactionType.SALE
            : PosTransactionType.AUTHORIZATION_ONLY;
        var status = responseCode.ResolveStatus(command.Capture);
        var approved = responseCode.IsApproval;
        var authorizedAt = DateTimeOffset.UtcNow;
        var hasHostIdentifiers = status != PosAuthorizeStatus.FAILED;

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
            authorizedAt.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture));

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
                authorizedAt,
                authorizedAt.Add(AuthorizationTtl),
                Captured: result.TransactionType == PosTransactionType.SALE,
                Voided: false,
                Refunded: false));
        }

        return result;
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
