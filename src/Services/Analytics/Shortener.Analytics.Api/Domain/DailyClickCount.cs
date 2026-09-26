namespace Shortener.Analytics.Api.Domain;

/// <summary>The number of clicks a short code received on a given day.</summary>
public sealed class DailyClickCount
{
    private DailyClickCount(string code, DateOnly date, int count)
    {
        Code = code;
        Date = date;
        Count = count;
    }

    /// <summary>The short code this count is for.</summary>
    public string Code { get; }

    /// <summary>The day this count covers (UTC).</summary>
    public DateOnly Date { get; }

    /// <summary>
    /// The number of clicks recorded so far. Private-set so it only ever
    /// changes through <see cref="RecordOneClick"/>, never an arbitrary
    /// external assignment.
    /// </summary>
    public int Count { get; private set; }

    /// <summary>Creates the first recorded click for a code on a day.</summary>
    /// <param name="code">The short code.</param>
    /// <param name="date">The day the click occurred (UTC).</param>
    /// <returns>A new <see cref="DailyClickCount"/> with <see cref="Count"/> of 1.</returns>
    public static DailyClickCount FirstClick(string code, DateOnly date) => new(code, date, 1);

    /// <summary>Records one more click on this day.</summary>
    public void RecordOneClick() => Count++;
}
