namespace Parking.Business
{
    using System.Collections.Generic;
    using System.Linq;
    using Model;
    using NodaTime;
    using NodaTime.Text;

    public static class ExtensionMethods
    {
        public static LocalDate StartOfWeek(this LocalDate localDate) =>
            localDate.Previous(IsoDayOfWeek.Sunday).Next(IsoDayOfWeek.Monday);

        public static string ToEmailDisplayString(this LocalDate localDate) =>
            LocalDatePattern.CreateWithCurrentCulture("ddd dd MMM").Format(localDate);

        public static string ToEmailDisplayString(this IEnumerable<LocalDate> localDateCollection)
        {
            var orderedDates = localDateCollection
                .OrderBy(d => d)
                .ToArray();

            return $"{orderedDates.First().ToEmailDisplayString()} - {orderedDates.Last().ToEmailDisplayString()}";
        }

        public static bool IsActive(this RequestStatus requestStatus) =>
            new[] {RequestStatus.Allocated, RequestStatus.Requested}.Contains(requestStatus);
    }
}