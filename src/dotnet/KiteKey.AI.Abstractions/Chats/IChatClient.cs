namespace KiteKey.AI.Abstractions.Chats;

/// <summary>Communicates model output to the human recipient.</summary>
public interface IChatClient
{
    Task SendMessage(string message, CancellationToken cancellation);
}
