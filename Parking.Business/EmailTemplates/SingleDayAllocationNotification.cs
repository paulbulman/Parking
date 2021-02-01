namespace Parking.Business.EmailTemplates
{
    using System.Collections.Generic;
    using System.Linq;
    using Model;

    public class SingleDayAllocationNotification : IEmailTemplate
    {
        private readonly Request request;
        
        private readonly User user;

        public SingleDayAllocationNotification(Request request, User user)
        {
            this.request = request;
            this.user = user;
        }

        public string To => this.user.EmailAddress;

        public string Subject => $"Parking space allocated for {this.request.Date.ToEmailDisplayString()}";

        public string PlainTextBody => string.Join("\r\n\r\n", this.BodyLines);

        public string HtmlBody => string.Join("\r\n", this.BodyLines.Select(l => $"<p>{l}</p>"));

        private IReadOnlyCollection<string> BodyLines =>
            new[]
            {
                $"You have been allocated a parking space for {this.request.Date.ToEmailDisplayString()}.",
                "If you no longer need this space, please cancel your request so that it can be given to someone else."
            };
    }
}