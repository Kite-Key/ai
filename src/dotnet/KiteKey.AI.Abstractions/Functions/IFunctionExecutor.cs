namespace KiteKey.AI.Abstractions.Functions;

/// <summary>Dispatches provider-agnostic function calls.</summary>
public interface IFunctionExecutor
{
    Task<string?> TryProcessFunctionCallAsync(string functionName, string arguments, CancellationToken cancellation);

    Task<string?> TryProcessFunctionCallAsync(string functionName, string arguments, IReadOnlyDictionary<string, string?> context, CancellationToken cancellation);
}
