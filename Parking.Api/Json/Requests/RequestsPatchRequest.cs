namespace Parking.Api.Json.Requests
{
    using System.Collections.Generic;

    public class RequestsPatchRequest
    {
        public RequestsPatchRequest(IEnumerable<RequestPatchRequestDailyData> requests) => this.Requests = requests;

        public IEnumerable<RequestPatchRequestDailyData> Requests { get; }
    }
}