using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using OpenScalp.TradingTerminal.Abstractions;

namespace OpenScalp.TradingTerminal.Quik;

public class QuikSubscriptionTrade : ISubscriptionTrade
{
    private readonly Func<ValueTask> _disposeAsyncFn;
    private readonly ILogger<QuikSubscriptionTrade> _logger;

    public QuikSubscriptionTrade(ChannelReader<Trade> trades, Func<ValueTask> disposeAsyncFn,
        ILogger<QuikSubscriptionTrade> logger)
    {
        Trades = trades;
        _disposeAsyncFn = disposeAsyncFn;
        _logger = logger;
    }

    public async ValueTask DisposeAsync()
    {
        await _disposeAsyncFn();
    }

    public ChannelReader<Trade> Trades { get; }
}