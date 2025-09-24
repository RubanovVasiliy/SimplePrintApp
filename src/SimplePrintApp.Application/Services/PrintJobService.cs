using SimplePrintApp.Application.Contracts;
using SimplePrintApp.Domain;

namespace SimplePrintApp.Application.Services;

public sealed class PrintJobService : IPrintJobService
{
    private readonly object _lock = new();
    private readonly IClock _clock;
    private readonly TimeSpan _total = TimeSpan.FromSeconds(30);
    private TimeSpan _remaining;
    private JobState _state = JobState.Idle;

    public PrintJobService(IClock clock)
    {
        _clock = clock;
        _remaining = _total;
    }

    public JobState State { get { lock (_lock) return _state; } }
    public TimeSpan Total  => _total;
    public TimeSpan Remaining { get { lock (_lock) return _remaining; } }

    public (JobState State, TimeSpan Remaining) GetSnapshot()
    {
        lock (_lock) return (_state, _remaining);
    }

    public void Start()
    {
        lock (_lock)
        {
            if (_state != JobState.Idle) return;
            _remaining = _total;
            _state = JobState.Running;
        }
    }

    public void Pause()
    {
        lock (_lock)
        {
            if (_state != JobState.Running) return;
            _state = JobState.Paused;
        }
    }

    public void Resume()
    {
        lock (_lock)
        {
            if (_state != JobState.Paused) return;
            _state = JobState.Running;
        }
    }

    public void Stop()
    {
        lock (_lock)
        {
            _state = JobState.Idle;
            _remaining = _total;
        }
    }

    public void Tick()
    {
        lock (_lock)
        {
            if (_state != JobState.Running) return;

            Console.WriteLine(_clock.Now.ToString("O"));

            if (_remaining > TimeSpan.Zero)
            {
                _remaining -= TimeSpan.FromSeconds(1);
                if (_remaining <= TimeSpan.Zero)
                {
                    _state = JobState.Idle;
                    _remaining = TimeSpan.Zero;
                    Console.WriteLine("=== DONE ===");
                }
            }
        }
    }
}
