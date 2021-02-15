namespace Parking.Api.Json.Calendar
{
    using System.Collections.Generic;

    public class Week<T> where T : class
    {
        public Week(IEnumerable<Day<T>> days) => this.Days = days;

        public IEnumerable<Day<T>> Days { get; }
    }
}