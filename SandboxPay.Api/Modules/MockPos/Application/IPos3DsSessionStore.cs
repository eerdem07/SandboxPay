namespace SandboxPay.Api.Modules.MockPos.Application;

public interface IPos3DsSessionStore
{
    void Save(Pos3DsSession session);

    bool TryGet(string sessionId, out Pos3DsSession? session);

    bool TryMarkCompleted(string sessionId, out Pos3DsSession? session);
}
