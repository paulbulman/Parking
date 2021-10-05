namespace Parking.Api
{
    using Amazon.Lambda.AspNetCoreServer;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Hosting;
    using NodaTime;
    using NodaTime.Text;
    using Serilog;
    using Serilog.Events;
    using Serilog.Formatting.Compact;

    public class LambdaEntryPoint : APIGatewayProxyFunction
    {
        public LambdaEntryPoint() =>
            Log.Logger = new LoggerConfiguration()
                .Destructure.ByTransforming<LocalDate>(d =>
                    LocalDatePattern.CreateWithCurrentCulture("yyyy-MM-dd").Format(d))
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console(new CompactJsonFormatter())
                .CreateLogger();

        protected override void Init(IHostBuilder builder) => builder.UseSerilog();

        protected override void Init(IWebHostBuilder builder) => builder.UseStartup<Startup>();
    }
}
