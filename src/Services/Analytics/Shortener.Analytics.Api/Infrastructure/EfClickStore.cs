using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shortener.Analytics.Api.Application;
using Shortener.Analytics.Api.Domain;
using Shortener.Contracts;

namespace Shortener.Analytics.Api.Infrastructure;

/// <summary>EF Core / Postgres implementation of <see cref="IClickRecorder"/> and <see cref="IClickStatsQuery"/>.</summary>
public sealed class EfClickStore(AnalyticsDbContext dbContext) : IClickRecorder, IClickStatsQuery
{
    /// <inheritdoc />
    public async Task<bool> TryRecordAsync(LinkClicked clickEvent, CancellationToken cancellationToken)
    {
        var date = DateOnly.FromDateTime(clickEvent.OccurredAt.UtcDateTime);

        dbContext.Add(new ProcessedEvent(clickEvent.EventId, clickEvent.OccurredAt));

        var dailyCount = await dbContext.DailyClickCounts.SingleOrDefaultAsync(
            count => count.Code == clickEvent.Code && count.Date == date,
            cancellationToken);

        if (dailyCount is null)
        {
            dbContext.Add(DailyClickCount.FirstClick(clickEvent.Code, date));
        }
        else
        {
            dailyCount.RecordOneClick();
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // EventId already processed: the whole SaveChanges call rolled
            // back, so the DailyClickCount change queued above never
            // committed either. Redelivery is a clean no-op.
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<ClickStats> GetStatsAsync(string code, CancellationToken cancellationToken)
    {
        var rows = await dbContext.DailyClickCounts
            .Where(count => count.Code == code)
            .OrderBy(count => count.Date)
            .ToListAsync(cancellationToken);

        var daily = rows.Select(row => new DailyClicks(row.Date, row.Count)).ToList();
        var total = rows.Sum(row => row.Count);

        return new ClickStats(code, total, daily);
    }
}
