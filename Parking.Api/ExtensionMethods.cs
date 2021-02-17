namespace Parking.Api
{
    using System.Linq;
    using Microsoft.AspNetCore.Mvc;

    public static class ExtensionMethods
    {
        public static string GetCognitoUserId(this ControllerBase controller) => 
            controller.User?.Claims.SingleOrDefault(c => c.Type == "cognito:username")?.Value;
    }
}