using System.Net.Sockets;
using System.Text;

namespace Connectly;

internal sealed class ChatConnection : IDisposable
{
    private readonly TcpClient _client;
    private readonly StreamReader _reader;
    private readonly StreamWriter _writer;
    private readonly char[] _readBuffer = new char[256];
    private int _readPosition;
    private int _readCount;
    private int _closed;

    public ChatConnection(TcpClient client)
    {
        _client = client;
        NetworkStream stream = client.GetStream();
        var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        _reader = new StreamReader(stream, utf8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        _writer = new StreamWriter(stream, utf8, leaveOpen: true) { NewLine = "\n" };
    }

    public bool IsClosed => Volatile.Read(ref _closed) != 0;

    public async Task<string?> ReadLineAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var line = new StringBuilder();

        while (true)
        {
            // Bir okumada birden fazla satır gelebilir. Sonraki satırın başı tamponda kalır.
            if (_readPosition == _readCount)
            {
                _readCount = await _reader.ReadAsync(_readBuffer.AsMemory(), cancellationToken).ConfigureAwait(false);
                _readPosition = 0;
                if (_readCount == 0)
                    return null;
            }

            char character = _readBuffer[_readPosition++];
            if (character == '\n')
            {
                if (line.Length > 0 && line[line.Length - 1] == '\r')
                    line.Length--;

                return line.ToString();
            }

            if (line.Length == ChatProtocol.MaxLineLength)
                throw new InvalidDataException("Ağdan gelen satır izin verilen uzunluğu aştı.");

            line.Append(character);
        }
    }

    public async Task WriteLineAsync(
        string line, CancellationToken cancellationToken = default, int timeoutSeconds = 3)
    {
        // Yavaş bir istemci, sunucunun diğer bağlantıları süresiz bekletmemeli.
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            await _writer.WriteLineAsync(line.AsMemory(), timeout.Token).ConfigureAwait(false);
            await _writer.FlushAsync(timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new IOException("Ağ yazma işlemi zaman aşımına uğradı.", exception);
        }
    }

    // Önce soketi kapatmak, bekleyen okuma ve yazma işlemlerini uyandırır.
    public void Close()
    {
        if (Interlocked.Exchange(ref _closed, 1) == 0)
            _client.Dispose();
    }

    public void Dispose()
    {
        Close();
        _reader.Dispose();

        try
        {
            _writer.Dispose();
        }
        catch (IOException)
        {
            // Bağlantı kesildiyse tampondaki veri artık gönderilemez.
        }
        catch (ObjectDisposedException)
        {
            // Soket kapanırken yarım kalan yazma olabilir.
        }
    }
}
