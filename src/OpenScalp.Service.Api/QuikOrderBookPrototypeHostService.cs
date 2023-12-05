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
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<QuikOrderBookPrototypeHostService> _logger;

    public QuikOrderBookPrototypeHostService(IHubContext<TradingHub, ITerminalClient> tradingHub,
        ILoggerFactory loggerFactory,
        ILogger<QuikOrderBookPrototypeHostService> logger)
    {
        _tradingHub = tradingHub;
        _loggerFactory = loggerFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var responseSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        responseSocket.NoDelay = true;
        await responseSocket.ConnectAsync("localhost", 34130);

        var callbackSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        responseSocket.NoDelay = true;
        await callbackSocket.ConnectAsync("localhost", 34131);


        var quikSharpConnection = new QuikSharpConnection(
            new SocketTransport(responseSocket),
            new SocketTransport(callbackSocket),
            TimeSpan.FromSeconds(5),
            _loggerFactory.CreateLogger<QuikSharpConnection>());

        _ = await quikSharpConnection.SubscribeOrderBook("SPBFUT", "NGZ3");

        await foreach (var message in quikSharpConnection.CallbackMessages)
        {
            if (message is Message<QuikOrderBook> messageOrderBook && messageOrderBook.Data.sec_code == "NGZ3")
            {
                var bids = messageOrderBook.Data.bid.Select(b => new PriceQuantity(b.price, b.quantity)).ToArray();

                var asks = messageOrderBook.Data.offer.Select(b => new PriceQuantity(b.price, b.quantity)).ToArray();

                await _tradingHub.Clients.All.OnOrderBookUpdate(new OrderBook(bids, asks));
            }
        }
    }
}