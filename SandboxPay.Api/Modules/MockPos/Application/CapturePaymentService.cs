using System.Globalization;
using SandboxPay.Api.Modules.MockPos.Domain;
using SandboxPay.Api.Modules.MockPos.Support;

namespace SandboxPay.Api.Modules.MockPos.Application;

public interface ICapturePaymentService
{
    CapturePaymentResult Capture(CapturePaymentCommand command);
}

public sealed class CapturePaymentService(
    IPosAuthorizationStore authorizationStore,
    IPosIdGenerator posIdGenerator) : ICapturePaymentService
{
    public CapturePaymentResult Capture(CapturePaymentCommand command)
    {
        var capturedAt = DateTimeOffset.UtcNow;

        if (!authorizationStore.TryGet(command.OriginalTransactionId, out var authorization) || authorization is null)
        {
            return Failure(command, PosResponseCode.InvalidTransaction, capturedAt);
        }

        if (!MatchesIdentifiers(command, authorization)
            || authorization.Captured
            || authorization.Voided
            || authorization.AuthorizationExpiresAt < capturedAt)
        {
            return Failure(command, PosResponseCode.InvalidTransaction, capturedAt);
        }

        if (!MatchesAmountAndCurrency(command, authorization))
        {
            return Failure(command, PosResponseCode.InvalidAmount, capturedAt);
        }

        var posCaptureId = posIdGenerator.GeneratePosCaptureId();
        var hostReferenceNumber = posIdGenerator.GenerateHostReferenceNumber(capturedAt);

        if (!authorizationStore.TryMarkCaptured(
                command.OriginalTransactionId,
                command.OriginalPosTransactionId,
                out _))
        {
            return Failure(command, PosResponseCode.InvalidTransaction, capturedAt);
        }

        return new CapturePaymentResult(
            PosCaptureStatus.CAPTURED,
            PosTransactionType.CAPTURE,
            Approved: true,
            PosResponseCode.Approved.Code,
            "Capture approved",
            command.TransactionId,
            command.OriginalTransactionId,
            posCaptureId,
            command.OriginalPosTransactionId,
            hostReferenceNumber,
            command.Amount,
            command.Currency,
            PosConstants.FormatTimestamp(capturedAt));
    }

    private static CapturePaymentResult Failure(
        CapturePaymentCommand command,
        PosResponseCode responseCode,
        DateTimeOffset capturedAt)
    {
        return new CapturePaymentResult(
            PosCaptureStatus.FAILED,
            PosTransactionType.CAPTURE,
            Approved: false,
            responseCode.Code,
            responseCode.Message,
            command.TransactionId,
            command.OriginalTransactionId,
            PosCaptureId: null,
            command.OriginalPosTransactionId,
            HostReferenceNumber: null,
            command.Amount,
            command.Currency,
            CapturedAt: null);
    }

    private static bool MatchesIdentifiers(CapturePaymentCommand command, PosAuthorization authorization)
    {
        return string.Equals(command.MerchantId, authorization.MerchantId, StringComparison.Ordinal)
            && string.Equals(command.TerminalId, authorization.TerminalId, StringComparison.Ordinal)
            && string.Equals(command.OrderId, authorization.OrderId, StringComparison.Ordinal)
            && string.Equals(command.OriginalTransactionId, authorization.TransactionId, StringComparison.Ordinal)
            && string.Equals(command.OriginalPosTransactionId, authorization.PosTransactionId, StringComparison.Ordinal)
            && string.Equals(command.AuthCode, authorization.AuthCode, StringComparison.Ordinal)
            && string.Equals(command.HostReferenceNumber, authorization.HostReferenceNumber, StringComparison.Ordinal);
    }

    private static bool MatchesAmountAndCurrency(CapturePaymentCommand command, PosAuthorization authorization)
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
