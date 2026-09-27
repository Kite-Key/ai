using System.Text.Json;
using KiteKey.AI.Abstractions.Functions;
using Microsoft.Extensions.Logging;

namespace KiteKey.AI.Functions;

/// <summary>Deserializes tool arguments and serializes tool output as web-style JSON.</summary>
public abstract class FunctionHandlerBase<TArgs, TOutput>(ILogger logger) : IFunctionHandler
    where TArgs : class
    where TOutput : class
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web);

    protected ILogger Logger { get; } = logger ?? throw new ArgumentNullException(nameof(logger));

    public abstract string Name { get; }

    public async Task<string> Process(string args, CancellationToken cancellation)
    {
        TArgs? input = string.IsNullOrEmpty(args)
            ? null
            : JsonSerializer.Deserialize<TArgs>(args, _jsonSerializerOptions)
                ?? throw new JsonException("Function arguments cannot be JSON null.");

        Logger.LogInformation("Processing function {FunctionName} with argument type {Type}", Name, typeof(TArgs));
        TOutput output = input is not null
            ? await ProcessCore(input, cancellation).ConfigureAwait(false)
            : await Process().ConfigureAwait(false);

        return JsonSerializer.Serialize(
            output ?? throw new InvalidOperationException($"Function {Name} returned a null result."),
            _jsonSerializerOptions);
    }

    public async Task<TOutput> Process(TArgs args, CancellationToken cancellation)
    {
        ArgumentNullException.ThrowIfNull(args);
        Logger.LogInformation("Processing function {FunctionName}", Name);
        TOutput output = await ProcessCore(args, cancellation).ConfigureAwait(false);
        return output ?? throw new InvalidOperationException($"Function {Name} returned a null result.");
    }

    public async Task<object> Process(object args, CancellationToken cancellation)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (args is not TArgs input)
            throw new ArgumentException($"Expected {typeof(TArgs).Name} arguments.", nameof(args));

        return await Process(input, cancellation).ConfigureAwait(false);
    }

    public abstract Task<TOutput> ProcessCore(TArgs args, CancellationToken cancellation);

    protected virtual Task<TOutput> Process() => throw new NotImplementedException();

}
