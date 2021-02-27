namespace Parking.Api.UnitTests.Middleware
{
    using System.IO;
    using System.Net;
    using System.Security.Claims;
    using System.Text;
    using System.Threading.Tasks;
    using Api.Middleware;
    using Business.Data;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Primitives;
    using Moq;
    using Xunit;

    public static class HttpErrorMiddlewareTests
    {
        [Fact]
        public static async Task Sends_notification_on_HTTP_error()
        {
            var mockNotificationRepository = new Mock<INotificationRepository>();

            var middleware = new HttpErrorMiddleware(Mock.Of<RequestDelegate>());

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
        public static async Task Includes_request_headers()
        {
            var mockNotificationRepository = new Mock<INotificationRepository>();

            var middleware = new HttpErrorMiddleware(Mock.Of<RequestDelegate>());

            var context = new DefaultHttpContext
            {
                Request = { Method = "GET", Path = "/overview" },
                Response = {StatusCode = 400},
            };
            context.Request.Headers.Add("key1", new StringValues(new[] {"value1", "value2"}));
            context.Request.Headers.Add("key2", new StringValues("value3"));

            await middleware.Invoke(context, mockNotificationRepository.Object);

            mockNotificationRepository.Verify(
                r => r.Send(
                    It.IsAny<string>(),
                    It.Is<string>(v => v.Contains("key1: value1; value2") && v.Contains("key2: value3"))),
                Times.Once);
        }

        [Fact]
        public static async Task Includes_remote_IP_address()
        {
            var mockNotificationRepository = new Mock<INotificationRepository>();

            var middleware = new HttpErrorMiddleware(Mock.Of<RequestDelegate>());

            var context = new DefaultHttpContext
            {
                Connection = { RemoteIpAddress = new IPAddress(0x2414188f) },
                Request = { Method = "GET", Path = "/overview" },
                Response = { StatusCode = 400 },
            };

            await middleware.Invoke(context, mockNotificationRepository.Object);

            mockNotificationRepository.Verify(
                r => r.Send(It.IsAny<string>(), It.Is<string>(v => v.Contains("143.24.20.36"))),
                Times.Once);
        }

        [Fact]
        public static async Task Conceals_authorization_header()
        {
            const string RawJwtValue =
                "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9." +
                "eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ." +
                "SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";

            var mockNotificationRepository = new Mock<INotificationRepository>();

            var middleware = new HttpErrorMiddleware(Mock.Of<RequestDelegate>());

            var context = new DefaultHttpContext
            {
                Request = { Method = "GET", Path = "/overview" },
                Response = { StatusCode = 400 },
            };
            context.Request.Headers.Add("Authorization", new StringValues($"Bearer {RawJwtValue}"));

            await middleware.Invoke(context, mockNotificationRepository.Object);

            mockNotificationRepository.Verify(
                r => r.Send(It.IsAny<string>(),
                    It.Is<string>(v => v.Contains("Authorization: Bearer eyJ *** V_adQssw5c"))),
                Times.Once);
        }

        [Fact]
        public static async Task Includes_user_claims()
        {
            var mockNotificationRepository = new Mock<INotificationRepository>();

            var middleware = new HttpErrorMiddleware(Mock.Of<RequestDelegate>());

            var context = new DefaultHttpContext
            {
                Request = { Method = "GET", Path = "/requests/123" },
                Response = { StatusCode = 403 },
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(new[]
                    {
                        new Claim("Type1", "Value1"),
                        new Claim("Type2", "Value2")
                    }))
            };

            await middleware.Invoke(context, mockNotificationRepository.Object);

            mockNotificationRepository.Verify(
                r => r.Send(
                    It.IsAny<string>(),
                    It.Is<string>(v => v.Contains("Type1: Value1") && v.Contains("Type2: Value2"))),
                Times.Once);
        }
        
        [Fact]
        public static async Task Includes_request_body()
        {
            var mockNotificationRepository = new Mock<INotificationRepository>();

            var middleware = new HttpErrorMiddleware(Mock.Of<RequestDelegate>());

            var context = new DefaultHttpContext
            {
                Request =
                {
                    Method = "PATCH",
                    Path = "/requests",
                    Body = new MemoryStream(Encoding.UTF8.GetBytes("__BODY_CONTENT__"))
                },
                Response = {StatusCode = 400}
            };

            await middleware.Invoke(context, mockNotificationRepository.Object);

            mockNotificationRepository.Verify(
                r => r.Send(It.IsAny<string>(), It.Is<string>(v => v.Contains("__BODY_CONTENT__"))),
                Times.Once);
        }
    }
}