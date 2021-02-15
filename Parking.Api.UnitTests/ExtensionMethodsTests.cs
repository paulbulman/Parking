namespace Parking.Api.UnitTests
{
    using System.Security.Claims;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Moq;
    using Xunit;

    public static class ExtensionMethodsTests
    {
        [Fact]
        public static void GetCognitoUserId_returns_value_of_matching_claim()
        {
            const string UserId = "09c0b0ac-6f80-4dd5-bce5-9c9b94d8ffd9";

            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("cognito:username", UserId)
            }));

            var controller = CreateController(user);

            var result = controller.GetCognitoUserId();

            Assert.Equal(UserId, result);
        }

        [Fact]
        public static void GetCognitoUserId_returns_null_when_no_matching_claim_exists()
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity());

            var controller = CreateController(user);

            var result = controller.GetCognitoUserId();

            Assert.Null(result);
        }

        private static Controller CreateController(ClaimsPrincipal user) =>
            Mock.Of<Controller>(c =>
                c.ControllerContext == new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = user }
                });
    }
}