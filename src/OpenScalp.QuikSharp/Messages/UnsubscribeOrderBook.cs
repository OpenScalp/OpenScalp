using OpenScalp.QuikSharp.Messages;

public class UnsubscribeOrderBookRequest : Message<string>
{
    public UnsubscribeOrderBookRequest(string classCode, string secCode)
    {
        Data = classCode + "|" + secCode;
        Command = "Unsubscribe_Level_II_Quotes";
    }
}

public class UnsubscribeOrderBookResponse : Message<bool>
{
}