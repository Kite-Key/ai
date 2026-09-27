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
    public async Task InvalidJsonThrows()
    {
        IFunctionHandler handler = new EchoHandler();
        await Assert.ThrowsAsync<JsonException>(() => handler.Process("{oops", CancellationToken.None));
    }

    [Fact]
    public async Task HandlerFailurePropagates()
    {
        IFunctionHandler handler = new FailingHandler();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Process("""{"message":"hello"}""", CancellationToken.None));
        Assert.Equal("failed", exception.Message);
    }

    [Fact]
    public async Task CancellationIsNotConvertedToToolOutput()
    {
        IFunctionHandler handler = new CancelledHandler();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => handler.Process("""{"message":"hello"}""", CancellationToken.None));
    }

    [Fact]
    public async Task NullOutputIsRejected()
    {
        IFunctionHandler handler = new NullOutputHandler();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Process("""{"message":"hello"}""", CancellationToken.None));
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

    private sealed class CancelledHandler() : FunctionHandlerBase<Input, Output>(NullLogger.Instance)
    {
        public override string Name => "cancel";
        public override Task<Output> ProcessCore(Input args, CancellationToken cancellation)
            => throw new OperationCanceledException();
    }

    private sealed class NullOutputHandler() : FunctionHandlerBase<Input, Output>(NullLogger.Instance)
    {
        public override string Name => "null-output";
        public override Task<Output> ProcessCore(Input args, CancellationToken cancellation)
            => Task.FromResult<Output>(null!);
    }
}
