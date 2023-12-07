using OpenScalp.TradingTerminal.Abstractions;

namespace OpenScalp.Service.Api;

public interface ITerminalClient
{
    Task OnOrderBookUpdate(OrderBook orderBook);
    Task OnTrade(Trade trade);
}