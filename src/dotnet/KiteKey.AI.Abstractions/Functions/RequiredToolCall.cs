namespace KiteKey.AI.Abstractions.Functions;

/// <summary>A tool call requested by a model, independent of its SDK.</summary>
public record RequiredToolCall(string FunctionName, string ToolCallId, string FunctionArguments);
