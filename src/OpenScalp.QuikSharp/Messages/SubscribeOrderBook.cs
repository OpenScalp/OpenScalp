namespace OpenScalp.QuikSharp.Messages;

public class SubscribeOrderBookRequest : Message<string>
{
    public SubscribeOrderBookRequest(string classCode, string secCode)
    {
        Data = classCode + "|" + secCode;
        Command = "Subscribe_Level_II_Quotes";
    }
}

public class SubscribeOrderBookResponse : Message<bool>
{
}