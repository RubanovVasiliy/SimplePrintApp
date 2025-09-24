using SimplePrintApp.Domain;

namespace SimplePrintApp.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.UtcNow;
    public Task Delay(TimeSpan delay, CancellationToken ct) => Task.Delay(delay, ct);
}