namespace Parking.Api.Json.Overview
{
    using Calendar;

    public class OverviewResponse
    {
        public OverviewResponse(Calendar<OverviewData> overview) => this.Overview = overview;

        public Calendar<OverviewData> Overview { get; }
    }
}