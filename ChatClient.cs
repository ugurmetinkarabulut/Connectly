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
    private int _connectionVersion;

    public bool IsConnected => _connection is { IsClosed: false };
    public int ConnectionVersion => Volatile.Read(ref _connectionVersion);

    public event Action<int, ChatMessage, bool>? MessageReceived;
    public event Action<int, string>? NoticeReceived;
    public event Action<int, string?>? ConnectionClosed;

    public async Task ConnectAsync(
        string userName, IPAddress address, int port, CancellationToken cancellationToken = default)
    {
        ChatProtocol.ValidatePort(port);
        userName = userName.Trim(' ');
        if (!ChatProtocol.IsValidText(userName, ChatProtocol.MaxUserNameLength))
            throw new ArgumentException("Kullanıcı adı 1–24 karakter olmalıdır ve kontrol karakteri içeremez.");

        await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (IsConnected)
                throw new InvalidOperationException("Sunucuya zaten bağlısınız.");

            // Kuyrukta kalan olaylar yeni bağlantı girişiminin arayüzünü değiştirmemeli.
            int version = Interlocked.Increment(ref _connectionVersion);
            await DisconnectCoreAsync().ConfigureAwait(false);
            ChatConnection connection = await OpenConnectionAsync(userName, address, port, cancellationToken)
                .ConfigureAwait(false);

            _connection = connection;
            _receiveTask = ReceiveAsync(connection, userName, version);
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
        bool accepted = false;
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
            {
                accepted = true;
                return connection;
            }

            string errorPrefix = ChatProtocol.Error + "\t";
            if (response?.StartsWith(errorPrefix, StringComparison.Ordinal) == true)
            {
                string error = response[errorPrefix.Length..];
                if (ChatProtocol.IsValidText(error, ChatProtocol.MaxMessageLength))
                    throw new IOException(error);
            }

            throw new IOException("Sunucu bağlantı isteğine geçerli bir yanıt vermedi.");
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("Bağlantı isteği zaman aşımına uğradı.", exception);
        }
        finally
        {
            if (!accepted)
            {
                // Bağlantı onaylanmadan oluşan hatalarda da soket kapatılır.
                if (connection is null)
                    tcpClient.Dispose();
                else
                    connection.Dispose();
            }
        }
    }

    public async Task SendAsync(string message)
    {
        message = message.Trim(' ');
        if (!ChatProtocol.IsValidText(message, ChatProtocol.MaxMessageLength))
            throw new ArgumentException("Mesaj 1–500 karakter olmalıdır ve kontrol karakteri içeremez.");

        ChatConnection? connection = _connection;
        await _sendLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (connection is not { IsClosed: false })
                throw new InvalidOperationException("Mesaj göndermek için sunucuya bağlanın.");

            // Sunucu, yavaş alıcıyı kendi 3 saniyelik yazma sınırıyla önce kapatabilmeli.
            await connection.WriteLineAsync($"{ChatProtocol.Send}\t{message}", timeoutSeconds: 6)
                .ConfigureAwait(false);
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
        ChatConnection? connection = _connection;
        Task? receiveTask = _receiveTask;
        _connection = null;
        _receiveTask = null;

        try
        {
            connection?.Close();
        }
        finally
        {
            if (receiveTask is not null)
                await receiveTask.ConfigureAwait(false);
        }
    }

    private async Task ReceiveAsync(ChatConnection connection, string userName, int version)
    {
        string reason = "Sunucu bağlantıyı kapattı.";
        try
        {
            while (!connection.IsClosed)
            {
                string? line = await connection.ReadLineAsync().ConfigureAwait(false);
                if (line is null)
                    break;

                HandleLine(line, userName, version);
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

            ConnectionClosed?.Invoke(version, closedLocally ? null : reason);
        }
    }

    private void HandleLine(string line, string userName, int version)
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
            MessageReceived?.Invoke(version, message, isOwnMessage);
        }
        else if (parts.Length == 2)
        {
            string? notice = parts[0] switch
            {
                ChatProtocol.Joined when ChatProtocol.IsValidText(parts[1], ChatProtocol.MaxUserNameLength)
                    => $"{parts[1]} sohbete katıldı.",
                ChatProtocol.Left when ChatProtocol.IsValidText(parts[1], ChatProtocol.MaxUserNameLength)
                    => $"{parts[1]} sohbetten ayrıldı.",
                ChatProtocol.Error when ChatProtocol.IsValidText(parts[1], ChatProtocol.MaxMessageLength)
                    => parts[1],
                _ => null
            };
            if (notice is not null)
                NoticeReceived?.Invoke(version, notice);
        }
    }
}
