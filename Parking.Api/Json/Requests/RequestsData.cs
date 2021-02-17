namespace Parking.Api.Json.Requests
{
    public class RequestsData
    {
        public RequestsData(bool requested) => this.Requested = requested;

        public bool Requested { get; }
    }
}