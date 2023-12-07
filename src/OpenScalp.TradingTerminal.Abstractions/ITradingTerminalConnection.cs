namespace OpenScalp.TradingTerminal.Abstractions;

public interface ITradingTerminalConnection
{
    Task<ITradingTerminal> CreateAsync(CancellationToken cancellationToken = default);
}