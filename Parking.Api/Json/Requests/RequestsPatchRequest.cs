namespace Parking.Api.Json.Requests
{
    using System.Collections.Generic;

    public class RequestsPatchRequest
    {
        public RequestsPatchRequest(IEnumerable<RequestsPatchRequestDailyData> requests) => this.Requests = requests;

        public IEnumerable<RequestsPatchRequestDailyData> Requests { get; }
    }
}