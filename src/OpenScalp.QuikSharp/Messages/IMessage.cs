using System.Text.Json.Serialization;

namespace OpenScalp.QuikSharp.Messages;

public interface IMessage
{
    long Id { get; set; }
    string? Command { get; set; }
    long CreatedTime { get; set; }
    DateTime? ValidUntil { get; set; }
}

public abstract class BaseMessage : IMessage
{
    protected static readonly long Epoch = (new DateTime(1970, 1, 1, 3, 0, 0, 0)).Ticks / 10000L;

    [JsonPropertyName("id")] public long Id { get; set; }

    [JsonPropertyName("cmd")] public string? Command { get; set; }

    [JsonPropertyName("t")] public long CreatedTime { get; set; }

    [JsonPropertyName("v")] public DateTime? ValidUntil { get; set; }
}

public class Message<T> : BaseMessage
{
    [JsonPropertyName("data")] public T Data { get; set; }
}