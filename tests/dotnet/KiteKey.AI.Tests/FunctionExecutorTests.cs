using System.Text.Json;
using KiteKey.AI.Abstractions.Functions;
using KiteKey.AI.Functions;

namespace KiteKey.AI.Tests;

public class FunctionExecutorTests
{
    [Fact]
    public async Task DispatchesNamedHandlerWithoutFallback()
    {
        var handler = new StubHandler("lookup");
        var executor = new FunctionExecutor([handler]);

        string? output = await executor.TryProcessFunctionCallAsync("lookup", "{\"value\":1}", CancellationToken.None);

        Assert.Equal("{\"result\":true}", output);
        Assert.Equal("{\"value\":1}", handler.Arguments);
    }

    [Fact]
    public async Task UnknownToolReturnsNullByDefault()
    {
        var executor = new FunctionExecutor([]);

        Assert.Null(await executor.TryProcessFunctionCallAsync("missing", "{}", CancellationToken.None));
    }

    [Fact]
    public async Task UnknownToolCanUseOptionalFallback()
    {
        var executor = new FunctionExecutor([], (name, args, _) => Task.FromResult<string?>($"{name}:{args}"));

        Assert.Equal("missing:{}", await executor.TryProcessFunctionCallAsync("missing", "{}", CancellationToken.None));
    }

    [Theory]
    [InlineData("unknown", "tenant")]
    [InlineData("n/a", "tenant")]
    [InlineData("na", "tenant")]
    [InlineData("", "tenant")]
    [InlineData("existing", "existing")]
    public async Task TrustedContextOnlyFillsMissingOrPlaceholderValues(string original, string expected)
    {
        var handler = new StubHandler("lookup");
        var executor = new FunctionExecutor([handler]);

        await executor.TryProcessFunctionCallAsync(
            "lookup", JsonSerializer.Serialize(new { tenantId = original }),
            new Dictionary<string, string?> { ["tenantId"] = "tenant", ["empty"] = null },
            CancellationToken.None);

        Assert.NotNull(handler.Arguments);
        var payload = JsonSerializer.Deserialize<Dictionary<string, string?>>(handler.Arguments);
        Assert.Equal(expected, payload!["tenantId"]);
        Assert.False(payload.ContainsKey("empty"));
    }

    [Fact]
    public async Task MalformedContextualArgumentsFailExplicitly()
    {
        var handler = new StubHandler("lookup");
        var executor = new FunctionExecutor([handler]);

        await Assert.ThrowsAsync<JsonException>(() => executor.TryProcessFunctionCallAsync(
            "lookup", "{invalid", new Dictionary<string, string?> { ["tenantId"] = "tenant" },
            CancellationToken.None));

        Assert.Null(handler.Arguments);
    }

    [Fact]
    public async Task NullContextualArgumentsFailExplicitly()
    {
        var executor = new FunctionExecutor([new StubHandler("lookup")]);

        await Assert.ThrowsAsync<JsonException>(() => executor.TryProcessFunctionCallAsync(
            "lookup", "null", new Dictionary<string, string?>(), CancellationToken.None));
    }

    [Fact]
    public async Task DuplicateNamesAreRejected()
    {
        var executor = new FunctionExecutor([new StubHandler("lookup"), new StubHandler("lookup")]);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => executor.TryProcessFunctionCallAsync("lookup", "{}", CancellationToken.None));
    }

    private sealed class StubHandler(string name) : IFunctionHandler
    {
        public string Name => name;
        public string? Arguments { get; private set; }
        public Task<string> Process(string args, CancellationToken cancellation)
        {
            Arguments = args;
            return Task.FromResult("{\"result\":true}");
        }
        public Task<object> Process(object args, CancellationToken cancellation) => Task.FromResult(args);
    }
}
