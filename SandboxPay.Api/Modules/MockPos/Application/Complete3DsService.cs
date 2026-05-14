using SandboxPay.Api.Modules.MockPos.Domain;
using SandboxPay.Api.Modules.MockPos.Support;

namespace SandboxPay.Api.Modules.MockPos.Application;

public interface IComplete3DsService
{
    Complete3DsResult Complete(Complete3DsCommand command);
}

public sealed class Complete3DsService(
    IPos3DsSessionStore sessionStore,
    IPosAuthorizationStore authorizationStore,
    IPosIdGenerator posIdGenerator) : IComplete3DsService
{
    public Complete3DsResult Complete(Complete3DsCommand command)
    {
        var completedAt = DateTimeOffset.UtcNow;

        if (!sessionStore.TryGet(command.ThreeDsSessionId, out var session) || session is null)
        {
            return NotFoundFailure(command, completedAt);
        }

        if (!MatchesIdentifiers(command, session))
        {
            return Failure(command, session, PosResponseCode.InvalidTransaction, null, null, completedAt);
        }

        if (session.ExpiresAt < completedAt)
        {
            return Failure(command, session, PosResponseCode.ThreeDsTimeout, ThreeDsStatus.EXPIRED, null, completedAt);
        }

        if (!sessionStore.TryMarkCompleted(command.ThreeDsSessionId, out _))
        {
            return Failure(command, session, PosResponseCode.InvalidTransaction, null, null, completedAt);
        }

        return session.Scenario switch
        {
            ThreeDsScenario.FRICTIONLESS_APPROVED  => HandleApproved(command, session, ThreeDsStatus.AUTHENTICATED, "05", completedAt),
            ThreeDsScenario.CHALLENGE_APPROVED      => HandleApproved(command, session, ThreeDsStatus.AUTHENTICATED, "05", completedAt),
            ThreeDsScenario.UNAVAILABLE_ATTEMPTED   => HandleApproved(command, session, ThreeDsStatus.ATTEMPTED, "06", completedAt),
            ThreeDsScenario.CHALLENGE_FAILED_AUTH   => HandleAuthFailed(command, session, completedAt),
            ThreeDsScenario.CHALLENGE_TIMEOUT       => HandleTimeout(command, session, completedAt),
            ThreeDsScenario.FRICTIONLESS_DECLINED   => HandleFrictionlessDeclined(command, session, completedAt),
            _                                       => Failure(command, session, PosResponseCode.InvalidTransaction, null, null, completedAt)
        };
    }

    private Complete3DsResult HandleApproved(
        Complete3DsCommand command,
        Pos3DsSession session,
        ThreeDsStatus threeDsStatus,
        string eci,
        DateTimeOffset completedAt)
    {
        var posTransactionId = posIdGenerator.GeneratePosTransactionId();
        var authCode = posIdGenerator.GenerateAuthCode();
        var hostReferenceNumber = posIdGenerator.GenerateHostReferenceNumber(completedAt);
        var status = session.Capture ? PosAuthorizeStatus.APPROVED : PosAuthorizeStatus.AUTHORIZED;
        var transactionType = session.Capture ? PosTransactionType.SALE : PosTransactionType.AUTHORIZATION_ONLY;

        authorizationStore.Save(new PosAuthorization(
            session.MerchantId,
            session.TerminalId,
            session.OrderId,
            session.TransactionId,
            posTransactionId,
            authCode,
            hostReferenceNumber,
            session.Amount,
            session.Currency,
            session.InstallmentCount,
            completedAt,
            completedAt.Add(PosConstants.AuthorizationTtl),
            Captured: session.Capture,
            Voided: false,
            Refunded: false));

        var installmentAmount = PosConstants.CalculateInstallmentAmount(session.Amount, session.InstallmentCount);

        return new Complete3DsResult(
            status,
            transactionType,
            Approved: true,
            PosResponseCode.Approved.Code,
            PosResponseCode.Approved.Message,
            command.TransactionId,
            session.TransactionId,
            posTransactionId,
            authCode,
            hostReferenceNumber,
            session.Amount,
            session.Currency,
            session.InstallmentCount,
            installmentAmount,
            PosConstants.FormatTimestamp(completedAt),
            command.ThreeDsSessionId,
            threeDsStatus,
            eci,
            PosConstants.ThreeDsMessageVersion);
    }

    private Complete3DsResult HandleFrictionlessDeclined(
        Complete3DsCommand command,
        Pos3DsSession session,
        DateTimeOffset completedAt)
    {
        var transactionType = session.Capture ? PosTransactionType.SALE : PosTransactionType.AUTHORIZATION_ONLY;
        var posTransactionId = posIdGenerator.GeneratePosTransactionId();
        var hostReferenceNumber = posIdGenerator.GenerateHostReferenceNumber(completedAt);

        return new Complete3DsResult(
            PosAuthorizeStatus.DECLINED,
            transactionType,
            Approved: false,
            PosResponseCode.DoNotHonor.Code,
            PosResponseCode.DoNotHonor.Message,
            command.TransactionId,
            session.TransactionId,
            posTransactionId,
            AuthCode: null,
            hostReferenceNumber,
            session.Amount,
            session.Currency,
            session.InstallmentCount,
            InstallmentAmount: null,
            PosConstants.FormatTimestamp(completedAt),
            command.ThreeDsSessionId,
            ThreeDsStatus.AUTHENTICATED,
            Eci: "05",
            PosConstants.ThreeDsMessageVersion);
    }

    private static Complete3DsResult HandleAuthFailed(
        Complete3DsCommand command,
        Pos3DsSession session,
        DateTimeOffset completedAt)
    {
        var transactionType = session.Capture ? PosTransactionType.SALE : PosTransactionType.AUTHORIZATION_ONLY;

        return new Complete3DsResult(
            PosAuthorizeStatus.DECLINED,
            transactionType,
            Approved: false,
            PosResponseCode.ThreeDsAuthFailed.Code,
            PosResponseCode.ThreeDsAuthFailed.Message,
            command.TransactionId,
            session.TransactionId,
            PosTransactionId: null,
            AuthCode: null,
            HostReferenceNumber: null,
            session.Amount,
            session.Currency,
            session.InstallmentCount,
            InstallmentAmount: null,
            PosConstants.FormatTimestamp(completedAt),
            command.ThreeDsSessionId,
            ThreeDsStatus.FAILED,
            Eci: null,
            PosConstants.ThreeDsMessageVersion);
    }

    private static Complete3DsResult HandleTimeout(
        Complete3DsCommand command,
        Pos3DsSession session,
        DateTimeOffset completedAt)
    {
        var transactionType = session.Capture ? PosTransactionType.SALE : PosTransactionType.AUTHORIZATION_ONLY;

        return new Complete3DsResult(
            PosAuthorizeStatus.FAILED,
            transactionType,
            Approved: false,
            PosResponseCode.ThreeDsTimeout.Code,
            PosResponseCode.ThreeDsTimeout.Message,
            command.TransactionId,
            session.TransactionId,
            PosTransactionId: null,
            AuthCode: null,
            HostReferenceNumber: null,
            session.Amount,
            session.Currency,
            session.InstallmentCount,
            InstallmentAmount: null,
            PosConstants.FormatTimestamp(completedAt),
            command.ThreeDsSessionId,
            ThreeDsStatus.EXPIRED,
            Eci: null,
            PosConstants.ThreeDsMessageVersion);
    }

    private static Complete3DsResult Failure(
        Complete3DsCommand command,
        Pos3DsSession session,
        PosResponseCode responseCode,
        ThreeDsStatus? threeDsStatus,
        string? eci,
        DateTimeOffset completedAt)
    {
        var transactionType = session.Capture ? PosTransactionType.SALE : PosTransactionType.AUTHORIZATION_ONLY;

        return new Complete3DsResult(
            PosAuthorizeStatus.FAILED,
            transactionType,
            Approved: false,
            responseCode.Code,
            responseCode.Message,
            command.TransactionId,
            session.TransactionId,
            PosTransactionId: null,
            AuthCode: null,
            HostReferenceNumber: null,
            session.Amount,
            session.Currency,
            session.InstallmentCount,
            InstallmentAmount: null,
            PosConstants.FormatTimestamp(completedAt),
            command.ThreeDsSessionId,
            threeDsStatus,
            eci,
            PosConstants.ThreeDsMessageVersion);
    }

    private static Complete3DsResult NotFoundFailure(Complete3DsCommand command, DateTimeOffset completedAt)
    {
        return new Complete3DsResult(
            PosAuthorizeStatus.FAILED,
            PosTransactionType.AUTHORIZATION_ONLY,
            Approved: false,
            PosResponseCode.InvalidTransaction.Code,
            PosResponseCode.InvalidTransaction.Message,
            command.TransactionId,
            OriginalTransactionId: null,
            PosTransactionId: null,
            AuthCode: null,
            HostReferenceNumber: null,
            Amount: null,
            Currency: null,
            InstallmentCount: null,
            InstallmentAmount: null,
            PosConstants.FormatTimestamp(completedAt),
            command.ThreeDsSessionId,
            ThreeDsStatus: null,
            Eci: null,
            MessageVersion: null);
    }

    private static bool MatchesIdentifiers(Complete3DsCommand command, Pos3DsSession session)
    {
        return string.Equals(command.MerchantId, session.MerchantId, StringComparison.Ordinal)
            && string.Equals(command.TerminalId, session.TerminalId, StringComparison.Ordinal)
            && string.Equals(command.OrderId, session.OrderId, StringComparison.Ordinal);
    }
}
