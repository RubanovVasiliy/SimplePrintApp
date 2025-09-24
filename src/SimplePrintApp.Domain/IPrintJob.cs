namespace SimplePrintApp.Domain;

public interface IPrintJob
{
    JobState State { get; }
    TimeSpan Total { get; }
    TimeSpan Remaining { get; }
}