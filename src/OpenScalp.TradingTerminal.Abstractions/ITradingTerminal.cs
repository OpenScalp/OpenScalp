namespace OpenScalp.TradingTerminal.Abstractions;

public interface ITradingTerminal : IAsyncDisposable
{
    Task<ISubscriptionOrderBook> SubscribeOrderBookAsync(string ticket, CancellationToken cancellationToken = default);
    Task<ISubscriptionTrade> SubscribeTradeAsync(string ticket, CancellationToken cancellationToken = default);
    Task<TicketInfo> GetTicketInfoAsync(string ticket, CancellationToken cancellationToken = default);
}