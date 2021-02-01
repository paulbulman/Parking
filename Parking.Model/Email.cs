namespace Parking.Model
{
    public class Email
    {
        public Email(string to, string subject, string plainTextBody, string htmlBody)
        {
            To = to;
            Subject = subject;
            PlainTextBody = plainTextBody;
            HtmlBody = htmlBody;
        }

        // Class used with JsonSerializer.Serialize
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public string To { get; }

        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public string Subject { get; }

        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public string PlainTextBody { get; }

        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public string HtmlBody { get; }

    }
}