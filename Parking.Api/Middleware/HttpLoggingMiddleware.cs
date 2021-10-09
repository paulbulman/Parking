namespace Parking.Api.Middleware
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Primitives;
    using Serilog;

    public class HttpLoggingMiddleware
    {
        private readonly RequestDelegate next;
        private readonly IDiagnosticContext diagnosticContext;

        public HttpLoggingMiddleware(RequestDelegate next, IDiagnosticContext diagnosticContext)
        {
            this.next = next;
            this.diagnosticContext = diagnosticContext;
        }

        public async Task Invoke(HttpContext context)
        {
            await this.next(context);

            var request = context.Request;

            this.diagnosticContext.Set("RequestHeaders", request.Headers.Select(FormatHeaderValues));
            this.diagnosticContext.Set("RemoteIpAddress", context.Connection.RemoteIpAddress);
            this.diagnosticContext.Set("UserClaims", context.User?.Claims?.Select(c => KeyValuePair.Create(c.Type, c.Value)));
            
            if (request.QueryString.HasValue)
            {
                this.diagnosticContext.Set("QueryString", request.QueryString.Value);
            }

            if (request.Body.CanSeek)
            {
                request.Body.Seek(0, SeekOrigin.Begin);
            }

            var bodyReader = new StreamReader(request.Body);
            var body = await bodyReader.ReadToEndAsync();

            if (body.Length > 0)
            {
                this.diagnosticContext.Set("RequestBody", body);
            }

            this.diagnosticContext.Set("StatusCode", context.Response.StatusCode);
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
}