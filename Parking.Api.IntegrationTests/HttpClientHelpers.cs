namespace Parking.Api.IntegrationTests
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;

    public static class HttpClientHelpers
    {
        public enum UserType
        {
            Normal,
            TeamLeader,
            UserAdmin
        }

        private const string NormalUserToken =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9." +
            "eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyLCJjb2duaXRvOnVzZXJuYW1lIjoiVXNlcjEifQ." +
            "zB45sGOz6fmJHpJBVtRZQDa1rZHMi-MpjAcOCvjkC5E";

        private const string TeamLeaderUserToken =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9." +
            "eyJzdWIiOiIxMjM0NTY3ODkwIiwiY29nbml0bzpncm91cHMiOlsiVGVhbUxlYWRlciJdLCJuYW1lIjoiSm9obiBEb2UiLCJpYXQiOjE1MTYyMzkwMjIsImNvZ25pdG86dXNlcm5hbWUiOiJVc2VyMSJ9." +
            "VL5oFUXkXfpoJlZozpiNMXFAYAttq2iCYFaCqryOdzU";

        private const string UserAdminUserToken =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9." +
            "eyJzdWIiOiIxMjM0NTY3ODkwIiwiY29nbml0bzpncm91cHMiOlsiVXNlckFkbWluIl0sIm5hbWUiOiJKb2huIERvZSIsImlhdCI6MTUxNjIzOTAyMiwiY29nbml0bzp1c2VybmFtZSI6IlVzZXIxIn0." +
            "pTwF5BPkgUHfEhkhybB7GuCh_4aTfa3jeF0VbLq7gMM";

        public static void AddAuthorizationHeader(HttpClient client, UserType userType)
        {
            var rawTokenValue = userType switch
            {
                UserType.Normal => NormalUserToken,
                UserType.TeamLeader => TeamLeaderUserToken,
                UserType.UserAdmin => UserAdminUserToken,
                _ => throw new ArgumentOutOfRangeException(nameof(userType))
            };

            client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {rawTokenValue}");
        }
    }
}