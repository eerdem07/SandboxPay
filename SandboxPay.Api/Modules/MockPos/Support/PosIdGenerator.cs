using System.Globalization;
using System.Security.Cryptography;

namespace SandboxPay.Api.Modules.MockPos.Support;

public interface IPosIdGenerator
{
    string GeneratePosTransactionId();

    string GenerateAuthCode();

    string GenerateHostReferenceNumber(DateTimeOffset timestamp);
}

public sealed class PosIdGenerator : IPosIdGenerator
{
    private const string HostReferenceAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public string GeneratePosTransactionId()
    {
        return $"pos_txn_{Convert.ToHexString(RandomNumberGenerator.GetBytes(5)).ToLowerInvariant()}";
    }

    public string GenerateAuthCode()
    {
        return $"A{RandomNumberGenerator.GetInt32(100000).ToString("D5", CultureInfo.InvariantCulture)}";
    }

    public string GenerateHostReferenceNumber(DateTimeOffset timestamp)
    {
        return $"HST{timestamp.ToUniversalTime().ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture)}{GenerateSuffix(4)}";
    }

    private static string GenerateSuffix(int length)
    {
        return string.Create(
            length,
            HostReferenceAlphabet,
            static (characters, alphabet) =>
            {
                for (var index = 0; index < characters.Length; index++)
                {
                    characters[index] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
                }
            });
    }
}
