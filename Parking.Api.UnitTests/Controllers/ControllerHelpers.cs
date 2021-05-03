namespace Parking.Api.UnitTests.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Xunit;

    public static class ControllerHelpers
    {
        public static T GetResultValue<T>(IActionResult actionResult) where T : class
        {
            var okObjectResult = actionResult as OkObjectResult;

            Assert.NotNull(okObjectResult);

            var value = okObjectResult!.Value as T;

            Assert.NotNull(value);

            return value!;
        }
    }
}