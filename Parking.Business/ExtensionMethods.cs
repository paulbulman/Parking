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

        public static IEnumerable<YearMonth> YearMonths(this DateInterval dateInterval) =>
            dateInterval.Select(d => d.ToYearMonth()).Distinct();

        public static string ToEmailDisplayString(this LocalDate localDate) =>
            LocalDatePattern.CreateWithCurrentCulture("ddd dd MMM").Format(localDate);

        public static string ToEmailDisplayString(this IEnumerable<LocalDate> localDateCollection)
        {
            var orderedDates = localDateCollection
                .OrderBy(d => d)
                .ToArray();

            return $"{orderedDates.First().ToEmailDisplayString()} - {orderedDates.Last().ToEmailDisplayString()}";
        }

        public static bool IsRequested(this RequestStatus requestStatus) => RequestedStatuses.Contains(requestStatus);

        public static bool IsAllocatable(this RequestStatus requestStatus) =>
            AllocatableStatuses.Contains(requestStatus);

        private static IEnumerable<RequestStatus> RequestedStatuses =>
            new[]
            {
                RequestStatus.Allocated,
                RequestStatus.Interrupted,
                RequestStatus.Pending,
                RequestStatus.SoftInterrupted,
                RequestStatus.HardInterrupted
            };

        private static IEnumerable<RequestStatus> AllocatableStatuses =>
            new[]
            {
                RequestStatus.Interrupted,
                RequestStatus.SoftInterrupted,
            };
    }
}