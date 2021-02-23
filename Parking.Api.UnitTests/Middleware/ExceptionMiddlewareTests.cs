namespace Parking.Api.UnitTests.Middleware
{
    using System;
    using System.Threading.Tasks;
    using Api.Middleware;
    using Business.Data;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Xunit;

    public static class ExceptionMiddlewareTests
    {
        [Fact]
        public static async Task Attempts_to_send_notification()
        {
            var mockRequestDelegate = new Mock<RequestDelegate>();

            mockRequestDelegate
                .Setup(d => d.Invoke(It.IsAny<HttpContext>()))
                .Throws(new Exception("Something went wrong"));

            var mockNotificationRepository = new Mock<INotificationRepository>();

            var middleware = new ExceptionMiddleware(mockRequestDelegate.Object, Mock.Of<ILogger<ExceptionMiddleware>>());

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

            var middleware = new ExceptionMiddleware(mockRequestDelegate.Object, Mock.Of<ILogger<ExceptionMiddleware>>());

            await Assert.ThrowsAsync<Exception>(async () =>
                await middleware.Invoke(Mock.Of<HttpContext>(), Mock.Of<INotificationRepository>()));
        }

        [Fact]
        public static async Task Throws_original_exception_if_sending_notification_fails()
        {
            var mockRequestDelegate = new Mock<RequestDelegate>();

            mockRequestDelegate
                .Setup(d => d.Invoke(It.IsAny<HttpContext>()))
                .Throws(new Exception("Something went wrong"));

            var mockNotificationRepository = new Mock<INotificationRepository>();

            mockNotificationRepository
                .Setup(r => r.Send(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new InvalidOperationException("Something else went wrong"));

            var middleware = new ExceptionMiddleware(mockRequestDelegate.Object, Mock.Of<ILogger<ExceptionMiddleware>>());

            await Assert.ThrowsAsync<Exception>(async () =>
                await middleware.Invoke(Mock.Of<HttpContext>(), Mock.Of<INotificationRepository>()));
        }
    }
}