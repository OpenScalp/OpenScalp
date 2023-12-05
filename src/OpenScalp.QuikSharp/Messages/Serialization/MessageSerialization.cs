using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace OpenScalp.QuikSharp.Messages.Serialization;

public static class MessageSerialization
{
    private static readonly byte[] NewLine = "\n"u8.ToArray();
    private static readonly byte[] IdNameUtf8 = "id"u8.ToArray();
    private static readonly byte[] CmdNameUtf8 = "cmd"u8.ToArray();
    private static readonly byte[] TNameUtf8 = "t"u8.ToArray();
    private static readonly byte[] VNameUtf8 = "v"u8.ToArray();
    private static readonly byte[] DataNameUtf8 = "data"u8.ToArray();

    private static readonly IReadOnlyDictionary<string, Type?> DataTypes = new Dictionary<string, Type?>()
    {
        ["OnQuote"] = typeof(OrderBook),
        ["ping"] = typeof(string),
        ["getSecurityInfo"] = typeof(SecurityInfo),
        ["Subscribe_Level_II_Quotes"] = typeof(bool)
    };

    private static readonly IReadOnlyDictionary<string, Type?> MessageTypes = new Dictionary<string, Type?>()
    {
        ["OnQuote"] = typeof(OrderBook),
        ["ping"] = typeof(PingResponse),
        ["getSecurityInfo"] = typeof(SecurityInfoResponse),
        ["Subscribe_Level_II_Quotes"] = typeof(SubscribeOrderBookResponse)
    };

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        Converters =
        {
            new StringConverterWithNumberSupport(),
            new DoubleConverterWithStringSupport(),
            new DecimalConverterWithStringSupport()
        }
    };

    public static void WriteMessage(IMessage message, PipeWriter pipeWriter)
    {
        using (var utf8JsonWriter = new Utf8JsonWriter(pipeWriter))
        {
            utf8JsonWriter.WriteStartObject();
            utf8JsonWriter.WritePropertyName("id");
            utf8JsonWriter.WriteNumberValue(message.Id);
            utf8JsonWriter.WritePropertyName("cmd");
            utf8JsonWriter.WriteStringValue(message.Command);
            utf8JsonWriter.WritePropertyName("t");
            utf8JsonWriter.WriteNumberValue(message.CreatedTime);

            if (message is Message<string> messageString)
            {
                utf8JsonWriter.WritePropertyName("data");
                utf8JsonWriter.WriteStringValue(messageString.Data);
            }

            utf8JsonWriter.WriteEndObject();
        }

        pipeWriter.Write(NewLine);
    }

    public static bool TryParseMessage(in ReadOnlySequence<byte> buffer, [NotNullWhen(true)] out IMessage? message)
    {
        long id = 0;
        string? cmd = null;
        long? t = null;
        DateTime? v = null;
        object? data = null;
        Type? type;

        var reader = new Utf8JsonReader(buffer);

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName && reader.CurrentDepth == 1)
            {
                if (reader.ValueTextEquals(IdNameUtf8))
                {
                    reader.Read();

                    id = reader.GetInt64();
                }

                else if (reader.ValueTextEquals(CmdNameUtf8))
                {
                    reader.Read();

                    cmd = reader.GetString();
                }

                else if (reader.ValueTextEquals(TNameUtf8))
                {
                    reader.Read();

                    t = (long)reader.GetDouble();
                }

                else if (reader.ValueTextEquals(VNameUtf8))
                {
                    reader.Read();

                    v = reader.GetDateTime();
                }

                else if (reader.ValueTextEquals(DataNameUtf8))
                {
                    if (cmd != null)
                    {
                        if (!DataTypes.TryGetValue(cmd, out type))
                        {
                            message = null;

                            return false;
                        }


                        reader.Read();

                        data = JsonSerializer.Deserialize(ref reader, type, JsonSerializerOptions);
                    }
                    else
                    {
                        reader.Read();

                        switch (reader.TokenType)
                        {
                            case JsonTokenType.StartObject:
                                data = JsonSerializer.Deserialize<JsonObject>(ref reader);
                                break;
                            case JsonTokenType.String:
                                data = reader.GetString();
                                break;
                            case JsonTokenType.True:
                                data = true;
                                break;
                            case JsonTokenType.False:
                                data = false;
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }
            }
        }

        if (!MessageTypes.TryGetValue(cmd, out type))
        {
            message = null;

            return false;
        }

        if (data is JsonObject jsonObject)
            data = jsonObject.Deserialize(type, JsonSerializerOptions);


        if (type == typeof(OrderBook))
        {
            message = new Message<OrderBook>
            {
                Id = id,
                Command = cmd!,
                CreatedTime = t!.Value,
                ValidUntil = v,
                Data = (OrderBook)data!
            };

            return true;
        }

        if (type == typeof(PingResponse))
        {
            message = new PingResponse
            {
                Id = id,
                Command = cmd!,
                CreatedTime = t!.Value,
                ValidUntil = v,
                Data = (string)data!
            };

            return true;
        }

        if (type == typeof(SecurityInfoResponse))
        {
            message = new SecurityInfoResponse
            {
                Id = id!,
                Command = cmd!,
                CreatedTime = t!.Value,
                ValidUntil = v,
                Data = (SecurityInfo)data!
            };

            return true;
        }

        if (type == typeof(SubscribeOrderBookResponse))
        {
            message = new SubscribeOrderBookResponse
            {
                Id = id,
                Command = cmd,
                CreatedTime = t!.Value,
                ValidUntil = v,
                Data = (bool)data!
            };

            return true;
        }

        message = null;

        return false;
    }
}