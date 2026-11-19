namespace Connectly;

internal sealed class ChatMessage
{
    public ChatMessage(string sender, string text, DateTimeOffset sentAt)
    {
        Sender = sender;
        Text = text;
        SentAt = sentAt;
    }

    public string Sender { get; }
    public string Text { get; }
    public DateTimeOffset SentAt { get; }
}
