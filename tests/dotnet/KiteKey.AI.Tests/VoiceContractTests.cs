using System.Text.Json;
using KiteKey.AI.Abstractions.Voice;

namespace KiteKey.AI.Tests;

public class VoiceContractTests
{
    [Fact]
    public async Task TranscriptCanBeMappedIntoApplicationModelWithoutPackageDependency()
    {
        var transport = new AppAudioTransport();
        var neutral = new ConversationTranscriptMessage
        {
            MessageId = "turn-1",
            Speaker = "tool",
            Text = "weather",
            IsFinal = true,
            Timestamp = new DateTimeOffset(2026, 9, 27, 14, 0, 0, TimeSpan.Zero),
            ToolName = "weather",
            ToolArguments = """{"city":"Boston"}""",
            ToolOutput = """{"forecast":"sunny"}"""
        };

        await transport.SendTranscriptAsync(neutral);

        Assert.Equal(neutral.MessageId, transport.Transcript!.MessageId);
        Assert.Equal(neutral.Speaker, transport.Transcript.Speaker);
        Assert.Equal(neutral.Text, transport.Transcript.Text);
        Assert.Equal(neutral.IsFinal, transport.Transcript.IsFinal);
        Assert.Equal(neutral.Timestamp, transport.Transcript.Timestamp);
        Assert.Equal(neutral.ToolName, transport.Transcript.ToolName);
        Assert.Equal(neutral.ToolArguments, transport.Transcript.ToolArguments);
        Assert.Equal(neutral.ToolOutput, transport.Transcript.ToolOutput);
        Assert.Equal("tool", JsonSerializer.Deserialize<ConversationTranscriptMessage>(
            JsonSerializer.Serialize(neutral))!.Speaker);
    }

    private sealed record AppTranscript(
        string MessageId, string Speaker, string Text, bool IsFinal, DateTimeOffset Timestamp,
        string? ToolName, string? ToolArguments, string? ToolOutput);

    private sealed class AppAudioTransport : IHumanAudioClient
    {
        public AppTranscript? Transcript { get; private set; }
        public string? ConversationId { get; set; }
        public bool IsHumanSpeaking { get; set; }
        public bool IsModelSpeaking { get; set; }
        public event Func<Task>? StreamClosed;

        public Task SendAudio(byte[] audio) => Task.CompletedTask;
        public Task ClearPlayback() => Task.CompletedTask;
        public Task EndPlayback() => StreamClosed?.Invoke() ?? Task.CompletedTask;
        public Stream ReceiveAudioStream() => Stream.Null;
        public Task StartCapture() => Task.CompletedTask;
        public Task StartPlayback() => Task.CompletedTask;
        public Task SendStatusAsync(string status) => Task.CompletedTask;
        public Task SendTranscriptAsync(ConversationTranscriptMessage entry)
        {
            Transcript = new AppTranscript(
                entry.MessageId, entry.Speaker, entry.Text, entry.IsFinal, entry.Timestamp,
                entry.ToolName, entry.ToolArguments, entry.ToolOutput);
            return Task.CompletedTask;
        }
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
