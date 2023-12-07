namespace OpenScalp.QuikSharp.Messages;

public class IsSubscribedOrderBookRequest : Message<string>
{
    public IsSubscribedOrderBookRequest(string classCode, string secCode)
    {
        Data = classCode + "|" + secCode;
        Command = "IsSubscribed_Level_II_Quotes";
    }
}

public class IsSubscribedOrderBookResponse : Message<bool>
{
}