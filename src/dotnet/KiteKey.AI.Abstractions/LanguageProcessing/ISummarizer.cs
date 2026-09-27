namespace KiteKey.AI.Abstractions.LanguageProcessing;

/// <summary>Creates AI-generated text summaries.</summary>
public interface ISummarizer
{
    int MaxProfileLength { get; }

    Task<string?> GenerateSummary(string caller, string kind, string audience, string purpose, string constraints, string profile, bool doReview, CancellationToken cancellationToken);
}
