namespace Parking.Api
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.AspNetCore.Mvc;
    using Model;

    public static class ExtensionMethods
    {
        public static string GetCognitoUserId(this ControllerBase controller) => 
            controller.User.Claims.Single(c => c.Type == "cognito:username").Value;

        public static IOrderedEnumerable<User> OrderForDisplay(this IEnumerable<User> users) => users
            .OrderBy(u => u.LastName, StringComparer.InvariantCultureIgnoreCase)
            .ThenBy(u => u.FirstName, StringComparer.InvariantCultureIgnoreCase);

        public static string DisplayName(this User user) => $"{user.FirstName} {user.LastName}";
    }
}