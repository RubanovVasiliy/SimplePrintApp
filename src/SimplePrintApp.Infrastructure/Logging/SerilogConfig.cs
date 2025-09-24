using Microsoft.AspNetCore.Builder;
using Serilog;

namespace SimplePrintApp.Infrastructure.Logging;

public static class SerilogConfig
{
    public static void Configure(WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((ctx, cfg) =>
        {
            cfg.ReadFrom.Configuration(ctx.Configuration)
                .Enrich.FromLogContext()
                .WriteTo.Console();
        });
    }
}