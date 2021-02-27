namespace Parking.Api.UnitTests.Middleware
{
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Api.Middleware;
    using Business.Data;
    using Microsoft.AspNetCore.Http;
    using Moq;
    using Xunit;

    public static class AuthorizationFailureMiddlewareTests
    {
        [Fact]
        public static async Task Sends_notification_on_authorization_failure()
        {
            var mockNotificationRepository = new Mock<INotificationRepository>();

            var middleware = new AuthorizationFailureMiddleware(Mock.Of<RequestDelegate>());

            var context = new DefaultHttpContext
            {
                Request = {Method = "GET", Path = "/requests/123"},
                Response = {StatusCode = 403},
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
                    "HTTP 403 error",
                    It.Is<string>(v =>
                        v.Contains("Type1: Value1") &&
                        v.Contains("Type2: Value2"))),
                Times.Once);

            mockNotificationRepository.VerifyNoOtherCalls();
        }
    }
}