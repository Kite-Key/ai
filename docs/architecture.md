# Architecture

KiteKey.AI extracts only reusable tool and text-processing primitives. The abstractions assembly has no third-party dependencies; the implementation assembly needs logging abstractions only. Model providers and application persistence stay with consumers.

```mermaid
flowchart LR
    Model["Any model/provider adapter"] --> Executor["IFunctionExecutor / FunctionExecutor"]
    Context["Trusted channel context"] --> Executor
    Executor --> Handler["IFunctionHandler"]
    Handler --> Base["FunctionHandlerBase<TArgs,TOutput>"]
    Base --> Application["Consumer-owned tool logic"]
    Executor -. "optional unknown-tool callback" .-> External["Consumer-owned external dispatch"]
    Model --> Text["IGptAgent / ISummarizer / ITextClassifier"]
    Model --> Chat["IChatClient"]
```

## Dispatch

```mermaid
flowchart TD
    Call["Function name + JSON + optional context"] --> Merge{"Valid contextual JSON?"}
    Merge -- "no" --> Error["Throw JSON exception"]
    Merge -- "yes" --> Fill["Fill absent/placeholder string arguments"]
    Fill --> Find{"Exactly one native handler?"}
    Find -- "yes" --> Deserialize["Deserialize typed arguments"]
    Deserialize --> Invoke["ProcessCore; serialize JSON output"]
    Find -- "no match" --> Fallback{"Optional callback?"}
    Fallback -- "yes" --> External["Call consumer integration"]
    Fallback -- "no" --> Missing["Return null"]
    Find -- "duplicate" --> Duplicate["Throw InvalidOperationException"]
```

The contextual overload retains Delphinium's placeholder handling but rejects malformed or null JSON instead of silently forwarding it. JSON and typed handler calls propagate exceptions, including cancellation; null results throw. Consumers decide how to communicate errors to their users at the transport boundary.

## Boundaries

```mermaid
flowchart LR
    C["Consumer application"] --> B["KiteKey.AI"]
    B --> A["KiteKey.AI.Abstractions"]
    B --> L["Microsoft.Extensions.Logging.Abstractions"]
    C --> D["Provider SDK / database / webhooks / audio"]
```

## Voice transport

```mermaid
flowchart LR
    Voice["Voice provider adapter"] --> Interface["IHumanAudioClient"]
    Interface --> Neutral["ConversationTranscriptMessage (neutral)"]
    Neutral --> Mapper["App-owned transcript mapping"]
    Mapper --> App["Delphinium transcript model / UI / persistence"]
    Interface --> Transport["App-owned WebSocket or phone transport"]
```

`IHumanAudioClient` and its transcript payload reside in Abstractions. The app implements the transport and translates neutral transcript fields into its own DTO; no persistence type crosses into the package. Voice adapters use `SendAudio(byte[])`, converting provider-specific binary wrappers at their edge. Browser audio and Twilio implementations remain in the app.

There are no references from either package to Delphinium, Entity Framework, Azure, or Twilio. `RequiredToolCall` carries SDK-neutral call details; provider adapters translate their SDK types at the boundary.
