namespace Parking.Api.Json.Calendar
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public class Calendar<T> where T : class
    {
        public Calendar(IEnumerable<Week<T>> weeks) => this.Weeks = weeks;

        [Required]
        public IEnumerable<Week<T>> Weeks { get; }
    }
}