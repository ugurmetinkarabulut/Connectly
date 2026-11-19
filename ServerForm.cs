using System.Net.Sockets;

namespace Connectly;

internal sealed class ServerForm : Form
{
    private readonly ChatServer _server = new();
    private readonly TextBox _port = Ui.TextBox("Port", ChatProtocol.DefaultPort.ToString(), 5);
    private readonly Button _start = Ui.Button("Sunucuyu Başlat", Ui.Blue);
    private readonly Button _stop = Ui.Button("Sunucuyu Durdur", Ui.Red);
    private readonly Label _status = Ui.Label("●  Sunucu kapalı", Ui.Muted);
    private readonly Label _clientCount = Ui.Label("Bağlı istemciler (0)");
    private readonly ListBox _users = CreateList("Bağlı istemciler");
    private readonly ListBox _activity = CreateList("Bağlantı ve mesaj olayları");
    private bool _closing;
    private bool _cleanupComplete;

    public ServerForm()
    {
        Ui.SetupForm(this, "Connectly · Sunucu", new Size(920, 600), new Size(690, 465));
        Controls.Add(BuildLayout());
        SetRunning(false);

        _start.Click += (_, _) => StartServer();
        _stop.Click += async (_, _) => await StopServerAsync();
        _server.Activity += entry =>
        {
            string time = DateTime.Now.ToString("HH:mm:ss");
            Ui.Post(this, () => AddActivity(time, entry));
        };
        _server.UsersChanged += names => Ui.Post(this, () => ShowUsers(names));
        _server.Stopped += () => Ui.Post(this, () =>
        {
            if (!_closing)
                SetRunning(false);
        });
    }

    private TableLayoutPanel BuildLayout()
    {
        TableLayoutPanel layout = Ui.Grid(1, 4);
        layout.Padding = new Padding(24);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        Label title = Ui.Heading("Sunucu", 26);
        title.Margin = new Padding(0, 0, 0, 14);
        layout.Controls.Add(title, 0, 0);

        Panel settings = Ui.Card();
        var controls = Ui.Grid(4, 2);
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82));
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        controls.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        controls.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        controls.Controls.Add(Ui.Label("Port", Ui.Muted), 0, 0);
        _port.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _port.Margin = new Padding(0, 0, 10, 0);
        controls.Controls.Add(_port, 0, 1);
        _start.Margin = new Padding(0, 0, 8, 0);
        controls.Controls.Add(_start, 1, 1);
        controls.Controls.Add(_stop, 2, 1);
        Label address = Ui.Label("127.0.0.1", Ui.Muted);
        address.Anchor = AnchorStyles.Right;
        controls.Controls.Add(address, 3, 1);
        settings.Controls.Add(controls);
        layout.Controls.Add(settings, 0, 1);

        _status.Margin = new Padding(0, 12, 0, 13);
        layout.Controls.Add(_status, 0, 2);

        var dashboard = Ui.Grid(2, 1);
        dashboard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 72));
        dashboard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
        Panel eventsCard = ListCard(Ui.Label("Bağlantı ve mesaj olayları"), _activity);
        eventsCard.Margin = new Padding(0, 0, 10, 0);
        dashboard.Controls.Add(eventsCard, 0, 0);
        dashboard.Controls.Add(ListCard(_clientCount, _users), 1, 0);
        layout.Controls.Add(dashboard, 0, 3);
        return layout;
    }

    private static Panel ListCard(Label heading, ListBox list)
    {
        Panel card = Ui.Card();
        var layout = Ui.Grid(1, 2);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        heading.Margin = new Padding(0, 0, 0, 10);
        layout.Controls.Add(heading, 0, 0);
        layout.Controls.Add(list, 0, 1);
        card.Controls.Add(layout);
        return card;
    }

    private static ListBox CreateList(string name) => new()
    {
        AccessibleName = name,
        Dock = DockStyle.Fill,
        Margin = Padding.Empty,
        IntegralHeight = false,
        HorizontalScrollbar = true,
        BackColor = Ui.Input,
        ForeColor = Ui.Text,
        BorderStyle = BorderStyle.None
    };

    private void StartServer()
    {
        if (!Ui.TryReadPort(_port, out int port))
        {
            SetStatus("Port 1 ile 65535 arasında olmalıdır.", Ui.LightRed);
            _port.Focus();
            return;
        }

        try
        {
            _server.Start(port);
            SetRunning(true);
        }
        catch (SocketException exception) when (exception.SocketErrorCode == SocketError.AddressAlreadyInUse)
        {
            SetStatus($"{port} numaralı port kullanımda. Başka bir port seçin.", Ui.LightRed);
        }
        catch (SocketException exception)
        {
            SetStatus($"Sunucu bu portu açamadı ({exception.SocketErrorCode}).", Ui.LightRed);
        }
        catch (InvalidOperationException exception)
        {
            SetStatus(exception.Message, Ui.LightRed);
        }
    }

    private async Task StopServerAsync()
    {
        _stop.Enabled = false;
        SetStatus("●  Bağlantılar kapatılıyor…", Ui.Muted);
        await _server.StopAsync();
        if (!_closing)
            SetRunning(false);
    }

    private void SetRunning(bool running)
    {
        _port.Enabled = !running;
        _start.Enabled = !running;
        _stop.Enabled = running;
        SetStatus(running ? "●  Sunucu çalışıyor" : "●  Sunucu kapalı", running ? Ui.Green : Ui.Muted);
    }

    private void SetStatus(string text, Color color)
    {
        _status.Text = text;
        _status.ForeColor = color;
    }

    private void ShowUsers(string[] names)
    {
        _clientCount.Text = $"Bağlı istemciler ({names.Length})";
        _users.BeginUpdate();
        _users.Items.Clear();
        _users.Items.AddRange(names);
        _users.EndUpdate();
    }

    private void AddActivity(string time, string entry)
    {
        if (_activity.Items.Count == 500)
            _activity.Items.RemoveAt(0);

        _activity.Items.Add($"[{time}]  {entry}");
        _activity.TopIndex = _activity.Items.Count - 1;
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
        await _server.StopAsync();
        _cleanupComplete = true;
        BeginInvoke((Action)Close);
    }
}
