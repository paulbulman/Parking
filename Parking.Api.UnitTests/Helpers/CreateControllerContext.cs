namespace Parking.Api.UnitTests.Helpers;

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

public static class CreateControllerContext
{
    public static ControllerContext WithUsername(string username) =>
        new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(
                        new[] {new Claim("cognito:username", username)}))
            }
        };
}