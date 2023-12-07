namespace OpenScalp.TradingTerminal.Abstractions;

public record Trade(decimal Price, decimal Quantity, TradeType TradeType);

public enum TradeType
{
    Sell,
    Buy
}