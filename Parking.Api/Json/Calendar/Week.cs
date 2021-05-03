namespace Parking.Api.Json.Calendar
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public class Week<T> where T : class
    {
        public Week(IEnumerable<Day<T>> days) => this.Days = days;

        [Required]
        public IEnumerable<Day<T>> Days { get; }
    }
}