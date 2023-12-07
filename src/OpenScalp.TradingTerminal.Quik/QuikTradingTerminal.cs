using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using OpenScalp.QuikSharp;
using OpenScalp.QuikSharp.Messages;
using OpenScalp.TradingTerminal.Abstractions;
using OrderBook = OpenScalp.TradingTerminal.Abstractions.OrderBook;
using QuikOrderBook = OpenScalp.QuikSharp.Messages.OrderBook;

namespace OpenScalp.TradingTerminal.Quik;

public class QuikTradingTerminal : ITradingTerminal
{
    private readonly QuikSharpConnection _quikSharpConnection;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<QuikTradingTerminal> _logger;
    private Task _readCallbackMessagesLoopTask;
    private readonly ConcurrentDictionary<string, Channel<OrderBook>> _subscribeOrderBooks = new();
    private readonly ConcurrentDictionary<string, Channel<Trade>> _subscribeAllTrades = new();

    public QuikTradingTerminal(
        QuikSharpConnection quikSharpConnection,
        ILoggerFactory loggerFactory,
        ILogger<QuikTradingTerminal> logger)
    {
        _quikSharpConnection = quikSharpConnection;
        _loggerFactory = loggerFactory;
        _logger = logger;
        _readCallbackMessagesLoopTask = ReadCallbackMessagesAsync(quikSharpConnection.CallbackMessages);
    }

    private async Task ReadCallbackMessagesAsync(IAsyncEnumerable<IMessage> messages)
    {
        await foreach (var message in messages)
        {
            if (message is Message<QuikOrderBook> messageOrderBook)
            {
                if (_subscribeOrderBooks.TryGetValue(messageOrderBook.Data.sec_code, out var channel))
                {
                    var bids = messageOrderBook.Data.bid.Select(b => new PriceQuantity(b.price, b.quantity)).ToArray();

                    var asks = messageOrderBook.Data.offer.Select(b => new PriceQuantity(b.price, b.quantity))
                        .ToArray();

                    channel.Writer.TryWrite(new OrderBook(bids, asks));
                }
            }

            if (message is Message<AllTrade> messageAllTrade)
            {
                if (_subscribeAllTrades.TryGetValue(messageAllTrade.Data.SecCode, out var channel))
                {
                    channel.Writer.TryWrite(new Trade((decimal)messageAllTrade.Data.Price,
                        (decimal)messageAllTrade.Data.Qty,
                        messageAllTrade.Data.Flags == AllTradeFlags.Buy ? TradeType.Buy : TradeType.Sell));
                }
            }
        }
    }

    public ValueTask DisposeAsync()
    {
        _quikSharpConnection.Complete();


        throw new NotImplementedException();
    }

    public async Task<ISubscriptionOrderBook> SubscribeOrderBookAsync(string ticket,
        CancellationToken cancellationToken = default)
    {
        var isSubscribedOrderBook = await _quikSharpConnection.IsSubscribedOrderBook("SPBFUT", ticket);

        if (isSubscribedOrderBook == false)
        {
            isSubscribedOrderBook = await _quikSharpConnection.SubscribeOrderBook("SPBFUT", ticket);

            if (isSubscribedOrderBook == false)
            {
                throw new InvalidOperationException("SubscribeOrderBook error");
            }
        }

        var channel = _subscribeOrderBooks.GetOrAdd(ticket, Channel.CreateUnbounded<OrderBook>());

        return new QuikSubscriptionOrderBook(channel.Reader, async () =>
        {
            await _quikSharpConnection.UnsubscribeOrderBook("SPBFUT", ticket);

            channel.Writer.Complete();

            _subscribeOrderBooks.TryRemove(ticket, out _);
        }, _loggerFactory.CreateLogger<QuikSubscriptionOrderBook>());
    }

    public Task<ISubscriptionTrade> SubscribeTradeAsync(string ticket, CancellationToken cancellationToken = default)
    {
        var channel = _subscribeAllTrades.GetOrAdd(ticket, Channel.CreateUnbounded<Trade>());

        return Task.FromResult<ISubscriptionTrade>(new QuikSubscriptionTrade(channel.Reader, () =>
        {
            channel.Writer.Complete();

            _subscribeOrderBooks.TryRemove(ticket, out _);

            return ValueTask.CompletedTask;
        }, _loggerFactory.CreateLogger<QuikSubscriptionTrade>()));
    }

    public async Task<TicketInfo> GetTicketInfoAsync(string ticket, CancellationToken cancellationToken = default)
    {
        var securityInfo = await _quikSharpConnection.GetSecurityInfo("SPBFUT", ticket);

        return new TicketInfo((decimal)securityInfo.MinPriceStep);
    }
}