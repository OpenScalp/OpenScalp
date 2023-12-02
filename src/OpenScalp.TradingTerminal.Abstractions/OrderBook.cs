namespace OpenScalp.TradingTerminal.Abstractions;

public record OrderBook(PriceQuantity[] Bids, PriceQuantity[] Asks);

public record PriceQuantity(decimal Price, decimal Quantity);