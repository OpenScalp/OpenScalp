using System.Threading.Channels;

namespace OpenScalp.TradingTerminal.Abstractions;

public interface ISubscriptionTrade : IAsyncDisposable
{
    ChannelReader<Trade> Trades { get; }
}