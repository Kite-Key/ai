using System.Text.Json;
using KiteKey.AI.Abstractions.Functions;
using KiteKey.AI.Functions;
using Microsoft.Extensions.Logging.Abstractions;

namespace KiteKey.AI.Tests;

public class FunctionHandlerBaseTests
{
    [Fact]
    public async Task ParsesWebJsonAndSerializesOutput()
    {
        IFunctionHandler handler = new EchoHandler();

        string output = await handler.Process("{\"MESSAGE\":\"hello\"}", CancellationToken.None);

        Assert.Equal("hello", JsonSerializer.Deserialize<Dictionary<string, string>>(output)!["message"]);
    }

    [Fact]
    public async Task EmptyArgumentsUseParameterlessOperation()
    {
        IFunctionHandler handler = new EchoHandler();
        string output = await handler.Process("", CancellationToken.None);
        Assert.Contains("empty", output);
    }

    [Fact]
    public async Task InvalidJsonProducesErrorResult()
    {
        IFunctionHandler handler = new EchoHandler();
        string output = await handler.Process("{oops", CancellationToken.None);
        Assert.True(JsonSerializer.Deserialize<Dictionary<string, string>>(output)!.ContainsKey("message"));
    }

    [Fact]
    public async Task HandlerFailureProducesJsonError()
    {
        IFunctionHandler handler = new FailingHandler();

        string output = await handler.Process("""{"message":"hello"}""", CancellationToken.None);

        Assert.Equal("failed", JsonSerializer.Deserialize<Dictionary<string, string>>(output)!["message"]);
    }

    [Fact]
    public async Task TypedAndObjectOverloadsInvokeCore()
    {
        var handler = new EchoHandler();
        Assert.Equal("hello", (await handler.Process(new Input("hello"), CancellationToken.None)).Message);
        Assert.Equal("world", ((Output)await ((IFunctionHandler)handler).Process(new Input("world"), CancellationToken.None)).Message);
        await Assert.ThrowsAsync<ArgumentException>(
            () => ((IFunctionHandler)handler).Process(new object(), CancellationToken.None));
    }

    private record Input(string Message);
    private record Output(string Message);

    private sealed class EchoHandler() : FunctionHandlerBase<Input, Output>(NullLogger.Instance)
    {
        public override string Name => "echo";
        public override Task<Output> ProcessCore(Input args, CancellationToken cancellation) => Task.FromResult(new Output(args.Message));
        protected override Task<Output> Process() => Task.FromResult(new Output("empty"));
    }

    private sealed class FailingHandler() : FunctionHandlerBase<Input, Output>(NullLogger.Instance)
    {
        public override string Name => "fail";
        public override Task<Output> ProcessCore(Input args, CancellationToken cancellation)
            => throw new InvalidOperationException("failed");
    }
}
