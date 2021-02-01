namespace Parking.Business.EmailTemplates
{
    using System.Collections.Generic;
    using System.Linq;
    using Model;
    using NodaTime;

    public class RequestReminder : IEmailTemplate
    {
        private readonly User user;
        
        private readonly IReadOnlyCollection<LocalDate> reminderDates;

        public RequestReminder(User user, IReadOnlyCollection<LocalDate> reminderDates)
        {
            this.user = user;
            this.reminderDates = reminderDates;
        }

        public string To => this.user.EmailAddress;

        public string Subject => $"No parking requests entered for {this.reminderDates.ToEmailDisplayString()}";
        
        public string PlainTextBody => string.Join("\r\n\r\n", this.BodyLines);

        public string HtmlBody => string.Join("\r\n", this.BodyLines.Select(l => $"<p>{l}</p>"));

        private IReadOnlyCollection<string> BodyLines => new[]
        {
            $"No requests have yet been entered for {this.reminderDates.ToEmailDisplayString()}.",
            "If you do not need parking during this period you can ignore this message.",
            "Otherwise, you should enter requests by the end of today to have them taken into account."
        };
    }
}