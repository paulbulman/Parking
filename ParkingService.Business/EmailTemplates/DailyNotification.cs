namespace ParkingService.Business.EmailTemplates
{
    using System.Collections.Generic;
    using System.Linq;
    using Model;

    public class DailyNotification : IEmailTemplate
    {
        private readonly IReadOnlyCollection<Request> requests;
        
        private readonly User user;

        public DailyNotification(IReadOnlyCollection<Request> requests, User user)
        {
            this.requests = requests;
            this.user = user;
        }

        public string To => this.user.EmailAddress;

        public string Subject => $"Parking status for {this.FormattedRequestsDate}: {this.FormattedRequestStatus}";

        public string PlainTextBody => string.Join("\r\n\r\n", this.BodyLines);

        public string HtmlBody => string
            .Join("\r\n", this.BodyLines.Select(l => $"<p>{l}</p>"))
            .Replace("NOT", "<strong>not</strong>");

        private string FormattedRequestsDate => this.requests
            .Select(r => r.Date)
            .Distinct()
            .Single()
            .ToEmailDisplayString();

        private RequestStatus UserRequestStatus => this.requests
            .Single(r => r.UserId == user.UserId)
            .Status;

        private string FormattedRequestStatus =>
            this.UserRequestStatus == RequestStatus.Allocated ? "allocated" : "INTERRUPTED";

        private IReadOnlyCollection<string> BodyLines
        {
            get
            {
                if (this.UserRequestStatus == RequestStatus.Allocated)
                {
                    return new[]
                    {
                        $"You have been allocated a parking space for {this.FormattedRequestsDate}.",
                        "If you no longer need this space, please cancel your request so that it can be given to someone else."
                    };
                }

                var otherRequestedUsersCount = this.requests
                    .Count(r => r.Status == RequestStatus.Requested) - 1;
                
                var isAre = otherRequestedUsersCount == 1 ? "is" : "are";
                var personPeople = otherRequestedUsersCount == 1 ? "person" : "people";

                return new[]
                {
                    $"You have NOT been allocated a parking space for {this.FormattedRequestsDate}.",
                    "If someone else cancels their request you may be allocated one later, but otherwise you can NOT park at the office.",
                    $"There {isAre} currently {otherRequestedUsersCount} other {personPeople} also waiting for a space."
                };
            }
        }
    }
}