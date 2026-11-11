using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SchoolAdmission.Infrastructure.Services;

/// <summary>Periodically runs <see cref="HoldExpirySweeper"/>. Interval: HOLD_EXPIRY_SWEEP_SECONDS (default 15).</summary>
public sealed class HoldExpiryService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<HoldExpiryService> _logger;
    private readonly TimeSpan _interval;

    public HoldExpiryService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<HoldExpiryService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        var seconds = int.TryParse(configuration["HOLD_EXPIRY_SWEEP_SECONDS"], out var parsed) && parsed > 0
            ? parsed
            : 15;
        _interval = TimeSpan.FromSeconds(seconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var sweeper = scope.ServiceProvider.GetRequiredService<HoldExpirySweeper>();
                var expired = await sweeper.SweepAsync(stoppingToken);

                if (expired > 0)
                    _logger.LogInformation("Marked {Count} expired hold(s) as EXPIRED", expired);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Never let the loop die: the next sweep may succeed (e.g. DB briefly unavailable).
                _logger.LogError(ex, "Hold expiry sweep failed");
            }

            try
            {
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
