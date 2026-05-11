namespace SandboxPay.Api.Modules.MockPos.Domain;

public static class LuhnValidator
{
    public static bool IsValid(string normalizedPan)
    {
        if (string.IsNullOrWhiteSpace(normalizedPan))
        {
            return false;
        }

        var sum = 0;
        var shouldDouble = false;

        for (var index = normalizedPan.Length - 1; index >= 0; index--)
        {
            var character = normalizedPan[index];
            if (!char.IsDigit(character))
            {
                return false;
            }

            var digit = character - '0';
            if (shouldDouble)
            {
                digit *= 2;
                if (digit > 9)
                {
                    digit -= 9;
                }
            }

            sum += digit;
            shouldDouble = !shouldDouble;
        }

        return sum % 10 == 0;
    }
}
