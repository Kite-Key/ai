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
    Call["Function name + JSON + optional context"] --> Merge["Fill absent/placeholder string arguments"]
    Merge --> Find{"Exactly one native handler?"}
    Find -- "yes" --> Deserialize["Deserialize typed arguments"]
    Deserialize --> Invoke["ProcessCore; serialize JSON output"]
    Find -- "no match" --> Fallback{"Optional callback?"}
    Fallback -- "yes" --> External["Call consumer integration"]
    Fallback -- "no" --> Missing["Return null"]
    Find -- "duplicate" --> Error["Throw InvalidOperationException"]
```

The contextual overload retains Delphinium's placeholder handling and malformed-JSON passthrough. JSON handlers return a serialized `message` error on failures; typed calls propagate errors. A consumer decides whether to expose exception messages to its users.

## Boundaries

```mermaid
flowchart LR
    A["KiteKey.AI.Abstractions"] --> B["KiteKey.AI"]
    L["Microsoft.Extensions.Logging.Abstractions"] --> B
    B --> C["Consumer application"]
    C --> D["Provider SDK / database / webhooks / audio"]
```

There are no references from either package to Delphinium, Entity Framework, Azure, or Twilio. `RequiredToolCall` carries SDK-neutral call details; provider adapters translate their SDK types at the boundary. Delphinium's browser audio adapter depends on application-specific transcript and stream types, so audio and Twilio remain outside these packages.
