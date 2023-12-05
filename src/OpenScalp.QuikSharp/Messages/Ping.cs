namespace OpenScalp.QuikSharp.Messages;

public class PingRequest : Message<string>
{
    public PingRequest()
    {
        Data = "Ping";
        Command = "ping";
    }
}

public class PingResponse : Message<string>
{
}