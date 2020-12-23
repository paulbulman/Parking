namespace ParkingService.Business.EmailTemplates
{
    using System.Collections.Generic;
    using System.Linq;
    using Model;
    using NodaTime;

    public class DailyNotification : IEmailTemplate
    {
        private readonly IReadOnlyCollection<Request> requests;
        
        private readonly User user;
        
        private readonly LocalDate localDate;

        public DailyNotification(IReadOnlyCollection<Request> requests, User user, LocalDate localDate)
        {
            this.requests = requests;
            this.user = user;
            this.localDate = localDate;
        }

        public string To => this.user.EmailAddress;

        public string Subject => $"Parking status for {this.localDate.ToEmailDisplayString()}: {this.FormattedRequestStatus}";

        public string PlainTextBody => string.Join("\r\n\r\n", this.BodyLines);

        public string HtmlBody => string
            .Join("\r\n", this.BodyLines.Select(l => $"<p>{l}</p>"))
            .Replace("NOT", "<strong>not</strong>");

        private RequestStatus UserRequestStatus => this.requests
            .Single(r => r.UserId == user.UserId)
            .Status;

        private string FormattedRequestStatus =>
            this.UserRequestStatus == RequestStatus.Allocated ? "Allocated" : "INTERRUPTED";

        private IReadOnlyCollection<string> BodyLines
        {
            get
            {
                if (this.UserRequestStatus == RequestStatus.Allocated)
                {
                    return new[]
                    {
                        $"You have been allocated a parking space for {this.localDate.ToEmailDisplayString()}.",
                        "If you no longer need this space, please cancel your request so that it can be given to someone else."
                    };
                }

                var otherInterruptedUsersCount = this
                    .requests
                    .Count(r => r.UserId != this.user.UserId && r.Status == RequestStatus.Requested);
                
                var isAre = otherInterruptedUsersCount == 1 ? "is" : "are";
                var personPeople = otherInterruptedUsersCount == 1 ? "person" : "people";

                return new[]
                {
                    $"You have NOT been allocated a parking space for {this.localDate.ToEmailDisplayString()}.",
                    "If someone else cancels their request you may be allocated one later, but otherwise you can NOT park at the office.",
                    $"There {isAre} currently {otherInterruptedUsersCount} other {personPeople} also waiting for a space."
                };
            }
        }
    }
}