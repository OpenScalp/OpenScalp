namespace OpenScalp.QuikSharp.Messages;

public class QuikDateTime
{
    /// <summary>
    /// Микросекунды игнорируются в текущей версии.
    /// </summary>
    public int mcs { get; set; }

    /// <summary>
    ///
    /// </summary>
    public int ms { get; set; }

    /// <summary>
    ///
    /// </summary>
    public int sec { get; set; }

    /// <summary>
    ///
    /// </summary>
    public int min { get; set; }

    /// <summary>
    ///
    /// </summary>
    public int hour { get; set; }

    /// <summary>
    ///
    /// </summary>
    public int day { get; set; }

    /// <summary>
    /// Monday is 1
    /// </summary>
    public int week_day { get; set; }

    /// <summary>
    ///
    /// </summary>
    public int month { get; set; }

    /// <summary>
    ///
    /// </summary>
    public int year { get; set; }


    /// <summary>
    ///
    /// </summary>
    /// <param name="qdt"></param>
    /// <returns></returns>
    public static explicit operator DateTime(QuikDateTime qdt)
    {
        var dt = new DateTime(qdt.year, qdt.month, qdt.day, qdt.hour, qdt.min, qdt.sec, qdt.ms);
        return dt; //dt.AddTicks(qdt.mcs * 10);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="dt"></param>
    /// <returns></returns>
    public static explicit operator QuikDateTime(DateTime dt)
    {
        return new QuikDateTime
        {
            year = dt.Year,
            month = dt.Month,
            day = dt.Day,
            hour = dt.Hour,
            min = dt.Minute,
            sec = dt.Second,
            ms = dt.Millisecond,
            mcs = 0, // ((int)(dt.TimeOfDay.Ticks) - ((dt.Hour * 60 + dt.Minute) * 60 + dt.Second) * 1000 * 10000) / 10,
            week_day = (int)dt.DayOfWeek
        };
    }
}