namespace Parking.Data
{
    using System;

    public static class Helpers
    {
        public static string GetRequiredEnvironmentVariable(string variable)
        {
            var environmentVariable = Environment.GetEnvironmentVariable(variable);

            if (string.IsNullOrEmpty(environmentVariable))
            {
                throw new InvalidOperationException($"Required environment variable {variable} was missing.");
            }

            return environmentVariable;
        }
    }
}