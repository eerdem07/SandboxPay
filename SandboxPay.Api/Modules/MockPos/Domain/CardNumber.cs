namespace SandboxPay.Api.Modules.MockPos.Domain;

public static class CardNumber
{
    public static string Normalize(string pan)
    {
        if (string.IsNullOrWhiteSpace(pan))
        {
            return string.Empty;
        }

        return new string(pan.Where(character => !char.IsWhiteSpace(character)).ToArray());
    }
}
