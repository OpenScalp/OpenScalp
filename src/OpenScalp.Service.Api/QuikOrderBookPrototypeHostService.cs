using System.Net.Sockets;
using Microsoft.AspNetCore.SignalR;
using OpenScalp.QuikSharp;
using OpenScalp.QuikSharp.Messages;
using OpenScalp.TradingTerminal.Abstractions;
using OrderBook = OpenScalp.TradingTerminal.Abstractions.OrderBook;
using QuikOrderBook = OpenScalp.QuikSharp.Messages.OrderBook;

namespace OpenScalp.Service.Api;

public class QuikOrderBookPrototypeHostService : BackgroundService
{
    private readonly IHubContext<TradingHub, ITerminalClient> _tradingHub;
    private readonly ITradingTerminalConnection _tradingTerminalConnection;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<QuikOrderBookPrototypeHostService> _logger;

    public QuikOrderBookPrototypeHostService(IHubContext<TradingHub, ITerminalClient> tradingHub,
        ITradingTerminalConnection tradingTerminalConnection,
        ILoggerFactory loggerFactory,
        ILogger<QuikOrderBookPrototypeHostService> logger)
    {
        _tradingHub = tradingHub;
        _tradingTerminalConnection = tradingTerminalConnection;
        _loggerFactory = loggerFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tradingTerminal = await _tradingTerminalConnection.CreateAsync(stoppingToken);

        await using var subscriptionOrderBook = await tradingTerminal.SubscribeOrderBookAsync("NGZ3", stoppingToken);

        await foreach (var order in subscriptionOrderBook.Orders.ReadAllAsync(stoppingToken))
        {
            await _tradingHub.Clients.All.OnOrderBookUpdate(order);
        }
    }
}