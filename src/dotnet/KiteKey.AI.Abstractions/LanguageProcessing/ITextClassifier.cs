namespace KiteKey.AI.Abstractions.LanguageProcessing;

/// <summary>Classifies text into an application-provided enum.</summary>
public interface ITextClassifier
{
    int MaxProfileLength { get; }

    Task<TEnum?> Classify<TEnum>(string caller, string categoryName, string kind, string purpose, string textToClassify, CancellationToken cancellationToken)
        where TEnum : struct, Enum;
}
