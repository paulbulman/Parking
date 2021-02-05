namespace Parking.Api.Middleware
{
    using System;
    using System.IdentityModel.Tokens.Jwt;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;

    // ReSharper disable once ClassNeverInstantiated.Global
    public class SetUserMiddleware
    {
        private readonly RequestDelegate next;

        public SetUserMiddleware(RequestDelegate next) => this.next = next;

        public async Task Invoke(HttpContext context)
        {
            SetUserFromAuthorizationHeader(context);

            await this.next(context);
        }

        private static void SetUserFromAuthorizationHeader(HttpContext context)
        {
            const string BearerPrefix = "Bearer ";

            var authorizationHeaderValue = context.Request.Headers["Authorization"].FirstOrDefault();

            if (string.IsNullOrEmpty(authorizationHeaderValue) ||
                !authorizationHeaderValue.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var rawTokenValue = authorizationHeaderValue[BearerPrefix.Length..].Trim();

            var token = new JwtSecurityToken(rawTokenValue);

            context.User = new ClaimsPrincipal(new ClaimsIdentity(token.Claims));
        }
    }
}