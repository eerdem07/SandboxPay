namespace SandboxPay.Api.Modules.MockPos.Domain;

public sealed record PosResponseCode(string Code, string Message, PosAuthorizeStatus Status)
{
    public static readonly PosResponseCode Approved = new("00", "Approved", PosAuthorizeStatus.APPROVED);
    public static readonly PosResponseCode DoNotHonor = new("05", "Do not honor", PosAuthorizeStatus.DECLINED);
    public static readonly PosResponseCode InvalidTransaction = new("12", "Invalid transaction", PosAuthorizeStatus.FAILED);
    public static readonly PosResponseCode InvalidAmount = new("13", "Invalid amount", PosAuthorizeStatus.FAILED);
    public static readonly PosResponseCode InvalidCardNumber = new("14", "Invalid card number", PosAuthorizeStatus.DECLINED);
    public static readonly PosResponseCode FormatError = new("30", "Format error", PosAuthorizeStatus.FAILED);
    public static readonly PosResponseCode LostCard = new("41", "Lost card", PosAuthorizeStatus.DECLINED);
    public static readonly PosResponseCode StolenCard = new("43", "Stolen card", PosAuthorizeStatus.DECLINED);
    public static readonly PosResponseCode InsufficientFunds = new("51", "Insufficient funds", PosAuthorizeStatus.DECLINED);
    public static readonly PosResponseCode ExpiredCard = new("54", "Expired card", PosAuthorizeStatus.DECLINED);
    public static readonly PosResponseCode TransactionNotPermittedToCardholder = new("57", "Transaction not permitted to cardholder", PosAuthorizeStatus.DECLINED);
    public static readonly PosResponseCode TransactionNotPermittedToTerminal = new("58", "Transaction not permitted to terminal", PosAuthorizeStatus.FAILED);
    public static readonly PosResponseCode ExceedsAmountLimit = new("61", "Exceeds amount limit", PosAuthorizeStatus.DECLINED);
    public static readonly PosResponseCode ExceedsFrequencyLimit = new("65", "Exceeds frequency limit", PosAuthorizeStatus.DECLINED);
    public static readonly PosResponseCode RestrictedCard = new("62", "Restricted card", PosAuthorizeStatus.DECLINED);
    public static readonly PosResponseCode IssuerUnavailable = new("91", "Issuer or switch unavailable", PosAuthorizeStatus.FAILED);
    public static readonly PosResponseCode SystemMalfunction = new("96", "System malfunction", PosAuthorizeStatus.FAILED);
    public static readonly PosResponseCode Timeout = new("TIMEOUT", "Bank POS timeout", PosAuthorizeStatus.FAILED);
    public static readonly PosResponseCode Pending = new("PENDING", "3DS authentication required", PosAuthorizeStatus.PENDING_3DS);
    public static readonly PosResponseCode ThreeDsAuthFailed = new("3DS_AUTH_FAILED", "3DS authentication failed", PosAuthorizeStatus.DECLINED);
    public static readonly PosResponseCode ThreeDsTimeout = new("3DS_TIMEOUT", "3DS session expired", PosAuthorizeStatus.FAILED);

    private static readonly IReadOnlyDictionary<string, PosResponseCode> Codes =
        new Dictionary<string, PosResponseCode>(StringComparer.Ordinal)
        {
            [Approved.Code] = Approved,
            [DoNotHonor.Code] = DoNotHonor,
            [InvalidTransaction.Code] = InvalidTransaction,
            [InvalidAmount.Code] = InvalidAmount,
            [InvalidCardNumber.Code] = InvalidCardNumber,
            [FormatError.Code] = FormatError,
            [LostCard.Code] = LostCard,
            [StolenCard.Code] = StolenCard,
            [InsufficientFunds.Code] = InsufficientFunds,
            [ExpiredCard.Code] = ExpiredCard,
            [TransactionNotPermittedToCardholder.Code] = TransactionNotPermittedToCardholder,
            [TransactionNotPermittedToTerminal.Code] = TransactionNotPermittedToTerminal,
            [ExceedsAmountLimit.Code] = ExceedsAmountLimit,
            [ExceedsFrequencyLimit.Code] = ExceedsFrequencyLimit,
            [RestrictedCard.Code] = RestrictedCard,
            [IssuerUnavailable.Code] = IssuerUnavailable,
            [SystemMalfunction.Code] = SystemMalfunction,
            [Timeout.Code] = Timeout,
            [Pending.Code] = Pending,
            [ThreeDsAuthFailed.Code] = ThreeDsAuthFailed,
            [ThreeDsTimeout.Code] = ThreeDsTimeout
        };

    public bool IsApproval => Code == Approved.Code;

    public PosAuthorizeStatus ResolveStatus(bool capture)
    {
        if (!IsApproval)
        {
            return Status;
        }

        return capture ? PosAuthorizeStatus.APPROVED : PosAuthorizeStatus.AUTHORIZED;
    }

    public static PosResponseCode FromCode(string code)
    {
        return Codes.TryGetValue(code, out var responseCode) ? responseCode : FormatError;
    }
}
