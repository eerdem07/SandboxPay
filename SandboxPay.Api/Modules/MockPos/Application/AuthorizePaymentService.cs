using System.Globalization;
using SandboxPay.Api.Modules.MockPos.Domain;
using SandboxPay.Api.Modules.MockPos.Support;

namespace SandboxPay.Api.Modules.MockPos.Application;

public interface IAuthorizePaymentService
{
    AuthorizePaymentResult Authorize(AuthorizePaymentCommand command);
}

public sealed class AuthorizePaymentService(IPosIdGenerator posIdGenerator) : IAuthorizePaymentService
{
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

        return new AuthorizePaymentResult(
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
    }
}
