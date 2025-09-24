using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SimplePrintApp.Application.Contracts;

namespace SimplePrintApp.Infrastructure.Hosting;

public sealed class PrintJobWorker(IPrintJobService svc, ILogger<PrintJobWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        log.LogInformation("Worker started");
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                svc.Tick();
            }
        }
        catch (OperationCanceledException) { }
        log.LogInformation("Worker stopped");
    }
}