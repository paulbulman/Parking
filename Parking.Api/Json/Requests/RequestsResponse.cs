namespace Parking.Api.Json.Requests
{
    using Calendar;

    public class RequestsResponse
    {
        public RequestsResponse(Calendar<RequestsData> requests) => this.Requests = requests;

        public Calendar<RequestsData> Requests { get; }
    }
}