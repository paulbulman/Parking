namespace Parking.Service
{
    using System;
    using System.Threading.Tasks;
    using Amazon.Lambda.Core;
    using Microsoft.Extensions.DependencyInjection;

    public class LambdaEntryPoint
    {
        private readonly IServiceProvider serviceProvider;

        public LambdaEntryPoint() => this.serviceProvider = new Startup().BuildServiceProvider();

        public async Task RunTasks(ILambdaContext context)
        {
            using var scope = this.serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();

            await TaskRunner.RunTasksAsync(scope.ServiceProvider);
        }
    }
}
