namespace Parking.Business.EmailTemplates
{
    using System.Collections.Generic;
    using System.Linq;
    using Model;
    using NodaTime;

    public class ReservationReminder : IEmailTemplate
    {
        private readonly User user;
        
        private readonly LocalDate localDate;

        public ReservationReminder(User user, LocalDate localDate)
        {
            this.user = user;
            this.localDate = localDate;
        }

        public string To => this.user.EmailAddress;

        public string Subject => $"No parking reservations entered for {this.localDate.ToEmailDisplayString()}";
        
        public string PlainTextBody => string.Join("\r\n\r\n", this.BodyLines);

        public string HtmlBody => string.Join("\r\n", this.BodyLines.Select(l => $"<p>{l}</p>"));

        private IReadOnlyCollection<string> BodyLines => new[]
        {
            $"No reservations have yet been entered for {this.localDate.ToEmailDisplayString()}.",
            "If no spaces need reserving for this date then you can ignore this message.",
            "Otherwise, you should enter reservations by 11am to ensure spaces are allocated accordingly.",
            "If you do not want to receive these emails, you can turn them off from your profile page in the app.",
        };
    }
}