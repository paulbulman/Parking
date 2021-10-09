namespace Parking.Api.Middleware
{
    using System.Threading.Tasks;
    using Business.Data;
    using Microsoft.AspNetCore.Http;

    public class HttpErrorMiddleware
    {
        private readonly RequestDelegate next;

        public HttpErrorMiddleware(RequestDelegate next) => this.next = next;

        public async Task Invoke(HttpContext context, INotificationRepository notificationRepository)
        {
            await this.next(context);

            var statusCode = context.Response.StatusCode;

            if (statusCode >= 400)
            {
                await notificationRepository.Send(
                    $"HTTP {statusCode} error",
                    $"An HTTP {statusCode} error occurred during a {context.Request.Method} request to {context.Request.Path}.");
            }
        }
    }
}