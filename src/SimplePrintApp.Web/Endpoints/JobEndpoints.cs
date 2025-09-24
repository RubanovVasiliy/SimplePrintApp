using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using SimplePrintApp.Application.Contracts;

namespace SimplePrintApp.Web.Endpoints;

public static class JobEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/state", (IPrintJobService svc) =>
        {
            var (st, rem) = svc.GetSnapshot();
            return Results.Json(new { state = st.ToString(), remainingSeconds = (int)Math.Ceiling(rem.TotalSeconds) });
        });

        app.MapPost("/start", (IPrintJobService svc) => { svc.Start(); return Results.Json(new { ok = true }); });
        app.MapPost("/pause", (IPrintJobService svc) => { svc.Pause(); return Results.Json(new { ok = true }); });
        app.MapPost("/resume", (IPrintJobService svc) => { svc.Resume(); return Results.Json(new { ok = true }); });
        app.MapPost("/stop", (IPrintJobService svc) => { svc.Stop(); return Results.Json(new { ok = true }); });
    }
}