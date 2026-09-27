namespace KiteKey.AI.Abstractions.Voice;

/// <summary>Bidirectional human audio transport, independent of voice provider and client platform.</summary>
public interface IHumanAudioClient : IAsyncDisposable, IDisposable
{
    string? ConversationId { get; set; }
    bool IsHumanSpeaking { get; set; }
    bool IsModelSpeaking { get; set; }

    event Func<Task>? StreamClosed;

    Task SendAudio(byte[] audio);
    Task ClearPlayback();
    Task EndPlayback();
    Stream ReceiveAudioStream();
    Task StartCapture();
    Task StartPlayback();
    Task SendTranscriptAsync(ConversationTranscriptMessage message);
    Task SendStatusAsync(string status);
}
