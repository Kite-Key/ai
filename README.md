# KiteKey.AI

Small, provider-neutral .NET 8 libraries extracted from Delphinium's AI service. MIT licensed.

| Package | Responsibility |
| --- | --- |
| `KiteKey.AI.Abstractions` | Tool dispatch, chat output, text-processing and voice transport contracts; no NuGet dependencies |
| `KiteKey.AI` | JSON handler base and named tool executor; depends on Abstractions and Microsoft.Extensions.Logging.Abstractions |

Install `KiteKey.AI` to implement and dispatch tools; install just `KiteKey.AI.Abstractions` for contracts. Neither package registers a model provider or makes network calls.

```csharp
using KiteKey.AI.Abstractions.Functions;
using KiteKey.AI.Functions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public sealed record WeatherArgs(string City);
public sealed record WeatherResult(string Forecast);

public sealed class WeatherHandler(ILogger<WeatherHandler> logger)
    : FunctionHandlerBase<WeatherArgs, WeatherResult>(logger)
{
    public override string Name => "weather";

    public override Task<WeatherResult> ProcessCore(WeatherArgs args, CancellationToken cancellation)
        => Task.FromResult(new WeatherResult($"Weather for {args.City}"));
}

IFunctionExecutor executor = new FunctionExecutor([new WeatherHandler(NullLogger<WeatherHandler>.Instance)]);
string? json = await executor.TryProcessFunctionCallAsync("weather", """{"city":"Boston"}""", CancellationToken.None);
```

Pass an optional `Func<string, string, CancellationToken, Task<string?>>` as the executor's second argument to dispatch unknown tools elsewhere. Without one, unknown tools return `null`; there is **no mandatory webhook**. The contextual overload fills missing or placeholder JSON string arguments with trusted channel context. Duplicate handler names throw. Malformed contextual JSON and handler failures propagate exceptions rather than returning success-shaped error JSON.

## Migration from Delphinium

| Delphinium source | New type |
| --- | --- |
| `Models.Functions.IFunctionHandler`, `RequiredToolCall` | `KiteKey.AI.Abstractions.Functions` |
| `Assistants.IFunctionExecutor` | `KiteKey.AI.Abstractions.Functions.IFunctionExecutor` |
| `Assistants.FunctionExecutor` | `KiteKey.AI.Functions.FunctionExecutor` |
| `Functions.FunctionHandlerBase<TArgs,TOutput>` | `KiteKey.AI.Functions.FunctionHandlerBase<TArgs,TOutput>` |
| `Models.Chats.IChatClient` | `KiteKey.AI.Abstractions.Chats.IChatClient` |
| `LanguageProcessing.IGptAgent`, `ISummarizer`, `ITextClassifier` | `KiteKey.AI.Abstractions.LanguageProcessing` |
| `Models.Voice.IHumanAudioClient` and app transcript DTO | `KiteKey.AI.Abstractions.Voice.IVoiceAudioClient` and `VoiceTranscript` (adapter required) |

Namespaces and executor constructor signatures change: update imports and register `IFunctionExecutor` against `FunctionExecutor` with your `IEnumerable<IFunctionHandler>`. The signatures of `IFunctionHandler`, `IFunctionExecutor`, `RequiredToolCall`, and `FunctionHandlerBase<TArgs,TOutput>` remain source-compatible after namespace changes. Delphinium's handler base used to turn every exception (including cancellation) into `{"message":"..."}` and returned `"Not found"` on null results; the new base propagates exceptions and rejects null results. Contextual dispatch likewise rejects malformed JSON instead of silently forwarding it. Consumers should handle these errors at their transport boundary. A former webhook implementation can be explicitly adapted through the optional fallback callback. Existing Delphinium entity-backed function definitions, Azure assistants/voice integrations, native app-specific functions, and Twilio are **not** included. The browser socket audio class remains deferred; its `ChannelStream` and app event handling are not shared. See [architecture](docs/architecture.md).

### Audio transport boundary

`IVoiceAudioClient` exposes only the conversation ID, speaking state, microphone stream, audio output, playback clearing, status, and transcript delivery used by a voice provider. Its cancellation-aware signatures match the standalone Azure VoiceLive transport, allowing future namespace-only migration once these packages are published. It does not copy the app's lifecycle/disposal APIs or require `BinaryData`; provider adapters call `SendAudioAsync(binaryData.ToArray(), cancellation)`. In a Delphinium audio adapter, map the neutral transcript into the existing application DTO at the boundary:

```csharp
public Task SendTranscriptAsync(KiteKey.AI.Abstractions.Voice.VoiceTranscript entry, CancellationToken cancellation)
    => SendToApplicationAsync(new Delphinium.Data.Shared.Models.AudioProcessing.ConversationTranscriptMessage
    {
        MessageId = entry.MessageId,
        Speaker = entry.Speaker,
        Text = entry.Text,
        IsFinal = entry.IsFinal,
        Timestamp = entry.Timestamp,
        ToolName = entry.ToolCall?.Name,
        ToolArguments = entry.ToolCall?.Arguments,
        ToolOutput = entry.ToolCall?.Output
    });
```

The example is a mapping inside a consumer-owned adapter; `SendToApplicationAsync` represents that application's existing transcript sink. No Delphinium type is referenced by either NuGet package.

## Build and release

Run `dotnet test KiteKey.AI.sln -c Release`, then `dotnet pack KiteKey.AI.sln -c Release --no-build -o artifacts`. CI runs both on PRs and main. To release, update both project versions, merge to main, then push a matching `vX.Y.Z` tag. The [release workflow](.github/workflows/release.yml) checks tag/version agreement, tests and packs, then uses NuGet trusted publishing (OIDC) to publish both packages.

Before the first release, add a **KiteKey NuGet organization** trusted publishing policy for GitHub owner `Kite-Key`, repository `ai`, workflow `release.yml` (no environment), and grant its NuGet account permission to publish `KiteKey.AI*`. Set repository variable `NUGET_USERNAME` to the NuGet.org **username** whose organization membership and policy can publish; no permanent API key is needed. Package ownership and policy setup are external prerequisites.
