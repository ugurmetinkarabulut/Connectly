namespace Connectly;

internal sealed class MainForm : Form
{
    public MainForm()
    {
        Ui.SetupForm(this, "Connectly", new Size(680, 395), new Size(590, 393));

        var layout = Ui.Grid(2, 3);
        layout.Padding = new Padding(28);
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var header = Ui.Grid(1, 2);
        header.AutoSize = true;
        header.Margin = new Padding(0, 0, 0, 24);
        header.Controls.Add(Ui.Heading("Connectly", 29), 0, 0);
        header.Controls.Add(Ui.Label("Yerel TCP sohbeti", Ui.Muted), 0, 1);
        layout.Controls.Add(header, 0, 0);
        layout.SetColumnSpan(header, 2);

        Panel server = RoleCard("Sunucu", "Sohbeti başlat, bağlı kullanıcıları ve ağ olaylarını izle.",
            "Sunucu ekranını aç", () => new ServerForm());
        server.Margin = new Padding(0, 0, 6, 0);
        layout.Controls.Add(server, 0, 1);

        Panel client = RoleCard("İstemci", "Bir kullanıcı adıyla bağlan ve sohbete katıl.",
            "İstemci ekranını aç", () => new ClientForm());
        client.Margin = new Padding(6, 0, 0, 0);
        layout.Controls.Add(client, 1, 1);

        Label course = Ui.Label("Fırat Üniversitesi · YMH219 Nesne Tabanlı Programlama", Ui.Muted);
        course.Margin = new Padding(0, 18, 0, 0);
        layout.Controls.Add(course, 0, 2);
        layout.SetColumnSpan(course, 2);
        Controls.Add(layout);
    }

    private Panel RoleCard(string title, string description, string buttonText, Func<Form> createForm)
    {
        Panel card = Ui.Card();
        var layout = Ui.Grid(1, 3);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(Ui.Heading(title, 17), 0, 0);
        Label summary = Ui.Label(description, Ui.Muted);
        summary.Margin = new Padding(0, 7, 0, 0);
        summary.MaximumSize = new Size(218, 0);
        layout.Controls.Add(summary, 0, 1);

        Button open = Ui.Button(buttonText, Ui.Blue);
        open.Dock = DockStyle.Top;
        open.Click += (_, _) =>
        {
            using Form form = createForm();
            form.ShowDialog(this);
        };
        layout.Controls.Add(open, 0, 2);
        card.Controls.Add(layout);
        return card;
    }
}
