namespace Parking.Api.Authentication
{
    using System;
    using System.IdentityModel.Tokens.Jwt;
    using System.Linq;
    using System.Security.Claims;
    using System.Text.Encodings.Web;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    public class DefaultAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public DefaultAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder) : 
            base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            const string BearerPrefix = "Bearer ";

            var authorizationHeaderValue = this.Request.Headers["Authorization"].FirstOrDefault();

            if (string.IsNullOrEmpty(authorizationHeaderValue) ||
                !authorizationHeaderValue.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(AuthenticateResult.Fail("No token was provided."));
            }

            var rawTokenValue = authorizationHeaderValue[BearerPrefix.Length..].Trim();

            try
            {
                var token = new JwtSecurityToken(rawTokenValue);

                var principal = new ClaimsPrincipal(new ClaimsIdentity(token.Claims, this.Scheme.Name));

                var ticket = new AuthenticationTicket(principal, this.Scheme.Name);

                return Task.FromResult(AuthenticateResult.Success(ticket));
            }
            catch (Exception exception)
            {
                return Task.FromResult(AuthenticateResult.Fail(exception));
            }
        }
    }
}