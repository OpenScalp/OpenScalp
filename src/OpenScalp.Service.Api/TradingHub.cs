using Microsoft.AspNetCore.SignalR;
using OpenScalp.TradingTerminal.Abstractions;

namespace OpenScalp.Service.Api;

public class TradingHub : Hub<ITerminalClient>
{
    public async Task SendUpdatedOrderBookToClients(OrderBook orderBook)
    {
        await Clients.All.OnOrderBookUpdate(orderBook);
    }

    public async Task SendTradeToClients(Trade trade)
    {
        await Clients.All.OnTrade(trade);
    }
}