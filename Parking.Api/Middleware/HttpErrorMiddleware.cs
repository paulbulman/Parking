namespace Parking.Api.Middleware
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Business.Data;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Primitives;

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
                var subject = $"HTTP {statusCode} error";

                var message =
                    new StringBuilder(
                        $"An HTTP {statusCode} error occurred during a {context.Request.Method} request to {context.Request.Path}.\r\n\r\n");

                if (context.Request.Headers.Any())
                {
                    message.AppendLine("Request headers:");
                    foreach (var header in context.Request.Headers)
                    {
                        message.AppendLine(FormatHeaderValues(header));
                    }

                    message.AppendLine();
                }
                else
                {
                    message.AppendLine("There were no request headers.\r\n");
                }

                message.AppendLine($"Remote IP address: {context.Connection.RemoteIpAddress}\r\n");

                if (context.User?.Claims?.Count() > 0)
                {
                    message.AppendLine("User claims:");
                    foreach (var userClaim in context.User.Claims)
                    {
                        message.AppendLine($"{userClaim.Type}: {userClaim.Value}");
                    }

                    message.AppendLine();
                }
                else
                {
                    message.AppendLine("There were no user claims.\r\n");
                }

                if (context.Request.Body.CanSeek)
                {
                    context.Request.Body.Seek(0, SeekOrigin.Begin);
                }

                var reader = new StreamReader(context.Request.Body);
                var body = await reader.ReadToEndAsync();
                
                if (body.Length > 0)
                {
                    message.AppendLine("Request body:");
                    message.AppendLine(body);
                }
                else
                {
                    message.AppendLine("There was no request body.");
                }

                await notificationRepository.Send(subject, message.ToString());
            }
        }

        private static string FormatHeaderValues(KeyValuePair<string, StringValues> header) =>
            header.Key == "Authorization"
                ? $"{header.Key}: {string.Join("; ", header.Value.Select(FormatAuthorizationHeaderValue))}"
                : $"{header.Key}: {string.Join("; ", header.Value)}";

        private static string FormatAuthorizationHeaderValue(string value) =>
            value?.Length > 23 ? $"{value.Substring(0, 10)} *** {value[^10..]} (length {value.Length})" : value;
    }
}