using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Connectly;

internal sealed class ClientForm : Form
{
    private readonly ChatClient _client = new();
    private readonly TextBox _userName = Ui.TextBox("Kullanıcı adı", "", ChatProtocol.MaxUserNameLength);
    private readonly TextBox _address = Ui.TextBox("Sunucu IP adresi", "127.0.0.1", 45);
    private readonly TextBox _port = Ui.TextBox("Port", ChatProtocol.DefaultPort.ToString(), 5);
    private readonly Button _connect = Ui.Button("Bağlan", Ui.Blue);
    private readonly Button _disconnect = Ui.Button("Bağlantıyı Kes", Ui.Red);
    private readonly Button _send = Ui.Button("Gönder", Ui.Blue);
    private readonly Label _status = Ui.Label("●  Bağlı değil", Ui.Muted);
    private readonly TextBox _message = Ui.TextBox("Mesaj", "", ChatProtocol.MaxMessageLength);
    private readonly RichTextBox _conversation = new()
    {
        AccessibleName = "Sohbet mesajları",
        ReadOnly = true,
        DetectUrls = false,
        Dock = DockStyle.Fill,
        Margin = Padding.Empty,
        BackColor = Ui.Input,
        ForeColor = Ui.Text,
        BorderStyle = BorderStyle.None
    };

    private CancellationTokenSource? _connectCancellation;
    private bool _changingConnection;
    private bool _sending;
    private bool _closing;
    private bool _cleanupComplete;

