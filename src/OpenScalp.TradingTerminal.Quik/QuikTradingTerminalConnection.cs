using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenScalp.QuikSharp;
using OpenScalp.TradingTerminal.Abstractions;

namespace OpenScalp.TradingTerminal.Quik;

public class QuikTradingTerminalConnection : ITradingTerminalConnection
{
    private readonly QuikTradingTerminalConnectionOptions _options;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<QuikTradingTerminalConnection> _logger;

    public QuikTradingTerminalConnection(
        IOptions<QuikTradingTerminalConnectionOptions> options,
        ILoggerFactory loggerFactory,
        ILogger<QuikTradingTerminalConnection> logger)
    {
        _options = options.Value;
        _loggerFactory = loggerFactory;
        _logger = logger;
    }

    public async Task<ITradingTerminal> CreateAsync(CancellationToken cancellationToken = default)
    {
        var responseSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        responseSocket.NoDelay = true;
        await responseSocket.ConnectAsync(_options.ResponseHostname, _options.ResponsePort, cancellationToken);

        _logger.LogInformation("Response socket connected to {RemoteEndPoint}",
            responseSocket.RemoteEndPoint?.ToString());

        var callbackSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        callbackSocket.NoDelay = true;
        await callbackSocket.ConnectAsync(_options.CallbackHostname, _options.CallbackPort, cancellationToken);

        _logger.LogInformation("Callback socket connected to {RemoteEndPoint}",
            callbackSocket.RemoteEndPoint?.ToString());

        var quikSharpConnection = new QuikSharpConnection(
            new SocketTransport(responseSocket),
            new SocketTransport(callbackSocket),
            _options.Keepalive,
            _loggerFactory.CreateLogger<QuikSharpConnection>());

        return new QuikTradingTerminal(quikSharpConnection, _loggerFactory,
            _loggerFactory.CreateLogger<QuikTradingTerminal>());
    }
}