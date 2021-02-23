namespace Parking.Api.Middleware
{
    using System;
    using System.Threading.Tasks;
    using Business.Data;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;

    public class ExceptionMiddleware
    {
        private readonly RequestDelegate next;
        
        private readonly ILogger<ExceptionMiddleware> logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            this.next = next;
            this.logger = logger;
        }

        public async Task Invoke(HttpContext context, INotificationRepository notificationRepository)
        {
            try
            {
                await this.next(context);
            }
            catch (Exception initialException)
            {
                try
                {
                    await notificationRepository.Send("Unhandled exception", initialException.ToString());
                }
                catch (Exception notificationException)
                {
                    this.logger.LogError(notificationException, "Exception occurred attempting to send exception notification.");
                }

                throw;
            }
        }
    }
}