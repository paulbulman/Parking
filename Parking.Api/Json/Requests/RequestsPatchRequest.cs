namespace Parking.Api.Json.Requests
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public class RequestsPatchRequest
    {
        public RequestsPatchRequest(IEnumerable<RequestsPatchRequestDailyData> requests) => this.Requests = requests;

        [Required]
        public IEnumerable<RequestsPatchRequestDailyData> Requests { get; }
    }
}