namespace Parking.Api;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using NodaTime;
using NodaTime.Text;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

public static class LocalEntryPoint
{
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .Destructure.ByTransforming<LocalDate>(d =>
                LocalDatePattern.CreateWithCurrentCulture("yyyy-MM-dd").Format(d))
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Parking.Api.Authentication.DefaultAuthenticationHandler", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(new CompactJsonFormatter())
            .CreateLogger();

        CreateHostBuilder(args).Build().Run();
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
}