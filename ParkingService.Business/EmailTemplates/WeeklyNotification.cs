﻿namespace ParkingService.Business.EmailTemplates
{
    using System.Collections.Generic;
    using System.Linq;
    using Model;
    using NodaTime;

    public class WeeklyNotification : IEmailTemplate
    {
        private readonly IReadOnlyCollection<Request> requests;

        private readonly User user;

        private readonly DateInterval dateInterval;

        private readonly IEnumerable<string> postAmbleLines = new[]
        {
            "The number in parentheses indicates how many other people are also waiting for a space on the given day.",
            "Further spaces are released for each date on the preceding working day."
        };

        public WeeklyNotification(IReadOnlyCollection<Request> requests, User user, DateInterval dateInterval)
        {
            this.requests = requests;
            this.user = user;
            this.dateInterval = dateInterval;
        }

        public string To => this.user.EmailAddress;

        public string Subject => $"Provisional parking status for {this.dateInterval.ToEmailDisplayString()}";

        public string PlainTextBody =>
            $"You have been allocated parking spaces for the period {this.dateInterval.ToEmailDisplayString()} as follows:\r\n\r\n" +
            string.Join("\r\n", this.UserActiveDates.Select(FormattedPlainTextStatus)) +
            PlainTextPostAmble;

        private string PlainTextPostAmble =>
            this.UserHasInterruptions
                ? "\r\n\r\n" + string.Join("\r\n\r\n", postAmbleLines)
                : string.Empty;

        private string FormattedPlainTextStatus(LocalDate localDate) =>
            localDate.ToEmailDisplayString() + ": " +
            (this.UserRequestStatus(localDate) == RequestStatus.Allocated
                ? "Allocated"
                : $"INTERRUPTED ({this.OtherInterruptedUsersCount(localDate)})");

        public string HtmlBody =>
            $"<p>You have been allocated parking spaces for the period {this.dateInterval.ToEmailDisplayString()} as follows:</p>\r\n" +
            "<ul>\r\n" + string.Join("\r\n", this.UserActiveDates.Select(FormattedHtmlStatus)) + "\r\n</ul>" +
            HtmlPostAmble;

        private string HtmlPostAmble =>
            this.UserHasInterruptions
                ? "\r\n" + string.Join("\r\n", postAmbleLines.Select(l => $"<p>{l}</p>"))
                : string.Empty;

        private string FormattedHtmlStatus(LocalDate localDate) =>
            "<li>" +
            localDate.ToEmailDisplayString() + ": " +
            (this.UserRequestStatus(localDate) == RequestStatus.Allocated
                ? "Allocated"
                : $"<strong>Interrupted</strong> ({this.OtherInterruptedUsersCount(localDate)})") +
            "</li>";

        private bool UserHasInterruptions => UserActiveDates.Any(d => UserRequestStatus(d) == RequestStatus.Requested);

        private IEnumerable<LocalDate> UserActiveDates => this.dateInterval.Where(UserIsActiveOnDate);

        private bool UserIsActiveOnDate(LocalDate localDate) => requests.Any(r =>
            r.UserId == this.user.UserId &&
            r.Date == localDate &&
            r.Status.IsActive());

        private RequestStatus UserRequestStatus(LocalDate localDate) =>
            this.requests
                .Single(r => r.UserId == this.user.UserId && r.Date == localDate)
                .Status;

        private int OtherInterruptedUsersCount(LocalDate localDate) =>
            this.requests
                .Count(r =>
                    r.UserId != this.user.UserId &&
                    r.Date == localDate &&
                    r.Status == RequestStatus.Requested);
    }
}