namespace Parking.Api.UnitTests.Middleware
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Api.Middleware;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Primitives;
    using Moq;
    using Xunit;

    public static class SetUserMiddlewareTests
    {
        private const string RawTokenValue =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9." +
            "eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ." +
            "SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";

        [Fact]
        public static async Task Sets_user_from_authorization_header()
        {
            var mockRequest = CreateMockRequest(RawTokenValue);

            var mockContext = new Mock<HttpContext>(MockBehavior.Strict);
            mockContext
                .SetupGet(c => c.Request)
                .Returns(mockRequest.Object);
            mockContext
                .SetupSet(c => c.User = It.IsAny<ClaimsPrincipal>());

            var middleware = new SetUserMiddleware(Mock.Of<RequestDelegate>());

            await middleware.Invoke(mockContext.Object);

            mockContext.VerifySet(
                c => c.User = It.Is<ClaimsPrincipal>(p =>
                    p.Claims.Count() == 3 &&
                    p.HasClaim("sub", "1234567890") &&
                    p.HasClaim("name", "John Doe") &&
                    p.HasClaim("iat", "1516239022")),
                Times.Once());
        }

        [Theory]
        [InlineData(RawTokenValue)]
        [InlineData(null)]
        public static async Task Calls_next_request_delegate(string rawTokenValue)
        {
            var mockRequest = CreateMockRequest(rawTokenValue);

            var context = Mock.Of<HttpContext>(c => c.Request == mockRequest.Object);

            var mockRequestDelegate = new Mock<RequestDelegate>();

            var middleware = new SetUserMiddleware(mockRequestDelegate.Object);

            await middleware.Invoke(context);

            mockRequestDelegate.Verify(d => d(context), Times.Once);
        }

        private static Mock<HttpRequest> CreateMockRequest(string rawTokenValue)
        {
            var headerValues = new Dictionary<string, StringValues>();

            if (!string.IsNullOrEmpty(rawTokenValue))
            {
                headerValues.Add("Authorization", new StringValues($"Bearer {rawTokenValue}"));
            }

            var mockRequest = new Mock<HttpRequest>(MockBehavior.Strict);
            mockRequest
                .Setup(r => r.Headers)
                .Returns(new HeaderDictionary(headerValues));

            return mockRequest;
        }
    }
}