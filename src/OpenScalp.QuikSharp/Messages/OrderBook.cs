namespace OpenScalp.QuikSharp.Messages;

public class OrderBook
{
    /// <summary>
    /// Строка стакана
    /// </summary>
    public class PriceQuantity
    {
        /// <summary>
        /// Цена покупки / продажи
        /// </summary>
        public decimal price { get; set; }

        /// <summary>
        /// Количество в лотах
        /// </summary>
        public decimal quantity { get; set; }
    }

    /// <summary>
    /// Код класса
    /// </summary>
    public string class_code { get; set; }

    /// <summary>
    /// Код бумаги
    /// </summary>
    public string sec_code { get; set; }

    /// <summary>
    /// time in msec from lua epoch
    /// </summary>
    public long LuaTimeStamp { get; set; }

    /// <summary>
    /// Result of getInfoParam("SERVERTIME") right before getQuoteLevel2 call
    /// </summary>
    public string server_time { get; set; }

    /// <summary>
    /// Количество котировок покупки
    /// </summary>
    public double bid_count { get; set; }

    /// <summary>
    /// Количество котировок продажи
    /// </summary>
    public double offer_count { get; set; }

    /// <summary>
    /// Котировки спроса (покупки)
    /// </summary>
    public PriceQuantity[] bid { get; set; }

    /// <summary>
    /// Котировки предложений (продажи)
    /// </summary>
    public PriceQuantity[] offer { get; set; }
}