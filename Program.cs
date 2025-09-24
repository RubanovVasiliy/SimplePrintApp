using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<PrintJobService>();
builder.Services.AddHostedService<PrintJobWorker>(); 

var app = builder.Build();

app.MapGet("/", async ctx =>
{
    ctx.Response.ContentType = "text/html; charset=utf-8";
    const string html = """
                        <!doctype html>
                        <html lang="ru">
                        <head>
                        <meta charset="utf-8"/>
                        <meta name="viewport" content="width=device-width, initial-scale=1"/>
                        <title>Print Job</title>
                        <style>
                          body { font-family: system-ui, -apple-system, Segoe UI, Roboto, Arial, sans-serif; margin: 40px; }
                          .row { display:flex; gap:12px; align-items:center; }
                          button { padding:10px 16px; border:1px solid #ccc; border-radius:8px; cursor:pointer; }
                          button:disabled { opacity:.5; cursor:not-allowed; }
                          .mono { font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace; }
                          .pill { display:inline-block; padding:2px 8px; border-radius:999px; border:1px solid #ddd; font-size:12px; }
                        </style>
                        </head>
                        <body>
                          <h1>Печать даты в консоль</h1>
                          <p>Каждую секунду, максимум 2 минуты чистого времени. Pause/Resume не тратит бюджет, Stop сбрасывает.</p>

                          <div class="row" style="margin:20px 0;">
                            <button id="primaryBtn">Start</button>
                            <button id="stopBtn">Stop</button>
                            <span id="state" class="pill">state: —</span>
                            <span id="remain" class="pill">remain: —</span>
                          </div>

                          <pre id="log" class="mono"></pre>

                          <script>
                            const stateEl = document.getElementById('state');
                            const remainEl = document.getElementById('remain');
                            const primaryBtn = document.getElementById('primaryBtn');
                            const stopBtn = document.getElementById('stopBtn');
                            const logEl = document.getElementById('log');

                            async function api(path, method='POST') {
                              const r = await fetch(path, { method });
                              if (!r.ok) throw new Error(await r.text());
                              return r.json();
                            }

                            function render(s) {
                              stateEl.textContent = 'state: ' + s.state;
                              remainEl.textContent = 'remain: ' + s.remainingSeconds + 's';

                              // Два состояния UI-кнопки: Start / Pause / Resume
                              if (s.state === 'Idle') {
                                primaryBtn.textContent = 'Start';
                                primaryBtn.disabled = false;
                                stopBtn.disabled = true;
                              } else if (s.state === 'Running') {
                                primaryBtn.textContent = 'Pause';
                                primaryBtn.disabled = false;
                                stopBtn.disabled = false;
                              } else if (s.state === 'Paused') {
                                primaryBtn.textContent = 'Resume';
                                primaryBtn.disabled = false;
                                stopBtn.disabled = false;
                              }
                            }

                            async function refresh() {
                              const s = await api('/state', 'GET');
                              render(s);
                            }

                            primaryBtn.addEventListener('click', async () => {
                              const s = await api('/state', 'GET');
                              if (s.state === 'Idle') {
                                await api('/start');
                                logEl.textContent += '▶ start\n';
                              } else if (s.state === 'Running') {
                                await api('/pause');
                                logEl.textContent += '⏸ pause\n';
                              } else if (s.state === 'Paused') {
                                await api('/resume');
                                logEl.textContent += '▶ resume\n';
                              }
                              await refresh();
                            });

                            stopBtn.addEventListener('click', async () => {
                              await api('/stop');
                              logEl.textContent += '⏹ stop\n';
                              await refresh();
                            });

                            setInterval(refresh, 500);
                            refresh();
                          </script>
                        </body>
                        </html>
                        """;
    await ctx.Response.WriteAsync(html, Encoding.UTF8);
});

app.MapGet("/state", (PrintJobService svc) =>
{
    var s = svc.GetSnapshot();
    return Results.Json(new { state = s.State.ToString(), remainingSeconds = (int)Math.Ceiling(s.Remaining.TotalSeconds) });
});

app.MapPost("/start", (PrintJobService svc) =>
{
    svc.Start();
    return Results.Json(new { ok = true });
});

app.MapPost("/pause", (PrintJobService svc) =>
{
    svc.Pause();
    return Results.Json(new { ok = true });
});

app.MapPost("/resume", (PrintJobService svc) =>
{
    svc.Resume();
    return Results.Json(new { ok = true });
});

app.MapPost("/stop", (PrintJobService svc) =>
{
    svc.Stop();
    return Results.Json(new { ok = true });
});

app.Run();

public enum JobState { Idle, Running, Paused }

public sealed class PrintJobService
{
    private readonly object _lock = new();
    private readonly ILogger<PrintJobService> _log;

    private readonly TimeSpan _total = TimeSpan.FromSeconds(30);
    private TimeSpan _remaining;
    private JobState _state = JobState.Idle;

    public PrintJobService(ILogger<PrintJobService> log)
    {
        _log = log;
        _remaining = _total;
    }

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
            _log.LogInformation("START: remaining={Remaining}", _remaining);
        }
    }

    public void Pause()
    {
        lock (_lock)
        {
            if (_state != JobState.Running) return;
            _state = JobState.Paused;
            _log.LogInformation("PAUSE: remaining={Remaining}", _remaining);
        }
    }

    public void Resume()
    {
        lock (_lock)
        {
            if (_state != JobState.Paused) return;
            _state = JobState.Running;
            _log.LogInformation("RESUME: remaining={Remaining}", _remaining);
        }
    }

    public void Stop()
    {
        lock (_lock)
        {
            _state = JobState.Idle;
            _remaining = _total;
            _log.LogInformation("STOP (reset to full)");
        }
    }

    internal void Tick()
    {
        lock (_lock)
        {
            if (_state != JobState.Running) return;
            
            var step = (_total - _remaining).TotalSeconds;
            Console.WriteLine($"{step:00} {DateTimeOffset.Now:O}");
            

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

public sealed class PrintJobWorker(PrintJobService svc, ILogger<PrintJobWorker> log) : BackgroundService
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
        catch (OperationCanceledException)
        {
            log.LogInformation("Worker stopping");
        }
    }
}
