using System.Buffers;
using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using OpenScalp.QuikSharp.Messages;
using static OpenScalp.QuikSharp.Messages.Serialization.MessageSerialization;

namespace OpenScalp.QuikSharp;

public class QuikSharpConnection
{
    private readonly ITransport _responseTransport;
    private readonly ITransport _callbackTransport;
    private readonly TimeSpan _pingInterval;
    private readonly ILogger<QuikSharpConnection> _logger;
    private readonly Channel<IMessage> _inputChannel;
    private readonly Channel<IMessage> _outputChannel;
    private PeriodicTimer _timer;
    private Task _timerTask;
    private readonly ConcurrentDictionary<long, TaskCompletionSource<IMessage>> _outstandingRequests = new();
    private long _correlationId;

    public QuikSharpConnection(ITransport responseTransport,
        ITransport callbackTransport,
        TimeSpan pingInterval,
        ILogger<QuikSharpConnection> logger)
    {
        _responseTransport = responseTransport;
        _callbackTransport = callbackTransport;
        _pingInterval = pingInterval;
        _logger = logger;

        _inputChannel = Channel.CreateUnbounded<IMessage>();
        _outputChannel = Channel.CreateUnbounded<IMessage>();

        _timer = new PeriodicTimer(pingInterval);
        _timerTask = SendPingMessage(_timer);
        _ = PipelineToChannelAsync();
        _ = ChannelToPipelineAsync();
        _ = PipelineToChannelCallbackAsync();
    }

    public IAsyncEnumerable<IMessage> CallbackMessages => _inputChannel.Reader.ReadAllAsync();

    public void Complete() => _outputChannel.Writer.Complete();

    public async Task<SecurityInfo> GetSecurityInfo(string classCode, string secCode)
    {
        var request = new SecurityInfoRequest(classCode, secCode);

        var response = await SendMessageAsync<SecurityInfoRequest, SecurityInfoResponse>(request);

        return response.Data;
    }

    public async Task<bool> SubscribeOrderBook(string classCode, string secCode)
    {
        var request = new SubscribeOrderBookRequest(classCode, secCode);

        var response = await SendMessageAsync<SubscribeOrderBookRequest, SubscribeOrderBookResponse>(request);

        return response.Data;
    }

    public async Task<bool> UnsubscribeOrderBook(string classCode, string secCode)
    {
        var request = new UnsubscribeOrderBookRequest(classCode, secCode);

        var response = await SendMessageAsync<UnsubscribeOrderBookRequest, UnsubscribeOrderBookResponse>(request);

        return response.Data;
    }

    public async Task<bool> IsSubscribedOrderBook(string classCode, string secCode)
    {
        var request = new IsSubscribedOrderBookRequest(classCode, secCode);

        var response = await SendMessageAsync<IsSubscribedOrderBookRequest, IsSubscribedOrderBookResponse>(request);

        return response.Data;
    }

    private async Task SendPingMessage(PeriodicTimer periodicTimer)
    {
        while (await periodicTimer.WaitForNextTickAsync())
        {
            var request = new PingRequest();

            var response = await SendMessageAsync<PingRequest, PingResponse>(request);

            _logger.LogInformation("{PingResponse}", response.Data);
        }
    }

    private async Task<TResponse> SendMessageAsync<TRequest, TResponse>(TRequest request)
        where TRequest : Message<string>
    {
        var tcs = new TaskCompletionSource<IMessage>(TaskCreationOptions.RunContinuationsAsynchronously);

        request.Id = Interlocked.Increment(ref _correlationId);

        _outstandingRequests.TryAdd(request.Id, tcs);

        var response = await SendMessageAndWaitForResponseAsync();

        return response;

        async Task<TResponse> SendMessageAndWaitForResponseAsync()
        {
            await _outputChannel.Writer.WriteAsync(request);

            _timer.Period = _pingInterval;

            return (TResponse)await tcs.Task;
        }
    }

    private async Task PipelineToChannelAsync()
    {
        try
        {
            while (true)
            {
                var data = await _responseTransport.Input.ReadAsync();
                var buffer = data.Buffer;

                while (TryReadLine(ref buffer, out var line))
                {
                    ProcessLine(line);
                }

                _responseTransport.Input.AdvanceTo(buffer.Start, buffer.End);


                if (data.IsCompleted)
                {
                    Close();
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Close(ex);
        }
    }

    private async Task PipelineToChannelCallbackAsync()
    {
        try
        {
            while (true)
            {
                var data = await _callbackTransport.Input.ReadAsync();
                var buffer = data.Buffer;

                while (TryReadLine(ref buffer, out var line))
                {
                    if (TryParseMessage(line, out var message))
                    {
                        _inputChannel.Writer.TryWrite(message);
                    }
                }

                _callbackTransport.Input.AdvanceTo(buffer.Start, buffer.End);


                if (data.IsCompleted)
                {
                    Close();
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Close(ex);
        }
    }

    private void ProcessLine(in ReadOnlySequence<byte> buffer)
    {
        if (!TryParseMessage(buffer, out var message))
            throw new InvalidOperationException("Protocol violation.");

        if (!_outstandingRequests.TryRemove(message.Id, out var request))
            throw new InvalidOperationException("Protocol violation.");

        request.TrySetResult(message);
    }

    private bool TryReadLine(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> line)
    {
        var position = buffer.PositionOf((byte)'\n');

        if (position == null)
        {
            line = default;
            return false;
        }

        line = buffer.Slice(0, position.Value);
        buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
        return true;
    }

    private async Task ChannelToPipelineAsync()
    {
        try
        {
            await foreach (var message in _outputChannel.Reader.ReadAllAsync())
            {
                WriteMessage(message, _responseTransport.Output);

                var flushResult = await _responseTransport.Output.FlushAsync();
                if (flushResult.IsCanceled)
                    break;
            }

            Close();
        }
        catch (Exception exception)
        {
            Close(exception);
        }
    }

    private void Close(Exception? exception = null)
    {
        _timer.Dispose();
        _inputChannel.Writer.TryComplete(exception);
        _responseTransport.Output.CancelPendingFlush();
        _responseTransport.Output.Complete();
    }
}