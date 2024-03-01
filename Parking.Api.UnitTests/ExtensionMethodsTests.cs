// ReSharper disable StringLiteralTypo
// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local
namespace Parking.Api.UnitTests;

using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Model;
using Moq;
using TestHelpers;
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
    public static void OrderForDisplay_sorts_case_insensitively_by_last_name_then_first_name()
    {
        var users = new[]
        {
            CreateUser.With(userId: "User1", firstName: "nevin", lastName: "janecki"),
            CreateUser.With(userId: "User2", firstName: "Cynthy", lastName: "Janecki"),
            CreateUser.With(userId: "User3", firstName: "cynthy", lastName: "Janecki"),
            CreateUser.With(userId: "User4", firstName: "Haslett", lastName: "maggiori"),
            CreateUser.With(userId: "User5", firstName: "Cully", lastName: "Cisar"),
        };

        var result = users
            .OrderForDisplay()
            .ToArray();

        CheckUser("Cully", "Cisar", result[0]);
        CheckUser("Cynthy", "Janecki", result[1]);
        CheckUser("cynthy", "Janecki", result[2]);
        CheckUser("nevin", "janecki", result[3]);
        CheckUser("Haslett", "maggiori", result[4]);
    }

    [Fact]
    public static void DisplayName_concatenates_first_and_last_names()
    {
        var user = CreateUser.With(userId: "User1", firstName: "Abel", lastName: "Cromb");

        var result = user.DisplayName();

        Assert.Equal("Abel Cromb", result);
    }

    private static ControllerBase CreateController(ClaimsPrincipal user) =>
        Mock.Of<ControllerBase>(c =>
            c.ControllerContext == new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            });

    private static void CheckUser(string expectedFirstName, string expectedLastName, User actual)
    {
        Assert.Equal(expectedFirstName, actual.FirstName);
        Assert.Equal(expectedLastName, actual.LastName);
    }
}