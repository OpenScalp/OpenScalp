namespace OpenScalp.TradingTerminal.Quik;

public class QuikTradingTerminalConnectionOptions
{
    public string ResponseHostname { get; set; }
    public int ResponsePort { get; set; }
    public string CallbackHostname { get; set; }
    public int CallbackPort { get; set; }
    public TimeSpan Keepalive { get; set; }
}