using System.Globalization;
using SandboxPay.Api.Modules.MockPos.Domain;
using SandboxPay.Api.Modules.MockPos.Support;

namespace SandboxPay.Api.Modules.MockPos.Application;

public interface IVoidPaymentService
{
    VoidPaymentResult Void(VoidPaymentCommand command);
}

public sealed class VoidPaymentService(
    IPosAuthorizationStore authorizationStore,
    IPosIdGenerator posIdGenerator) : IVoidPaymentService
{
    public VoidPaymentResult Void(VoidPaymentCommand command)
    {
        var voidedAt = DateTimeOffset.UtcNow;

        if (!authorizationStore.TryGet(command.OriginalTransactionId, out var authorization) || authorization is null)
        {
            return Failure(command, PosResponseCode.InvalidTransaction, voidedAt);
        }

        if (!MatchesIdentifiers(command, authorization)
            || authorization.Captured
            || authorization.Voided
            || authorization.AuthorizationExpiresAt < voidedAt)
        {
            return Failure(command, PosResponseCode.InvalidTransaction, voidedAt);
        }

        if (!MatchesAmountAndCurrency(command, authorization))
        {
            return Failure(command, PosResponseCode.InvalidAmount, voidedAt);
        }

        var posVoidId = posIdGenerator.GeneratePosVoidId();
        var hostReferenceNumber = posIdGenerator.GenerateHostReferenceNumber(voidedAt);

        if (!authorizationStore.TryMarkVoided(
                command.OriginalTransactionId,
                command.OriginalPosTransactionId,
                out _))
        {
            return Failure(command, PosResponseCode.InvalidTransaction, voidedAt);
        }

        return new VoidPaymentResult(
            PosVoidStatus.VOIDED,
            PosTransactionType.VOID,
            Approved: true,
            PosResponseCode.Approved.Code,
            "Void approved",
            command.TransactionId,
            command.OriginalTransactionId,
            posVoidId,
            command.OriginalPosTransactionId,
            hostReferenceNumber,
            command.Amount,
            command.Currency,
            PosConstants.FormatTimestamp(voidedAt));
    }

    private static VoidPaymentResult Failure(
        VoidPaymentCommand command,
        PosResponseCode responseCode,
        DateTimeOffset voidedAt)
    {
        return new VoidPaymentResult(
            PosVoidStatus.FAILED,
            PosTransactionType.VOID,
            Approved: false,
            responseCode.Code,
            responseCode.Message,
            command.TransactionId,
            command.OriginalTransactionId,
            PosVoidId: null,
            command.OriginalPosTransactionId,
            HostReferenceNumber: null,
            command.Amount,
            command.Currency,
            VoidedAt: null);
    }

    private static bool MatchesIdentifiers(VoidPaymentCommand command, PosAuthorization authorization)
    {
        return string.Equals(command.MerchantId, authorization.MerchantId, StringComparison.Ordinal)
            && string.Equals(command.TerminalId, authorization.TerminalId, StringComparison.Ordinal)
            && string.Equals(command.OrderId, authorization.OrderId, StringComparison.Ordinal)
            && string.Equals(command.OriginalTransactionId, authorization.TransactionId, StringComparison.Ordinal)
            && string.Equals(command.OriginalPosTransactionId, authorization.PosTransactionId, StringComparison.Ordinal)
            && string.Equals(command.AuthCode, authorization.AuthCode, StringComparison.Ordinal)
            && string.Equals(command.HostReferenceNumber, authorization.HostReferenceNumber, StringComparison.Ordinal);
    }

    private static bool MatchesAmountAndCurrency(VoidPaymentCommand command, PosAuthorization authorization)
    {
        return AmountsMatch(command.Amount, authorization.Amount)
            && string.Equals(command.Currency, authorization.Currency, StringComparison.Ordinal);
    }

    private static bool AmountsMatch(string requestedAmount, string authorizedAmount)
    {
        return decimal.TryParse(
                requestedAmount,
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out var requested)
            && decimal.TryParse(
                authorizedAmount,
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out var authorized)
            && requested == authorized;
    }
}
