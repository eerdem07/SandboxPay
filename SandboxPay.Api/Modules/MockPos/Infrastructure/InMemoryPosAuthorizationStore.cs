using System.Collections.Concurrent;
using SandboxPay.Api.Modules.MockPos.Application;

namespace SandboxPay.Api.Modules.MockPos.Infrastructure;

public sealed class InMemoryPosAuthorizationStore : IPosAuthorizationStore
{
    private readonly ConcurrentDictionary<string, PosAuthorization> authorizations = new(StringComparer.Ordinal);

    public void Save(PosAuthorization authorization)
    {
        authorizations[authorization.TransactionId] = authorization;
    }

    public bool TryGet(string transactionId, out PosAuthorization authorization)
    {
        return authorizations.TryGetValue(transactionId, out authorization!);
    }

    public bool TryMarkCaptured(string transactionId, string posTransactionId, out PosAuthorization? authorization)
    {
        while (authorizations.TryGetValue(transactionId, out var current))
        {
            if (!string.Equals(current.PosTransactionId, posTransactionId, StringComparison.Ordinal))
            {
                authorization = current;
                return false;
            }

            if (current.Captured || current.Voided)
            {
                authorization = current;
                return false;
            }

            var captured = current with { Captured = true };
            if (authorizations.TryUpdate(transactionId, captured, current))
            {
                authorization = captured;
                return true;
            }
        }

        authorization = null;
        return false;
    }

    public bool TryMarkVoided(string transactionId, string posTransactionId, out PosAuthorization? authorization)
    {
        while (authorizations.TryGetValue(transactionId, out var current))
        {
            if (!string.Equals(current.PosTransactionId, posTransactionId, StringComparison.Ordinal))
            {
                authorization = current;
                return false;
            }

            if (current.Captured || current.Voided)
            {
                authorization = current;
                return false;
            }

            var voided = current with { Voided = true };
            if (authorizations.TryUpdate(transactionId, voided, current))
            {
                authorization = voided;
                return true;
            }
        }

        authorization = null;
        return false;
    }

    public bool TryMarkRefunded(string transactionId, string posTransactionId, out PosAuthorization? authorization)
    {
        while (authorizations.TryGetValue(transactionId, out var current))
        {
            if (!string.Equals(current.PosTransactionId, posTransactionId, StringComparison.Ordinal))
            {
                authorization = current;
                return false;
            }

            if (!current.Captured || current.Voided || current.Refunded)
            {
                authorization = current;
                return false;
            }

            var refunded = current with { Refunded = true };
            if (authorizations.TryUpdate(transactionId, refunded, current))
            {
                authorization = refunded;
                return true;
            }
        }

        authorization = null;
        return false;
    }
}
