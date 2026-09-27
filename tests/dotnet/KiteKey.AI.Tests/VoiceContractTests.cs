using System.Text.Json;
using KiteKey.AI.Abstractions.Voice;

namespace KiteKey.AI.Tests;

public class VoiceContractTests
{
    [Fact]
    public async Task TranscriptCanBeMappedIntoApplicationModelWithoutPackageDependency()
    {
        var transport = new AppAudioTransport();
        var neutral = new VoiceTranscript(
            "turn-1", "tool", "weather", true,
            new DateTimeOffset(2026, 9, 27, 14, 0, 0, TimeSpan.Zero),
            new VoiceToolCall("weather", """{"city":"Boston"}""", """{"forecast":"sunny"}"""));

        await transport.SendTranscriptAsync(neutral, CancellationToken.None);

        Assert.Equal(neutral.MessageId, transport.Transcript!.MessageId);
        Assert.Equal(neutral.Speaker, transport.Transcript.Speaker);
        Assert.Equal(neutral.Text, transport.Transcript.Text);
        Assert.Equal(neutral.IsFinal, transport.Transcript.IsFinal);
        Assert.Equal(neutral.Timestamp, transport.Transcript.Timestamp);
        Assert.Equal(neutral.ToolCall!.Name, transport.Transcript.ToolName);
        Assert.Equal(neutral.ToolCall.Arguments, transport.Transcript.ToolArguments);
        Assert.Equal(neutral.ToolCall.Output, transport.Transcript.ToolOutput);
        Assert.Equal("tool", JsonSerializer.Deserialize<VoiceTranscript>(
            JsonSerializer.Serialize(neutral))!.Speaker);
    }

    private sealed record AppTranscript(
        string MessageId, string Speaker, string Text, bool IsFinal, DateTimeOffset Timestamp,
        string? ToolName, string? ToolArguments, string? ToolOutput);

    private sealed class AppAudioTransport : IVoiceAudioClient
    {
        public AppTranscript? Transcript { get; private set; }
        public string? ConversationId => "test-conversation";
        public bool IsHumanSpeaking { get; set; }
        public bool IsModelSpeaking { get; set; }

        public Task SendAudioAsync(byte[] audio, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task ClearPlaybackAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Stream ReceiveAudioStream() => Stream.Null;
        public Task SendStatusAsync(string status, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SendTranscriptAsync(VoiceTranscript entry, CancellationToken cancellationToken)
        {
            Transcript = new AppTranscript(
                entry.MessageId, entry.Speaker, entry.Text, entry.IsFinal, entry.Timestamp,
                entry.ToolCall?.Name, entry.ToolCall?.Arguments, entry.ToolCall?.Output);
            return Task.CompletedTask;
        }
    }
}
