namespace Parking.Api.Json.Calendar
{
    using System.Collections.Generic;

    public class Calendar<T> where T : class
    {
        public Calendar(IEnumerable<Week<T>> weeks) => this.Weeks = weeks;

        public IEnumerable<Week<T>> Weeks { get; }
    }
}