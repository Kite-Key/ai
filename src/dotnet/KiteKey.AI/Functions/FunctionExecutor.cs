using System.Text.Json;
using KiteKey.AI.Abstractions.Functions;

namespace KiteKey.AI.Functions;

/// <summary>Dispatches named tools without requiring any application-specific integration.</summary>
public sealed class FunctionExecutor(
    IEnumerable<IFunctionHandler> functions,
    Func<string, string, CancellationToken, Task<string?>>? fallback = null) : IFunctionExecutor
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IEnumerable<IFunctionHandler> _functions = functions ?? throw new ArgumentNullException(nameof(functions));

    public Task<string?> TryProcessFunctionCallAsync(string functionName, string arguments, CancellationToken cancellation)
        => ExecuteAsync(functionName, arguments, cancellation);

    public Task<string?> TryProcessFunctionCallAsync(
        string functionName, string arguments, IReadOnlyDictionary<string, string?> context, CancellationToken cancellation)
        => ExecuteAsync(functionName, MergeContext(arguments, context), cancellation);

    private async Task<string?> ExecuteAsync(string functionName, string arguments, CancellationToken cancellation)
    {
        IFunctionHandler? function = _functions.SingleOrDefault(f => f.Name == functionName);
        if (function is not null)
            return await function.Process(arguments, cancellation).ConfigureAwait(false);

        return fallback is null
            ? null
            : await fallback(functionName, arguments, cancellation).ConfigureAwait(false);
    }

    private static string MergeContext(string arguments, IReadOnlyDictionary<string, string?> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        Dictionary<string, string?> payload;
        try
        {
            payload = string.IsNullOrWhiteSpace(arguments)
                ? []
                : JsonSerializer.Deserialize<Dictionary<string, string?>>(arguments, SerializerOptions) ?? [];
        }
        catch (JsonException)
        {
            return arguments;
        }

        foreach ((string key, string? value) in context)
        {
            if (string.IsNullOrWhiteSpace(value))
                continue;

            if (!payload.TryGetValue(key, out string? existingValue)
                || string.IsNullOrWhiteSpace(existingValue)
                || existingValue is "unknown" or "n/a" or "na")
                payload[key] = value;
        }

        return JsonSerializer.Serialize(payload, SerializerOptions);
    }
}
