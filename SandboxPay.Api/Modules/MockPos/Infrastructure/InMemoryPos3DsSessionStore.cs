using System.Collections.Concurrent;
using SandboxPay.Api.Modules.MockPos.Application;

namespace SandboxPay.Api.Modules.MockPos.Infrastructure;

public sealed class InMemoryPos3DsSessionStore : IPos3DsSessionStore
{
    private readonly ConcurrentDictionary<string, Pos3DsSession> sessions = new(StringComparer.Ordinal);

    public void Save(Pos3DsSession session)
    {
        sessions[session.ThreeDsSessionId] = session;
    }

    public bool TryGet(string sessionId, out Pos3DsSession? session)
    {
        return sessions.TryGetValue(sessionId, out session);
    }

    public bool TryMarkCompleted(string sessionId, out Pos3DsSession? session)
    {
        while (sessions.TryGetValue(sessionId, out var current))
        {
            if (current.Completed)
            {
                session = null;
                return false;
            }

            var completed = current with { Completed = true };
            if (sessions.TryUpdate(sessionId, completed, current))
            {
                session = completed;
                return true;
            }
        }

        session = null;
        return false;
    }
}
