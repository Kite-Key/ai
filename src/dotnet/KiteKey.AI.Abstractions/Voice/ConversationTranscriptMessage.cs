namespace KiteKey.AI.Abstractions.Voice;

/// <summary>A speaker's transcript entry, including optional tool-call details.</summary>
public class ConversationTranscriptMessage
{
    public required string MessageId { get; set; }
    public required string Speaker { get; set; }
    public required string Text { get; set; }
    public bool IsFinal { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string? ToolName { get; set; }
    public string? ToolArguments { get; set; }
    public string? ToolOutput { get; set; }
}
