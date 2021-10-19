namespace Parking.Api.UnitTests.Middleware
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Security.Claims;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Primitives;
    using Moq;
    using Parking.Api.Middleware;
    using Parking.Business.Data;
    using Serilog;
    using Xunit;

    public static class HttpLoggingMiddlewareTests
    {
        [Fact]
        public static async Task Logs_incoming_request_data()
        {
            var mockDiagnosticContext = new Mock<IDiagnosticContext>();

            var middleware = new HttpLoggingMiddleware(
                Mock.Of<ILogger<HttpLoggingMiddleware>>(),
                Mock.Of<RequestDelegate>(),
                mockDiagnosticContext.Object);

            var context = new DefaultHttpContext
            {
                Connection = { RemoteIpAddress = new IPAddress(0x2414188f) },
                Request =
                {
                    Method = "PATCH",
                    Path = "/requests?userId=user1",
                    Body = new MemoryStream(Encoding.UTF8.GetBytes("__BODY_CONTENT__"))
                },
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(new[]
                    {
                        new Claim("type1", "value1"),
                        new Claim("type2", "value2")
                    })),
                Response = { StatusCode = 200 },
            };

            context.Request.Headers.Add("key1", new StringValues(new[] { "value1a", "value1b" }));
            context.Request.Headers.Add("key2", new StringValues("value2"));

            await middleware.Invoke(context, Mock.Of<INotificationRepository>());

            var expectedHeaders = new Dictionary<string, string>
            {
                { "key1", "value1a; value1b" },
                { "key2", "value2" }
            };

            var expectedClaims = new Dictionary<string, string>
            {
                { "type1", "value1" },
                { "type2", "value2" }
            };

            mockDiagnosticContext.Verify(c => c.Set("RemoteIpAddress", new IPAddress(0x2414188f), false));
            mockDiagnosticContext.Verify(
                c => c.Set(
                    "UserClaims",
                    It.Is<IEnumerable<KeyValuePair<string, string>>>(
                        actual => CheckDictionary(new Dictionary<string, string>(actual), expectedClaims)),
                    false));
            mockDiagnosticContext.Verify(
                c => c.Set(
                    "RequestHeaders",
                    It.Is<IEnumerable<KeyValuePair<string, string>>>(
                        actual => CheckDictionary(new Dictionary<string, string>(actual), expectedHeaders)),
                    false));
            mockDiagnosticContext.Verify(c => c.Set("RequestBody", "__BODY_CONTENT__", false));
            mockDiagnosticContext.Verify(c => c.Set("StatusCode", 200, false));
        }

        [Fact]
        public static async Task Logs_incoming_request_data_when_exception_is_thrown()
        {
            var mockDiagnosticContext = new Mock<IDiagnosticContext>();

            var mockRequestDelegate = new Mock<RequestDelegate>();

            mockRequestDelegate
                .Setup(d => d.Invoke(It.IsAny<HttpContext>()))
                .Throws(new Exception("Something went wrong"));

            var context = new DefaultHttpContext
            {
                Request = { Body = new MemoryStream(Encoding.UTF8.GetBytes("__BODY_CONTENT__")) }
            };

            var middleware = new HttpLoggingMiddleware(
                Mock.Of<ILogger<HttpLoggingMiddleware>>(),
                mockRequestDelegate.Object,
                mockDiagnosticContext.Object);

            try
            {
                await middleware.Invoke(context, Mock.Of<INotificationRepository>());
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch
            {
            }

            mockDiagnosticContext.Verify(c => c.Set("RequestBody", "__BODY_CONTENT__", false));
        }

        [Fact]
        public static async Task Sends_notification_on_HTTP_error()
        {
            var mockNotificationRepository = new Mock<INotificationRepository>();

            var middleware = new HttpLoggingMiddleware(
                Mock.Of<ILogger<HttpLoggingMiddleware>>(),
                Mock.Of<RequestDelegate>(),
                Mock.Of<IDiagnosticContext>());

            var context = new DefaultHttpContext
            {
                Request = { Method = "GET", Path = "/overview" },
                Response = { StatusCode = 400 },
            };

            await middleware.Invoke(context, mockNotificationRepository.Object);

            mockNotificationRepository.Verify(
                r => r.Send("HTTP 400 error", It.Is<string>(v => v.Contains("GET") && v.Contains("/overview"))),
                Times.Once);
        }

        [Fact]
        public static async Task Sends_notification_when_exception_is_thrown()
        {
            var mockRequestDelegate = new Mock<RequestDelegate>();

            mockRequestDelegate
                .Setup(d => d.Invoke(It.IsAny<HttpContext>()))
                .Throws(new Exception("Something went wrong"));

            var mockNotificationRepository = new Mock<INotificationRepository>();

            var middleware = new HttpLoggingMiddleware(
                Mock.Of<ILogger<HttpLoggingMiddleware>>(),
                mockRequestDelegate.Object,
                Mock.Of<IDiagnosticContext>());

            try
            {
                await middleware.Invoke(Mock.Of<HttpContext>(), mockNotificationRepository.Object);
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch
            {
            }

            mockNotificationRepository.Verify(
                r => r.Send(
                    "Unhandled exception",
                    It.Is<string>(s => s.StartsWith("System.Exception: Something went wrong"))),
                Times.Once);
        }

        [Fact]
        public static async Task Throws_original_exception()
        {
            var mockRequestDelegate = new Mock<RequestDelegate>();

            mockRequestDelegate
                .Setup(d => d.Invoke(It.IsAny<HttpContext>()))
                .Throws(new Exception("Something went wrong"));

            var middleware = new HttpLoggingMiddleware(
                Mock.Of<ILogger<HttpLoggingMiddleware>>(),
                mockRequestDelegate.Object,
                Mock.Of<IDiagnosticContext>());

            await Assert.ThrowsAsync<Exception>(async () =>
                await middleware.Invoke(new DefaultHttpContext(), Mock.Of<INotificationRepository>()));
        }

        [Fact]
        public static async Task Throws_original_exception_if_sending_exception_notification_fails()
        {
            var mockRequestDelegate = new Mock<RequestDelegate>();

            mockRequestDelegate
                .Setup(d => d.Invoke(It.IsAny<HttpContext>()))
                .Throws(new Exception("Something went wrong"));

            var mockNotificationRepository = new Mock<INotificationRepository>();

            mockNotificationRepository
                .Setup(r => r.Send(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new InvalidOperationException("Something else went wrong"));

            var middleware = new HttpLoggingMiddleware(
                Mock.Of<ILogger<HttpLoggingMiddleware>>(),
                mockRequestDelegate.Object,
                Mock.Of<IDiagnosticContext>());

            await Assert.ThrowsAsync<Exception>(async () =>
                await middleware.Invoke(new DefaultHttpContext(), Mock.Of<INotificationRepository>()));
        }

        private static bool CheckDictionary(
            IDictionary<string, string> actual,
            IDictionary<string, string> expected) =>
            actual.Count == expected.Count &&
            expected.Keys.All(k => actual.ContainsKey(k) && actual[k] == expected[k]);
    }
}