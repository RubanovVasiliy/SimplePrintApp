namespace SimplePrintApp.Domain;

public interface IClock
{
    DateTimeOffset Now { get; }
    Task Delay(TimeSpan delay, CancellationToken ct);
}