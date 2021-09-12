namespace Parking.Api
{
    using System;
    using Amazon.Lambda.AspNetCoreServer;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Hosting;
    using Serilog;
    using Serilog.Events;

    public class LambdaEntryPoint : APIGatewayProxyFunction
    {
        public LambdaEntryPoint()
        {
            Console.WriteLine("Configuring Serilog...");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();
        }

        protected override void Init(IHostBuilder builder) => builder.UseSerilog();

        protected override void Init(IWebHostBuilder builder) => builder.UseStartup<Startup>();
    }
}
