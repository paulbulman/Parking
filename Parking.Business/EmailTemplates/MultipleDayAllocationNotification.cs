namespace Parking.Business.EmailTemplates
{
    using System.Collections.Generic;
    using System.Linq;
    using Model;

    public class MultipleDayAllocationNotification : IEmailTemplate
    {
        private readonly IReadOnlyCollection<Request> requests;

        private readonly User user;

        public MultipleDayAllocationNotification(IReadOnlyCollection<Request> requests, User user)
        {
            this.requests = requests;
            this.user = user;
        }

        public string To => this.user.EmailAddress;

        public string Subject => "Parking spaces allocated for multiple upcoming dates";

        public string PlainTextBody =>
            "You have been allocated parking spaces for the following dates:\r\n\r\n" +
            string.Join("\r\n", this.requests.Select(r => r.Date.ToEmailDisplayString())) + "\r\n\r\n" +
            "If there are spaces you no longer need, please cancel the corresponding requests so that they can be given to someone else.";

        public string HtmlBody =>
            "<p>You have been allocated parking spaces for the following dates:</p>\r\n" +
            "<ul>\r\n" + string.Join("\r\n", this.requests.Select(r => $"<li>{r.Date.ToEmailDisplayString()}</li>")) + "\r\n</ul>\r\n" +
            "<p>If there are spaces you no longer need, please cancel the corresponding requests so that they can be given to someone else.</p>";
    }
}