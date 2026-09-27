namespace KiteKey.AI.Abstractions.Voice;

/// <summary>Host-provided bidirectional audio and transcript transport for a voice conversation.</summary>
public interface IVoiceAudioClient
{
    string? ConversationId { get; }
    bool IsHumanSpeaking { get; set; }
    bool IsModelSpeaking { get; set; }

    Stream ReceiveAudioStream();
    Task SendAudioAsync(byte[] audio, CancellationToken cancellationToken);
    Task ClearPlaybackAsync(CancellationToken cancellationToken);
    Task SendTranscriptAsync(VoiceTranscript transcript, CancellationToken cancellationToken);
    Task SendStatusAsync(string status, CancellationToken cancellationToken);
}

/// <summary>Transcript event independent of provider SDKs and host persistence entities.</summary>
public sealed record VoiceTranscript(
    string MessageId,
    string Speaker,
    string Text,
    bool IsFinal,
    DateTimeOffset Timestamp,
    VoiceToolCall? ToolCall = null);

/// <summary>Optional tool-call details surfaced alongside a transcript event.</summary>
public sealed record VoiceToolCall(string Name, string Arguments, string? Output);
