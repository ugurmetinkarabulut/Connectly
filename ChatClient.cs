using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace Connectly;

internal sealed class ChatClient
{
    private readonly SemaphoreSlim _lifecycleLock = new(1);
    private readonly SemaphoreSlim _sendLock = new(1);
    private volatile ChatConnection? _connection;
    private Task? _receiveTask;

    public bool IsConnected => _connection is { IsClosed: false };

    public event Action<ChatMessage, bool>? MessageReceived;
    public event Action<string>? NoticeReceived;
    public event Action<string?>? ConnectionClosed;

    public async Task ConnectAsync(
        string userName, IPAddress address, int port, CancellationToken cancellationToken = default)
    {
        ChatProtocol.ValidatePort(port);
        userName = userName.Trim();
        if (!ChatProtocol.IsValidText(userName, ChatProtocol.MaxUserNameLength))
            throw new ArgumentException("Kullanıcı adı 1–24 karakter olmalıdır ve kontrol karakteri içeremez.");

        await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (IsConnected)
                throw new InvalidOperationException("Sunucuya zaten bağlısınız.");

            await DisconnectCoreAsync().ConfigureAwait(false);
            ChatConnection connection = await OpenConnectionAsync(userName, address, port, cancellationToken)
                .ConfigureAwait(false);

            _connection = connection;
            _receiveTask = ReceiveAsync(connection, userName);
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    private static async Task<ChatConnection> OpenConnectionAsync(
        string userName, IPAddress address, int port, CancellationToken cancellationToken)
    {
        var tcpClient = new TcpClient(address.AddressFamily);
        ChatConnection? connection = null;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));

        try
        {
            await tcpClient.ConnectAsync(address, port, timeout.Token).ConfigureAwait(false);
            connection = new ChatConnection(tcpClient);
            await connection.WriteLineAsync($"{ChatProtocol.Hello}\t{userName}", timeout.Token)
                .ConfigureAwait(false);

            string? response = await connection.ReadLineAsync(timeout.Token).ConfigureAwait(false);
            if (response == ChatProtocol.Accepted)
                return connection;

            string errorPrefix = ChatProtocol.Error + "\t";
            if (response?.StartsWith(errorPrefix, StringComparison.Ordinal) == true)
                throw new IOException(response[errorPrefix.Length..]);

            throw new IOException("Sunucu bağlantı isteğine geçerli bir yanıt vermedi.");
        }
        catch
        {
            // Bağlantı onaylanmadan oluşan hatalarda da soket kapatılır.
            connection?.Dispose();
            tcpClient.Dispose();
            throw;
        }
    }

    public async Task SendAsync(string message)
    {
        message = message.Trim();
        if (!ChatProtocol.IsValidText(message, ChatProtocol.MaxMessageLength))
            throw new ArgumentException("Mesaj 1–500 karakter olmalıdır ve kontrol karakteri içeremez.");

        ChatConnection? connection = _connection;
        await _sendLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (connection is not { IsClosed: false })
                throw new InvalidOperationException("Mesaj göndermek için sunucuya bağlanın.");

            await connection.WriteLineAsync($"{ChatProtocol.Send}\t{message}").ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is IOException or SocketException or ObjectDisposedException)
        {
            connection?.Close();
            throw new IOException("Mesaj gönderilemedi. Bağlantı kapatıldı.", exception);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    public async Task DisconnectAsync()
    {
        await _lifecycleLock.WaitAsync().ConfigureAwait(false);
        try
        {
            await DisconnectCoreAsync().ConfigureAwait(false);
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    private async Task DisconnectCoreAsync()
    {
        _connection?.Close();
        if (_receiveTask is not null)
            await _receiveTask.ConfigureAwait(false);

        _connection = null;
        _receiveTask = null;
    }

    private async Task ReceiveAsync(ChatConnection connection, string userName)
    {
        string reason = "Sunucu bağlantıyı kapattı.";
        try
        {
            while (!connection.IsClosed)
            {
                string? line = await connection.ReadLineAsync().ConfigureAwait(false);
                if (line is null)
                    break;

                HandleLine(line, userName);
            }
        }
        catch (InvalidDataException)
        {
            reason = "Sunucudan geçersiz veri alındı.";
        }
        catch (Exception exception) when (exception is IOException or SocketException or ObjectDisposedException)
        {
            reason = "Sunucuyla bağlantı kesildi.";
        }
        finally
        {
            bool closedLocally = connection.IsClosed;
            connection.Close();

            // Soketi kapatmak yazmayı keser. Akışları bırakmadan önce yazma işlemini bekle.
            await _sendLock.WaitAsync().ConfigureAwait(false);
            try
            {
                connection.Dispose();
            }
            finally
            {
                _sendLock.Release();
            }

            ConnectionClosed?.Invoke(closedLocally ? null : reason);
        }
    }

    private void HandleLine(string line, string userName)
    {
        string[] parts = line.Split('\t', 4);
        if (parts.Length == 4 && parts[0] == ChatProtocol.Message &&
            DateTimeOffset.TryParseExact(parts[1], "O", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateTimeOffset sentAt) &&
            ChatProtocol.IsValidText(parts[2], ChatProtocol.MaxUserNameLength) &&
            ChatProtocol.IsValidText(parts[3], ChatProtocol.MaxMessageLength))
        {
            var message = new ChatMessage(parts[2], parts[3], sentAt);
            bool isOwnMessage = string.Equals(parts[2], userName, StringComparison.OrdinalIgnoreCase);
            MessageReceived?.Invoke(message, isOwnMessage);
        }
        else if (parts.Length == 2)
        {
            string? notice = parts[0] switch
            {
                ChatProtocol.Joined => $"{parts[1]} sohbete katıldı.",
                ChatProtocol.Left => $"{parts[1]} sohbetten ayrıldı.",
                ChatProtocol.Error => parts[1],
                _ => null
            };
            if (notice is not null)
                NoticeReceived?.Invoke(notice);
        }
    }
}
