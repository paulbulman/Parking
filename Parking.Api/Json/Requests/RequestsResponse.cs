namespace Parking.Api.Json.Requests
{
    using System.ComponentModel.DataAnnotations;
    using Calendar;

    public class RequestsResponse
    {
        public RequestsResponse(Calendar<RequestsData> requests) => this.Requests = requests;

        [Required]
        public Calendar<RequestsData> Requests { get; }
    }
}