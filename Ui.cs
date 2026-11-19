namespace Connectly;

internal static class Ui
{
    public static readonly Color Background = Color.FromArgb(14, 22, 39);
    public static readonly Color Surface = Color.FromArgb(25, 36, 56);
    public static readonly Color Input = Color.FromArgb(34, 48, 72);
    public static readonly Color Text = Color.FromArgb(240, 245, 255);
    public static readonly Color Muted = Color.FromArgb(169, 185, 210);
    public static readonly Color Blue = Color.FromArgb(40, 91, 210);
    public static readonly Color LightBlue = Color.FromArgb(151, 187, 255);
    public static readonly Color Green = Color.FromArgb(112, 221, 169);
    public static readonly Color Red = Color.FromArgb(157, 52, 76);
    public static readonly Color LightRed = Color.FromArgb(255, 166, 179);

    public static void SetupForm(Form form, string title, Size clientSize, Size minimumSize)
    {
        form.Text = title;
        form.ClientSize = clientSize;
        form.MinimumSize = minimumSize;
        form.StartPosition = FormStartPosition.CenterScreen;
        form.AutoScaleMode = AutoScaleMode.Dpi;
        form.BackColor = Background;
        form.ForeColor = Text;
        form.Font = new Font("Segoe UI", 10);
    }

    public static TableLayoutPanel Grid(int columns, int rows) => new()
    {
        ColumnCount = columns,
        RowCount = rows,
        Dock = DockStyle.Fill,
        Margin = Padding.Empty
    };

    public static Label Label(string text, Color? color = null) => new()
    {
        Text = text,
        AutoSize = true,
        ForeColor = color ?? Text,
        Margin = Padding.Empty,
        UseMnemonic = false
    };

    public static Label Heading(string text, float size)
    {
        Label label = Label(text);
        label.Font = new Font("Segoe UI", size, FontStyle.Bold);
        return label;
    }

    public static Button Button(string text, Color color)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            MinimumSize = new Size(0, 36),
            Padding = new Padding(10, 2, 10, 2),
            Margin = Padding.Empty,
            BackColor = color,
            ForeColor = Text,
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false
        };
        button.FlatAppearance.BorderSize = 0;
        return button;
    }

    public static TextBox TextBox(string name, string text, int maxLength) => new()
    {
        AccessibleName = name,
        Text = text,
        MaxLength = maxLength,
        Dock = DockStyle.Fill,
        Margin = Padding.Empty,
        BackColor = Input,
        ForeColor = Text,
        BorderStyle = BorderStyle.FixedSingle
    };

    public static Panel Card() => new()
    {
        Dock = DockStyle.Fill,
        Margin = Padding.Empty,
        Padding = new Padding(14),
        BackColor = Surface
    };

    public static bool TryReadPort(TextBox field, out int port) =>
        int.TryParse(field.Text.Trim(), out port) && ChatProtocol.IsValidPort(port);

    public static void Post(Form form, Action action)
    {
        if (!form.IsHandleCreated || form.IsDisposed || form.Disposing)
            return;

        try
        {
            // Ağ olayları arka planda çalışır. Kontrolleri formun iş parçacığı günceller.
            form.BeginInvoke(() =>
            {
                if (!form.IsDisposed && !form.Disposing)
                    action();
            });
        }
        catch (InvalidOperationException)
        {
            // Olay gönderilirken pencere kapanmış olabilir.
        }
    }
}
