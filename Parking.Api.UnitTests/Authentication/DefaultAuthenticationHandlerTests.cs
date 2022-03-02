namespace Parking.Api.UnitTests.Authentication
{
    using System.Linq;
    using System.Text.Encodings.Web;
    using System.Threading.Tasks;
    using Api.Authentication;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Moq;
    using TestHelpers;
    using Xunit;

    public static class DefaultAuthenticationHandlerTests
    {
        private const string RawTokenValue =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9." +
            "eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ." +
            "SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";

        [Fact]
        public static async Task Returns_success_when_token_is_set()
        {
            var context = CreateDefaultHttpContext.WithBearerToken(RawTokenValue);

            var handler = CreateHandler();

            await handler.InitializeAsync(new AuthenticationScheme("Default", null, typeof(DefaultAuthenticationHandler)), context);

            var result = await handler.AuthenticateAsync();

            Assert.True(result.Succeeded);
        }

        [Fact]
        public static async Task Sets_user_from_authorization_header()
        {
            var context = CreateDefaultHttpContext.WithBearerToken(RawTokenValue);

            var handler = CreateHandler();

            await handler.InitializeAsync(new AuthenticationScheme("Default", null, typeof(DefaultAuthenticationHandler)), context);

            var result = await handler.AuthenticateAsync();

            Assert.True(result.Succeeded);

            Assert.NotNull(result.Principal);

            Assert.Equal(3, result.Principal!.Claims.Count());

            Assert.Contains(result.Principal.Claims, c => c.Type == "sub" && c.Value == "1234567890");
            Assert.Contains(result.Principal.Claims, c => c.Type == "name" && c.Value == "John Doe");
            Assert.Contains(result.Principal.Claims, c => c.Type == "iat" && c.Value == "1516239022");
        }

        [Fact]
        public static async Task Returns_failure_when_token_is_not_set()
        {
            var context = CreateDefaultHttpContext.WithoutRequestHeaders();

            var handler = CreateHandler();

            await handler.InitializeAsync(new AuthenticationScheme("Default", null, typeof(DefaultAuthenticationHandler)), context);

            var result = await handler.AuthenticateAsync();

            Assert.False(result.Succeeded);
        }

        [Fact]
        public static async Task Returns_failure_when_token_is_invalid()
        {
            var context = CreateDefaultHttpContext.WithBearerToken("INVALID");

            var handler = CreateHandler();

            await handler.InitializeAsync(new AuthenticationScheme("Default", null, typeof(DefaultAuthenticationHandler)), context);

            var result = await handler.AuthenticateAsync();

            Assert.False(result.Succeeded);
        }

        private static DefaultAuthenticationHandler CreateHandler()
        {
            var options = Mock.Of<IOptionsMonitor<AuthenticationSchemeOptions>>(
                o => o.Get(It.IsAny<string>()) == new AuthenticationSchemeOptions());

            var loggerFactory = Mock.Of<ILoggerFactory>(
                f => f.CreateLogger(It.IsAny<string>()) == Mock.Of<ILogger<DefaultAuthenticationHandler>>());

            return new DefaultAuthenticationHandler(options, loggerFactory, UrlEncoder.Default, Mock.Of<ISystemClock>());
        }
    }
}