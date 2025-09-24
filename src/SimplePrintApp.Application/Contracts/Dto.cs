namespace SimplePrintApp.Application.Contracts;

public sealed record JobSnapshotDto(string State, int RemainingSeconds);