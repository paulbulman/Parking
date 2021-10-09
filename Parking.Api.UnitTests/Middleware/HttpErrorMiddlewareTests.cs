namespace Parking.Api.UnitTests.Middleware
{
    using System.Threading.Tasks;
    using Api.Middleware;
    using Business.Data;
    using Microsoft.AspNetCore.Http;
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
    }
}