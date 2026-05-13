using System.Globalization;
using SandboxPay.Api.Modules.MockPos.Domain;
using SandboxPay.Api.Modules.MockPos.Support;

namespace SandboxPay.Api.Modules.MockPos.Application;

public interface IRefundPaymentService
{
    RefundPaymentResult Refund(RefundPaymentCommand command);
}

public sealed class RefundPaymentService(
    IPosAuthorizationStore authorizationStore,
    IPosIdGenerator posIdGenerator) : IRefundPaymentService
{
    public RefundPaymentResult Refund(RefundPaymentCommand command)
    {
        var refundedAt = DateTimeOffset.UtcNow;

        if (!authorizationStore.TryGet(command.OriginalTransactionId, out var authorization))
        {
            return Failure(command, PosResponseCode.InvalidTransaction, refundedAt);
        }

        if (!MatchesIdentifiers(command, authorization)
            || !authorization.Captured
            || authorization.Voided
            || authorization.Refunded
            || authorization.AuthorizationExpiresAt < refundedAt)
        {
            return Failure(command, PosResponseCode.InvalidTransaction, refundedAt);
        }

        if (!MatchesAmountAndCurrency(command, authorization))
        {
            return Failure(command, PosResponseCode.InvalidAmount, refundedAt);
        }

        var posRefundId = posIdGenerator.GeneratePosRefundId();
        var hostReferenceNumber = posIdGenerator.GenerateHostReferenceNumber(refundedAt);

        if (!authorizationStore.TryMarkRefunded(
                command.OriginalTransactionId,
                command.OriginalPosTransactionId,
                out _))
        {
            return Failure(command, PosResponseCode.InvalidTransaction, refundedAt);
        }

        return new RefundPaymentResult(
            PosRefundStatus.REFUNDED,
            PosTransactionType.REFUND,
            Approved: true,
            PosResponseCode.Approved.Code,
            "Refund approved",
            command.TransactionId,
            command.OriginalTransactionId,
            posRefundId,
            command.OriginalPosTransactionId,
            hostReferenceNumber,
            command.Amount,
            command.Currency,
            FormatTimestamp(refundedAt));
    }

    private static RefundPaymentResult Failure(
        RefundPaymentCommand command,
        PosResponseCode responseCode,
        DateTimeOffset refundedAt)
    {
        return new RefundPaymentResult(
            PosRefundStatus.FAILED,
            PosTransactionType.REFUND,
            Approved: false,
            responseCode.Code,
            responseCode.Message,
            command.TransactionId,
            command.OriginalTransactionId,
            PosRefundId: null,
            command.OriginalPosTransactionId,
            HostReferenceNumber: null,
            command.Amount,
            command.Currency,
            RefundedAt: null);
    }

    private static bool MatchesIdentifiers(RefundPaymentCommand command, PosAuthorization authorization)
    {
        return string.Equals(command.MerchantId, authorization.MerchantId, StringComparison.Ordinal)
            && string.Equals(command.TerminalId, authorization.TerminalId, StringComparison.Ordinal)
            && string.Equals(command.OrderId, authorization.OrderId, StringComparison.Ordinal)
            && string.Equals(command.OriginalTransactionId, authorization.TransactionId, StringComparison.Ordinal)
            && string.Equals(command.OriginalPosTransactionId, authorization.PosTransactionId, StringComparison.Ordinal)
            && string.Equals(command.AuthCode, authorization.AuthCode, StringComparison.Ordinal)
            && string.Equals(command.HostReferenceNumber, authorization.HostReferenceNumber, StringComparison.Ordinal);
    }

    private static bool MatchesAmountAndCurrency(RefundPaymentCommand command, PosAuthorization authorization)
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

    private static string FormatTimestamp(DateTimeOffset timestamp)
    {
        return timestamp.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
    }
}