    public ClientForm()
    {
        Ui.SetupForm(this, "Connectly · İstemci", new Size(920, 650), new Size(740, 475));
        _message.PlaceholderText = "Mesaj yaz…";
        Controls.Add(BuildLayout());

        _connect.Click += async (_, _) => await ConnectAsync();
        _disconnect.Click += async (_, _) => await DisconnectAsync();
        _send.Click += async (_, _) => await SendAsync();
        _message.TextChanged += (_, _) => UpdateControls();
        _message.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                await SendAsync();
            }
        };

        _client.MessageReceived += (version, message, isOwn) =>
            PostConnectionEvent(version, () => ShowMessage(message, isOwn));
        _client.NoticeReceived += (version, notice) =>
            PostConnectionEvent(version, () => AddNotice(notice));
        _client.ConnectionClosed += (version, reason) => PostConnectionEvent(version, () =>
        {
            if (reason is not null)
            {
                AddNotice(reason);
                SetStatus(reason, Ui.LightRed);
            }

            UpdateControls();
        });

        ActiveControl = _userName;
        UpdateControls();
    }

    private void PostConnectionEvent(int version, Action action)
    {
        Ui.Post(this, () =>
        {
            if (!_closing && version == _client.ConnectionVersion)
                action();
        });
    }

    private TableLayoutPanel BuildLayout()
    {
        var layout = Ui.Grid(1, 5);
        layout.Padding = new Padding(24);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

        Label title = Ui.Heading("İstemci", 26);
        title.Margin = new Padding(0, 0, 0, 14);
        layout.Controls.Add(title, 0, 0);
        layout.Controls.Add(ConnectionCard(), 0, 1);

        _status.Margin = new Padding(0, 12, 0, 13);
        layout.Controls.Add(_status, 0, 2);

        Panel chat = Ui.Card();
        var chatLayout = Ui.Grid(1, 2);
        chatLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        chatLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Label chatTitle = Ui.Label("Sohbet");
        chatTitle.Margin = new Padding(0, 0, 0, 10);
        chatLayout.Controls.Add(chatTitle, 0, 0);
        chatLayout.Controls.Add(_conversation, 0, 1);
        chat.Controls.Add(chatLayout);
        layout.Controls.Add(chat, 0, 3);

        var composer = Ui.Grid(2, 1);
        composer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        composer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        composer.Margin = new Padding(0, 10, 0, 0);
        _message.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _message.Margin = new Padding(0, 0, 10, 0);
        composer.Controls.Add(_message, 0, 0);
        _send.Anchor = AnchorStyles.Right;
        composer.Controls.Add(_send, 1, 0);
        layout.Controls.Add(composer, 0, 4);
        return layout;
    }

    private Panel ConnectionCard()
    {
        Panel card = Ui.Card();
        var layout = Ui.Grid(5, 2);
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 75));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        AddField("Kullanıcı adı", _userName, 0);
        AddField("Sunucu IP adresi", _address, 1);
        AddField("Port", _port, 2);
        _connect.Margin = new Padding(0, 0, 8, 0);
        layout.Controls.Add(_connect, 3, 1);
        layout.Controls.Add(_disconnect, 4, 1);
        card.Controls.Add(layout);
        return card;

        void AddField(string name, TextBox field, int column)
        {
            layout.Controls.Add(Ui.Label(name, Ui.Muted), column, 0);
            field.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            field.Margin = new Padding(0, 0, 10, 0);
            layout.Controls.Add(field, column, 1);
        }
    }

    private async Task ConnectAsync()
    {
        string userName = _userName.Text.Trim(' ');
        if (!ChatProtocol.IsValidText(userName, ChatProtocol.MaxUserNameLength))
        {
            SetStatus("Geçerli bir kullanıcı adı girin (1–24 karakter).", Ui.LightRed);
            _userName.Focus();
            return;
        }

        if (!IPAddress.TryParse(_address.Text.Trim(), out IPAddress? address))
        {
            SetStatus("Geçerli bir IP adresi girin. Yerel adres 127.0.0.1.", Ui.LightRed);
            _address.Focus();
            return;
        }

        if (!Ui.TryReadPort(_port, out int port))
        {
            SetStatus("Port 1 ile 65535 arasında olmalıdır.", Ui.LightRed);
            _port.Focus();
            return;
        }

        using var cancellation = new CancellationTokenSource();
        _connectCancellation = cancellation;
        _changingConnection = true;
        SetStatus("●  Bağlanılıyor…", Ui.Muted);
        UpdateControls();

        try
        {
            await _client.ConnectAsync(userName, address, port, cancellation.Token);
            if (_client.IsConnected && !_closing)
            {
                SetStatus($"●  Bağlı  ·  {userName}  ·  {address}:{port}", Ui.Green);
                AddNotice($"{userName} olarak bağlandınız.");
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            // Form kapanırken bekleyen bağlantı iptal edilir.
        }
        catch (TimeoutException exception)
        {
            SetStatus(exception.Message, Ui.LightRed);
        }
        catch (SocketException)
        {
            SetStatus($"Sunucuya erişilemedi ({address}:{port}). IP adresini ve portu kontrol edin.", Ui.LightRed);
        }
        catch (IOException exception)
        {
            SetStatus(exception.Message, Ui.LightRed);
        }
        catch (InvalidDataException)
        {
            SetStatus("Sunucudan geçersiz yanıt alındı.", Ui.LightRed);
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
            SetStatus("Bağlantı kurulamadı. Tekrar deneyin.", Ui.LightRed);
        }
        finally
        {
            _connectCancellation = null;
            _changingConnection = false;
            UpdateControls();
            if (_client.IsConnected && !_closing)
                _message.Focus();
        }
    }

    private async Task DisconnectAsync()
    {
        _changingConnection = true;
        UpdateControls();
        try
        {
            await _client.DisconnectAsync();
            SetStatus("●  Bağlı değil", Ui.Muted);
            if (!_closing)
                AddNotice("Bağlantıyı kestiniz.");
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
            SetStatus("Bağlantı kapatılırken bir hata oluştu.", Ui.LightRed);
        }
        finally
        {
            _changingConnection = false;
            UpdateControls();
        }
    }

    private async Task SendAsync()
    {
        if (!_send.Enabled)
            return;

        _sending = true;
        string originalText = _message.Text;
        UpdateControls();
        try
        {
            await _client.SendAsync(originalText);
            if (!_closing && _message.Text == originalText)
                _message.Clear();
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or IOException)
        {
            if (!_closing)
            {
                AddNotice(exception.Message);
                if (!_client.IsConnected && !_changingConnection)
                    SetStatus("●  Bağlı değil", Ui.LightRed);
            }
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
            SetStatus("Mesaj gönderilemedi.", Ui.LightRed);
        }
        finally
        {
            _sending = false;
            UpdateControls();
        }
    }

    private void UpdateControls()
    {
        if (_closing)
            return;

        bool connected = _client.IsConnected;
        bool canConnect = !_changingConnection && !_sending && !connected;
        _userName.Enabled = _address.Enabled = _port.Enabled = _connect.Enabled = canConnect;
        _disconnect.Enabled = _message.Enabled = !_changingConnection && connected;
        _send.Enabled = _message.Enabled && !_sending &&
            ChatProtocol.IsValidText(_message.Text.Trim(' '), ChatProtocol.MaxMessageLength);
    }

    private void SetStatus(string text, Color color)
    {
        if (_closing)
            return;

        _status.Text = text;
        _status.ForeColor = color;
    }

    private void ShowMessage(ChatMessage message, bool isOwn)
    {
        string sender = isOwn ? $"{message.Sender} (Siz)" : message.Sender;
        AppendLine($"{message.SentAt.ToLocalTime():HH:mm:ss}  ·  {sender}", isOwn ? Ui.LightBlue : Ui.Muted);
        AppendLine($"{message.Text}\n", isOwn ? Ui.LightBlue : Ui.Text);
    }

    private void AddNotice(string notice) => AppendLine($"{DateTime.Now:HH:mm:ss}  ·  {notice}\n", Ui.Muted);

    private void AppendLine(string text, Color color)
    {
        _conversation.SelectionStart = _conversation.TextLength;
        _conversation.SelectionLength = 0;
        _conversation.SelectionColor = color;
        _conversation.AppendText(text + "\n");
        _conversation.ScrollToCaret();
    }

    protected override async void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        if (e.Cancel || _cleanupComplete)
            return;

        e.Cancel = true;
        if (_closing)
            return;

        _closing = true;
        Enabled = false;
        try
        {
            try
            {
                _connectCancellation?.Cancel();
            }
            finally
            {
                await _client.DisconnectAsync();
            }
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
        }
        finally
        {
            _cleanupComplete = true;
            Ui.Post(this, Close);
        }
    }
}
