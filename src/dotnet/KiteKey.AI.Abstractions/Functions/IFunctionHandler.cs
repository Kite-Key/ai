namespace KiteKey.AI.Abstractions.Functions;

/// <summary>A named JSON tool callable by an assistant.</summary>
public interface IFunctionHandler
{
    string Name { get; }

    Task<string> Process(string args, CancellationToken cancellation);

    Task<object> Process(object args, CancellationToken cancellation);
}
