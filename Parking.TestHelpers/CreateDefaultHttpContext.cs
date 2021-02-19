﻿namespace Parking.TestHelpers
{
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Primitives;

    public class CreateDefaultHttpContext
    {
        public static DefaultHttpContext WithBearerToken(string rawTokenValue) =>
            new DefaultHttpContext
            {
                Request =
                {
                    Headers =
                    {
                        KeyValuePair.Create("Authorization", new StringValues($"Bearer {rawTokenValue}"))
                    }
                }
            };

        public static DefaultHttpContext WithoutRequestHeaders() => new DefaultHttpContext();
    }
}