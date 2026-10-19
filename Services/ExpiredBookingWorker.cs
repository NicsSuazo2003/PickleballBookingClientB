using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PickleballBookingSystem.Data;
using PickleballBookingSystem.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace PickleballBookingSystem.Services;

public class ExpiredBookingWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExpiredBookingWorker> _logger;

    public ExpiredBookingWorker(IServiceProvider serviceProvider, ILogger<ExpiredBookingWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

                var clientIds = await db.Bookings
                    .Where(b => b.Status == "pending_payment"
                             && b.PaymentExpiresAt != null
                             && b.PaymentExpiresAt < DateTime.UtcNow)
                    .Select(b => b.ClientId)
                    .Distinct()
                    .ToListAsync(stoppingToken);

                foreach (var clientId in clientIds)
                {
                    await bookingService.CancelExpiredPaymentsAsync(clientId);
                }

                if (clientIds.Count > 0)
                {
                    _logger.LogInformation($"Auto-expired pending bookings for {clientIds.Count} clients.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while auto-expiring bookings.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}