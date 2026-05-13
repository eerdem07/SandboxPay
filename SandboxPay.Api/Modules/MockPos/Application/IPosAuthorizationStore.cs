namespace SandboxPay.Api.Modules.MockPos.Application;

public interface IPosAuthorizationStore
{
    void Save(PosAuthorization authorization);

    bool TryGet(string transactionId, out PosAuthorization authorization);

    bool TryMarkCaptured(string transactionId, string posTransactionId, out PosAuthorization? authorization);

    bool TryMarkVoided(string transactionId, string posTransactionId, out PosAuthorization? authorization);

    bool TryMarkRefunded(string transactionId, string posTransactionId, out PosAuthorization? authorization);
}
