// Services/SecurityRetentionWorker.cs
using Microsoft.EntityFrameworkCore;
using PickleballBookingSystem.Data;

namespace PickleballBookingSystem.Services;

/// <summary>
/// Runs once a day and clears the raw IpAddress from security events older
/// than RetentionDays. The IpHash is kept — it's the abuse-correlation value.
/// Also deletes events older than TotalRetentionDays entirely.
/// </summary>
public class SecurityRetentionWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SecurityRetentionWorker> _logger;

    // Raw IP retention (30 days) — the hash stays forever.
    private const int RawIpRetentionDays = 30;

    // Total event retention (365 days) — older rows are deleted entirely.
    private const int TotalRetentionDays = 365;

    // How often to run. Once a day is plenty for a low-volume audit table.
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    public SecurityRetentionWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<SecurityRetentionWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait 5 min after startup so we don't compete with the app's first request
        try { await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var now = DateTime.UtcNow;
                var ipCutoff = now.AddDays(-RawIpRetentionDays);
                var totalCutoff = now.AddDays(-TotalRetentionDays);

                // 1. Blank out raw IPs older than 30 days
                var ipCleared = await db.Database.ExecuteSqlRawAsync(
                    @"UPDATE securityevents
                      SET ""IpAddress"" = NULL
                      WHERE ""IpAddress"" IS NOT NULL
                        AND ""CreatedAt"" < {0}",
                    new object[] { ipCutoff },
                    stoppingToken);

                // 2. Delete events older than 365 days
                var deleted = await db.Database.ExecuteSqlRawAsync(
                    @"DELETE FROM securityevents
                      WHERE ""CreatedAt"" < {0}",
                    new object[] { totalCutoff },
                    stoppingToken);

                if (ipCleared > 0 || deleted > 0)
                {
                    _logger.LogInformation(
                        "SecurityRetentionWorker: cleared {IpCount} IPs, deleted {DelCount} old events",
                        ipCleared, deleted);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SecurityRetentionWorker run failed");
            }

            try { await Task.Delay(Interval, stoppingToken); }
            catch (OperationCanceledException) { return; }
        }
    }
}