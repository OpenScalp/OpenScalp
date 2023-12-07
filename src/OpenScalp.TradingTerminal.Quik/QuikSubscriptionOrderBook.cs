using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using OpenScalp.TradingTerminal.Abstractions;

namespace OpenScalp.TradingTerminal.Quik;

public class QuikSubscriptionOrderBook : ISubscriptionOrderBook
{
    private readonly Func<Task> _disposeAsyncFn;
    private readonly ILogger<QuikSubscriptionOrderBook> _logger;

    public QuikSubscriptionOrderBook(ChannelReader<OrderBook> orders, Func<Task> disposeAsyncFn,
        ILogger<QuikSubscriptionOrderBook> logger)
    {
        Orders = orders;
        _disposeAsyncFn = disposeAsyncFn;
        _logger = logger;
    }

    public async ValueTask DisposeAsync()
    {
        await _disposeAsyncFn();
    }

    public ChannelReader<OrderBook> Orders { get; }
}