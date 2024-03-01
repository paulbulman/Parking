namespace Parking.Api.Middleware;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Parking.Business.Data;
using Serilog;

public class HttpLoggingMiddleware(
    ILogger<HttpLoggingMiddleware> logger,
    RequestDelegate next,
    IDiagnosticContext diagnosticContext)
{
    public async Task Invoke(
        HttpContext context,
        INotificationRepository notificationRepository)
    {
        try
        {
            await next(context);
        }
        catch (Exception initialException)
        {
            await this.SendNotification(
                notificationRepository,
                subject: "Unhandled exception",
                body: initialException.ToString());

            throw;
        }
        finally
        {
            diagnosticContext.Set("RemoteIpAddress", context.Connection.RemoteIpAddress);
            diagnosticContext.Set("UserClaims", context.User?.Claims?.Select(c => KeyValuePair.Create(c.Type, c.Value)));

            var request = context.Request;

            diagnosticContext.Set("RequestHeaders", request.Headers.Select(FormatHeaderValues).ToArray());

            if (request.Body.CanSeek)
            {
                request.Body.Seek(0, SeekOrigin.Begin);
            }

            var bodyReader = new StreamReader(request.Body);
            var body = await bodyReader.ReadToEndAsync();

            if (body.Length > 0)
            {
                diagnosticContext.Set("RequestBody", body);
            }

            var statusCode = context.Response.StatusCode;

            diagnosticContext.Set("StatusCode", statusCode);

            if (statusCode >= 400)
            {
                var notificationSubject = $"HTTP {statusCode} error";
                var notificationBody =
                    $"An HTTP {statusCode} error occurred during a {context.Request.Method} request to {context.Request.Path}.";

                await this.SendNotification(
                    notificationRepository,
                    subject: notificationSubject,
                    body: notificationBody);
            }
        }
    }

    private async Task SendNotification(
        INotificationRepository notificationRepository,
        string subject,
        string body)
    {
        try
        {
            await notificationRepository.Send(subject, body);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Exception occurred attempting to send exception notification.");
        }
    }

    private static KeyValuePair<string, string> FormatHeaderValues(KeyValuePair<string, StringValues> header) =>
        KeyValuePair.Create(
            header.Key,
            string.Join("; ", header.Value.Select(v => FormatHeaderValue(header.Key, v))));

    private static string? FormatHeaderValue(string key, string? value) =>
        string.Equals(key, "Authorization", StringComparison.OrdinalIgnoreCase)
            ? FormatAuthorizationHeaderValue(value)
            : value;

    private static string? FormatAuthorizationHeaderValue(string? value) =>
        value?.Length > 23 ? $"{value[..10]} *** {value[^10..]} (length {value.Length})" : value;
}