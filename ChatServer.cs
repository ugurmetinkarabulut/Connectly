using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace Connectly;

internal sealed class ChatServer
{
    private readonly object _lifecycleLock = new();
    private readonly SemaphoreSlim _broadcastLock = new(1);
    private readonly Dictionary<string, ChatConnection> _users = new(StringComparer.OrdinalIgnoreCase);
    private CancellationTokenSource? _stopSignal;
    private Task? _serverTask;

    public event Action<string>? Activity;
    public event Action<string[]>? UsersChanged;
    public event Action? Stopped;

    public void Start(int port)
    {
        ChatProtocol.ValidatePort(port);

        lock (_lifecycleLock)
        {
            if (_serverTask is { IsCompleted: false })
                throw new InvalidOperationException("Sunucu zaten çalışıyor veya durduruluyor.");

            // Loopback adresi yalnızca aynı bilgisayardan bağlantı kabul eder.
            var listener = new TcpListener(IPAddress.Loopback, port);
            try
            {
                listener.Start();
            }
            catch (SocketException)
            {
                listener.Stop();
                throw;
            }

            var stopSignal = new CancellationTokenSource();
            _stopSignal = stopSignal;
            _serverTask = Task.Run(() => ListenAsync(listener, port, stopSignal));
        }
    }

    public Task StopAsync()
    {
        lock (_lifecycleLock)
        {
            _stopSignal?.Cancel();
            return _serverTask ?? Task.CompletedTask;
        }
    }

    private async Task ListenAsync(TcpListener listener, int port, CancellationTokenSource stopSignal)
    {
        CancellationToken cancellationToken = stopSignal.Token;
        var clientTasks = new List<Task>();
        Activity?.Invoke($"Sunucu 127.0.0.1:{port} adresinde dinliyor.");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                TcpClient client = await listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
                clientTasks.RemoveAll(task => task.IsCompletedSuccessfully);
                clientTasks.Add(HandleClientAsync(client, cancellationToken));
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Durdur butonu yeni bağlantı bekleyen işlemi iptal eder.
        }
        catch (SocketException exception)
        {
            Activity?.Invoke($"Sunucu bağlantı kabul edemedi ({exception.SocketErrorCode}).");
        }
        finally
        {
            stopSignal.Cancel();
            listener.Stop();
            await Task.WhenAll(clientTasks).ConfigureAwait(false);

            lock (_lifecycleLock)
            {
                _stopSignal = null;
                stopSignal.Dispose();
            }

            Activity?.Invoke("Sunucu durdu.");
            Stopped?.Invoke();
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using (client)
        {
            string? joinedName = null;

            try
            {
                using var connection = new ChatConnection(client);
                try
                {
                    string? hello;
                    using (var loginTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                    {
                        loginTimeout.CancelAfter(TimeSpan.FromSeconds(5));
                        hello = await connection.ReadLineAsync(loginTimeout.Token).ConfigureAwait(false);
                    }

                    if (hello is null)
                        return;

                    string prefix = ChatProtocol.Hello + "\t";
                    string userName = hello.StartsWith(prefix, StringComparison.Ordinal)
                        ? hello[prefix.Length..].Trim()
                        : "";
                    if (!ChatProtocol.IsValidText(userName, ChatProtocol.MaxUserNameLength))
                    {
                        await connection.WriteLineAsync(
                            $"{ChatProtocol.Error}\tGeçerli bir kullanıcı adı gerekli.", cancellationToken)
                            .ConfigureAwait(false);
                        return;
                    }

                    await _broadcastLock.WaitAsync(cancellationToken).ConfigureAwait(false);
                    try
                    {
                        if (_users.ContainsKey(userName))
                        {
                            await connection.WriteLineAsync(
                                $"{ChatProtocol.Error}\tBu kullanıcı adı zaten kullanımda.", cancellationToken)
                                .ConfigureAwait(false);
                            return;
                        }

                        await connection.WriteLineAsync(ChatProtocol.Accepted, cancellationToken).ConfigureAwait(false);
                        await BroadcastAsync($"{ChatProtocol.Joined}\t{userName}", cancellationToken).ConfigureAwait(false);
                        _users.Add(userName, connection);
                        joinedName = userName;
                        ReportUsers();
                        Activity?.Invoke($"{userName} bağlandı.");
                    }
                    finally
                    {
                        _broadcastLock.Release();
                    }

                    while (!connection.IsClosed)
                    {
                        string? line = await connection.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                        if (line is null)
                            break;

                        await HandleMessageAsync(userName, connection, line, cancellationToken).ConfigureAwait(false);
                    }
                }
                finally
                {
                    // Sözlükten çıkarmadan akışları kapatırsak başka bir yayında kullanılabilirler.
                    if (joinedName is not null)
                        await RemoveUserAsync(joinedName, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // Sunucu durmuş veya istemci kullanıcı adını zamanında göndermemiş olabilir.
            }
            catch (InvalidDataException exception)
            {
                Activity?.Invoke(exception.Message);
            }
            catch (Exception exception) when (exception is IOException or SocketException or ObjectDisposedException)
            {
                // Soket beklenmedik şekilde kapansa da dinleme devam eder.
            }
        }
    }

    private async Task HandleMessageAsync(
        string userName, ChatConnection connection, string line, CancellationToken cancellationToken)
    {
        string prefix = ChatProtocol.Send + "\t";
        string message = line.StartsWith(prefix, StringComparison.Ordinal) ? line[prefix.Length..].Trim() : "";

        await _broadcastLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!ChatProtocol.IsValidText(message, ChatProtocol.MaxMessageLength))
            {
                await WriteToUserAsync(
                    connection, $"{ChatProtocol.Error}\tMesaj boş, çok uzun veya geçersiz.", cancellationToken)
                    .ConfigureAwait(false);
                return;
            }

            string time = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            // Gönderen de yayını alır. İstemci, yazdığı mesajı yalnızca bu cevapla ekrana koyar.
            await BroadcastAsync($"{ChatProtocol.Message}\t{time}\t{userName}\t{message}", cancellationToken)
                .ConfigureAwait(false);
            Activity?.Invoke($"{userName}: {message}");
        }
        finally
        {
            _broadcastLock.Release();
        }
    }

    private async Task RemoveUserAsync(string userName, CancellationToken cancellationToken)
    {
        // Sunucu dururken de kullanıcı listesi ve bağlantı kaydı mutlaka temizlenir.
        await _broadcastLock.WaitAsync().ConfigureAwait(false);
        try
        {
            _users.Remove(userName);
            ReportUsers();
            Activity?.Invoke($"{userName} ayrıldı.");

            if (!cancellationToken.IsCancellationRequested)
                await BroadcastAsync($"{ChatProtocol.Left}\t{userName}", cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _broadcastLock.Release();
        }
    }

    // Kayıt, yayın ve ayrılma aynı kilitten geçer. Bir akışa iki yayın aynı anda yazamaz.
    private Task BroadcastAsync(string line, CancellationToken cancellationToken) =>
        Task.WhenAll(_users.Values.Select(user => WriteToUserAsync(user, line, cancellationToken)));

    private static async Task WriteToUserAsync(
        ChatConnection user, string line, CancellationToken cancellationToken)
    {
        if (user.IsClosed)
            return;

        try
        {
            await user.WriteLineAsync(line, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is IOException or SocketException or ObjectDisposedException)
        {
            user.Close();
        }
    }

    private void ReportUsers() =>
        UsersChanged?.Invoke(_users.Keys.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray());
}
