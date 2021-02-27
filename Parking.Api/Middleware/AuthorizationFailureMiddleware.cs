namespace Parking.Api.Middleware
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Business.Data;
    using Microsoft.AspNetCore.Http;

    public class AuthorizationFailureMiddleware
    {
        private readonly RequestDelegate next;

        public AuthorizationFailureMiddleware(RequestDelegate next) => this.next = next;

        public async Task Invoke(HttpContext context, INotificationRepository notificationRepository)
        {
            await this.next(context);

            if (context.Response.StatusCode == 403)
            {
                const string Subject = "HTTP 403 error";

                var claimStrings = context.User.Claims.Select(c => $"{c.Type}: {c.Value}");
                
                var message =
                    $"An HTTP 403 error occurred during a {context.Request.Method} request to {context.Request.Path}.\r\n\r\n" +
                    "The user claims were as follows:\r\n" +
                    string.Join(Environment.NewLine, claimStrings);

                await notificationRepository.Send(Subject, message);
            }
        }
    }
}