namespace KiteKey.AI.Abstractions.LanguageProcessing;

/// <summary>Provider-neutral text generation contract (historical name retained for migration).</summary>
public interface IGptAgent
{
    int MaxInputLength { get; }

    Task<string?> GenerateResponse(string caller, string purpose, string audience, string constraints, string dataKind, string data, CancellationToken cancellationToken);
}
