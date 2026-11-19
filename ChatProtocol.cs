namespace Connectly;

internal static class ChatProtocol
{
    public const int DefaultPort = 5000;
    public const int MaxUserNameLength = 24;
    public const int MaxMessageLength = 500;
    public const int MaxLineLength = MaxMessageLength + MaxUserNameLength + 64;

    public const string Hello = "HELLO";
    public const string Accepted = "OK";
    public const string Error = "ERROR";
    public const string Send = "MSG";
    public const string Message = "CHAT";
    public const string Joined = "JOIN";
    public const string Left = "LEAVE";

    public static bool IsValidPort(int port) => port is >= 1 and <= 65535;

    public static void ValidatePort(int port)
    {
        if (!IsValidPort(port))
            throw new ArgumentOutOfRangeException(nameof(port), "Port 1 ile 65535 arasında olmalıdır.");
    }

    // Kontrol karakterleri satır sonunu veya alan ayırıcısını taklit edebilir.
    public static bool IsValidText(string value, int maxLength) =>
        value.Length >= 1 && value.Length <= maxLength && !value.Any(char.IsControl);
}
