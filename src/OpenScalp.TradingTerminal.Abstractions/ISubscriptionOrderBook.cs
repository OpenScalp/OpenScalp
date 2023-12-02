using System.Threading.Channels;

namespace OpenScalp.TradingTerminal.Abstractions;

public interface ISubscriptionOrderBook : IAsyncDisposable
{
    ChannelReader<OrderBook> Orders { get; }
}